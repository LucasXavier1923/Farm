using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmPrototype.Farming
{
    /// <summary>Mechanics-only processor sharing the existing workbench interaction point.</summary>
    public sealed class FarmProcessorSystem : MonoBehaviour
    {
        private const float Range = 2.2f;
        private FarmTestPlot plot;
        private FarmGameState state;
        private FarmHudController hud;
        private Transform player;
        private Transform station;

        public bool HasSelectedRecipe => ResolveSelectedRecipe() != null;

        public void Initialize(FarmTestPlot owner, FarmGameState gameState, FarmHudController ownerHud, Transform playerTransform)
        {
            if (plot != null) return;
            plot = owner; state = gameState; hud = ownerHud; player = playerTransform;
            station = owner != null && owner.CraftingSystem != null && owner.CraftingSystem.Station != null ? owner.CraftingSystem.Station.transform : null;
        }

        private void Update()
        {
            if (plot == null || state == null || player == null || station == null) return;
            var near = Vector3.Distance(player.position, station.position) <= Range;
            var recipe = ResolveSelectedRecipe();
            var prompt = near ? BuildPrompt(recipe) : string.Empty;
            plot.SetExternalPrompt(this, prompt);
            if (!near || FarmHudController.IsModalOpen || Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame) return;
            // The workbench also owns the crafting menu. A toggle press must never
            // close/open that menu and queue production in the same frame.
            if (plot.CraftingSystem != null && (plot.CraftingSystem.IsOpen || plot.CraftingSystem.ConsumedInteractionThisFrame)) return;
            if (recipe == null) return;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.Production, "Player", $"action=process;station=workbench;recipe={recipe?.Id ?? string.Empty}");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            var result = ExecuteHostProcess("host", recipe?.Id);
            if (result.Succeeded) hud?.ShowSystemToast(result.Message, false);
            else hud?.ShowSystemToast(result.Message, true);
        }

        public FarmSessionCommandResult ExecuteHostProcess(string requestedBy, string requestedRecipeId = null)
        {
            if (!FarmSessionTime.IsSimulationAuthority)
                return new FarmSessionCommandResult(false, FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."));
            if (state == null || string.IsNullOrWhiteSpace(requestedBy))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("production.command.invalid", "Invalid production command."));
            foreach (var job in state.ProcessingJobs)
            {
                if (state.IsProcessingComplete(job) && state.TryCollectProcessing(job.JobId, out var output, out var amount, out var quality))
                    return new FarmSessionCommandResult(true, FarmLocalization.Format("processing.collected_quality", "Collected {0} x{1} ({2}).", FarmContentDatabase.GetItem(output)?.LocalizedName ?? output, amount, FarmItemQualityRules.DisplayName(quality)));
                if (state.IsProcessingComplete(job))
                    return new FarmSessionCommandResult(false, FarmLocalization.Get("processing.inventory_full", "Make room before collecting the refined material."));
            }
            if (state.ProcessingQueueCount >= state.EffectiveProcessingQueueCapacity)
                return new FarmSessionCommandResult(false, FarmLocalization.Get("processing.queue_full", "The workshop queue is full. Let a job finish first."));
            var recipe = ResolveRecipe(requestedRecipeId);
            if (recipe == null) return new FarmSessionCommandResult(false, FarmLocalization.Get("processing.select_input", "Select Stone, Eggs, Pumpkin, or Pond Fish in the hotbar."));
            if (!state.TryGetHighestQualityWithQuantity(recipe.InputItemId, recipe.InputAmount, out var inputQuality))
                return new FarmSessionCommandResult(false, FarmLocalization.Format("processing.missing_recipe", "Need {0} x{1} for {2}.", recipe.InputAmount, FarmContentDatabase.GetItem(recipe.InputItemId)?.LocalizedName ?? recipe.InputItemId, RecipeDisplayName(recipe)));
            if (!state.TryStartProcessing(recipe.Id, recipe.InputItemId, inputQuality, recipe.InputAmount, recipe.OutputItemId, inputQuality, recipe.OutputAmount, recipe.DurationMinutes, out _))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("processing.queue_failed", "The workbench could not queue that job."));
            return new FarmSessionCommandResult(true, FarmLocalization.Format("processing.queued_recipe", "{0} queued ({1}/{2}). It begins after the current job.", RecipeDisplayName(recipe), state.ProcessingQueueCount, state.EffectiveProcessingQueueCapacity));
        }

        private FarmArtisanRecipe ResolveSelectedRecipe()
        {
            var entry = state?.SelectedHotbarEntry ?? string.Empty;
            if (!entry.StartsWith(FarmGameState.ItemPrefix, System.StringComparison.OrdinalIgnoreCase)) return null;
            var itemId = entry[FarmGameState.ItemPrefix.Length..];
            return ResolveRecipe(itemId == "stone" ? "refine_stone" : FarmArtisanCatalog.ForInput(itemId)?.Id);
        }

        private static FarmArtisanRecipe ResolveRecipe(string recipeId)
        {
            if (string.Equals(recipeId, "refine_stone", System.StringComparison.OrdinalIgnoreCase))
                return new FarmArtisanRecipe("refine_stone", "stone", 2, "refined_stone", 1, 10f);
            return FarmArtisanCatalog.Get(recipeId);
        }

        private string BuildPrompt(FarmArtisanRecipe recipe)
        {
            if (recipe == null)
                return FarmLocalization.Format("processing.prompt_queue", "Workbench: select Stone, Eggs, Pumpkin, or Pond Fish. Queue {0}/{1}.", state.ProcessingQueueCount, state.EffectiveProcessingQueueCapacity);
            return FarmLocalization.Format("processing.prompt_recipe", "Workbench: {0} x{1} -> {2}. Press F to queue. {3}/{4} jobs.", FarmContentDatabase.GetItem(recipe.InputItemId)?.LocalizedName ?? recipe.InputItemId, recipe.InputAmount, RecipeDisplayName(recipe), state.ProcessingQueueCount, state.EffectiveProcessingQueueCapacity);
        }

        private static string RecipeDisplayName(FarmArtisanRecipe recipe) =>
            FarmContentDatabase.GetItem(recipe?.OutputItemId)?.LocalizedName ?? recipe?.OutputItemId ?? string.Empty;
    }
}
