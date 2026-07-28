using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmPrototype.Farming
{
    [Serializable]
    public sealed class FarmCommunityProjectProgress
    {
        public int Wood;
        public int Stone;
        public int RefinedStone;
        public bool MarketRouteComplete;
        public FarmCommunityProjectProgress Clone() => new() { Wood = Wood, Stone = Stone, RefinedStone = RefinedStone, MarketRouteComplete = MarketRouteComplete };
    }

    public static class FarmCommunityProjectCatalog
    {
        public const int WoodTarget = 15;
        public const int StoneTarget = 10;
        public const int RefinedStoneTarget = 3;
        public static bool IsProjectMaterial(string itemId) => itemId is "wood" or "stone" or "refined_stone";
    }

    public sealed class FarmCommunityProjectSystem : MonoBehaviour
    {
        private FarmTestPlot plot;
        private FarmGameState state;
        private FarmHudController hud;

        public void Initialize(FarmTestPlot owner, FarmGameState gameState, FarmHudController ownerHud) { plot = owner; state = gameState; hud = ownerHud; }

        private void Update()
        {
            if (plot == null || state == null) return;
            if (FarmFestivalCatalog.TryGetActive(state.DayNumber, out _)) { plot.SetExternalPrompt(this, null); return; }
            var project = state.CommunityProjects;
            plot.SetExternalPrompt(this, project.MarketRouteComplete
                ? FarmLocalization.Get("project.complete_prompt", "Market Route active: daily orders may use shared storage.")
                : FarmLocalization.Format("project.prompt", "Market Route: Wood {0}/{1}  Stone {2}/{3}  Refined {4}/{5}. Select a material and press B.", project.Wood, FarmCommunityProjectCatalog.WoodTarget, project.Stone, FarmCommunityProjectCatalog.StoneTarget, project.RefinedStone, FarmCommunityProjectCatalog.RefinedStoneTarget));
            if (project.MarketRouteComplete || FarmHudController.IsModalOpen || Keyboard.current == null || !Keyboard.current.bKey.wasPressedThisFrame) return;
            var entry = state.SelectedHotbarEntry;
            if (!entry.StartsWith(FarmGameState.ItemPrefix, StringComparison.OrdinalIgnoreCase)) return;
            var itemId = entry[FarmGameState.ItemPrefix.Length..];
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.CommunityProject, "Player", $"item={itemId}");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            if (state.TryContributeCommunityProject(itemId, out var complete, out var error))
                hud?.ShowSystemToast(complete
                    ? FarmLocalization.Get("project.completed", "Market Route complete! Daily orders can now use shared storage.")
                    : FarmLocalization.Get("project.contributed", "Project material delivered."), false);
            else hud?.ShowSystemToast(error, true);
        }
    }
}
