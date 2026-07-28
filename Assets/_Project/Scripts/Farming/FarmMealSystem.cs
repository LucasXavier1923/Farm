using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Small food boundary: recipes create normal inventory items and this
    /// system is the only place that turns a meal into a shared-energy result.
    /// Future buffs, gift preferences, and multiplayer acknowledgements can
    /// extend the same host command without changing recipe data.
    /// </summary>
    public sealed class FarmMealSystem : MonoBehaviour
    {
        private FarmGameState state;
        private FarmHudController hud;
        private string playerName;

        public void Initialize(FarmGameState gameState, FarmHudController ownerHud, string localPlayerName)
        {
            state = gameState;
            hud = ownerHud;
            playerName = string.IsNullOrWhiteSpace(localPlayerName) ? "Player" : localPlayerName;
        }

        private void Update()
        {
            if (state == null || FarmHudController.IsModalOpen || Keyboard.current == null || !Keyboard.current.rKey.wasPressedThisFrame) return;
            var entry = state.SelectedHotbarEntry;
            if (!entry.StartsWith(FarmGameState.ItemPrefix, System.StringComparison.OrdinalIgnoreCase)) return;
            var itemId = entry[FarmGameState.ItemPrefix.Length..];
            if (!FarmMealCatalog.IsMeal(itemId)) return;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.Consumption, playerName, $"item={itemId}");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            var result = ExecuteHostConsume(itemId, playerName);
            hud?.ShowSystemToast(result.Message, !result.Succeeded);
        }

        public FarmSessionCommandResult ExecuteHostConsume(string itemId, string requestedBy)
        {
            if (!FarmSessionTime.IsSimulationAuthority || state == null || string.IsNullOrWhiteSpace(requestedBy))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("meal.command.invalid", "Invalid meal request."));
            if (!FarmMealCatalog.TryConsume(state, itemId, out var restored, out var error))
                return new FarmSessionCommandResult(false, error);
            var item = FarmContentDatabase.GetItem(itemId);
            return new FarmSessionCommandResult(true, FarmLocalization.Format("meal.consumed", "Enjoyed {0}: restored {1} Energy.", item != null ? item.LocalizedName : itemId, restored));
        }
    }

    public static class FarmMealCatalog
    {
        public static bool IsMeal(string itemId) => EnergyFor(itemId) > 0;

        public static int EnergyFor(string itemId) => itemId switch
        {
            "wildflower_tea" => 18,
            "mushroom_stew" => 35,
            "fish_skewer" => 28,
            "pumpkin_roast" => 26,
            "farm_omelet" => 42,
            "egg_preserve" => 16,
            "pumpkin_jam" => 26,
            "smoked_fish" => 30,
            _ => 0
        };

        public static bool TryConsume(FarmGameState state, string itemId, out int restored, out string error)
        {
            restored = 0;
            error = string.Empty;
            var energy = EnergyFor(itemId);
            if (state == null || energy <= 0 || FarmContentDatabase.GetItem(itemId)?.Category != ItemCategory.Consumable)
            {
                error = FarmLocalization.Get("meal.command.invalid", "Invalid meal request.");
                return false;
            }
            if (state.Energy >= FarmGameState.MaxEnergy)
            {
                error = FarmLocalization.Get("meal.energy_full", "Energy is already full. Save the meal for later.");
                return false;
            }
            if (!state.TryRemoveItem(itemId, 1))
            {
                error = FarmLocalization.Get("meal.missing", "That meal is no longer in the inventory.");
                return false;
            }
            restored = state.RestoreEnergy(energy);
            return restored > 0;
        }
    }
}
