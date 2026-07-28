using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmPrototype.Farming
{
    [Serializable]
    public sealed class FarmFestivalProgress
    {
        public string FestivalId;
        public int Contributions;
        public bool Completed;
        public FarmFestivalProgress Clone() => new() { FestivalId = FestivalId, Contributions = Contributions, Completed = Completed };
    }

    public readonly struct FarmFestivalDefinition
    {
        public readonly string Id;
        public readonly int Target;
        public readonly int Reward;
        public FarmFestivalDefinition(string id, int target, int reward) { Id = id; Target = target; Reward = reward; }
    }

    public static class FarmFestivalCatalog
    {
        public static bool TryGetActive(int day, out FarmFestivalDefinition festival)
        {
            if (FarmDayClock.DayInSeason(day) == 6)
            {
                festival = new FarmFestivalDefinition($"harvest_share_{(Mathf.Max(1, day) - 1) / FarmDayClock.DaysPerSeason}", 6, 150);
                return true;
            }
            festival = default;
            return false;
        }
    }

    public sealed class FarmFestivalSystem : MonoBehaviour
    {
        private FarmTestPlot plot;
        private FarmGameState state;
        private FarmHudController hud;

        public void Initialize(FarmTestPlot owner, FarmGameState gameState, FarmHudController ownerHud)
        {
            plot = owner; state = gameState; hud = ownerHud;
        }

        private void Update()
        {
            if (state == null || plot == null) return;
            if (!FarmFestivalCatalog.TryGetActive(state.DayNumber, out var festival)) { plot.SetExternalPrompt(this, null); return; }
            var progress = state.Festival;
            var amount = string.Equals(progress.FestivalId, festival.Id, StringComparison.OrdinalIgnoreCase) ? progress.Contributions : 0;
            plot.SetExternalPrompt(this, progress.Completed
                ? FarmLocalization.Get("festival.complete_prompt", "Harvest Share complete! The valley thanks your farm.")
                : FarmLocalization.Format("festival.prompt", "Harvest Share: {0}/{1} crops. Select a crop and press V to contribute.", amount, festival.Target));
            if (FarmHudController.IsModalOpen || Keyboard.current == null || !Keyboard.current.vKey.wasPressedThisFrame || progress.Completed) return;
            var entry = state.SelectedHotbarEntry;
            if (!entry.StartsWith(FarmGameState.ItemPrefix, StringComparison.OrdinalIgnoreCase)) return;
            var itemId = entry[FarmGameState.ItemPrefix.Length..];
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.FestivalContribution, "Player", $"item={itemId}");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            if (state.TryContributeFestival(itemId, out var contributed, out var completed, out var reward, out var error))
                hud?.ShowSystemToast(completed
                    ? FarmLocalization.Format("festival.completed", "Harvest Share complete! +${0} and +1 Favor with every neighbor.", reward)
                    : FarmLocalization.Format("festival.contributed", "Festival contribution accepted: {0}/{1}.", contributed, festival.Target), false);
            else hud?.ShowSystemToast(error, true);
        }
    }
}
