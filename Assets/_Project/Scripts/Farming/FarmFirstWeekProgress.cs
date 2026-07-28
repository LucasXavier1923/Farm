using System;
using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Presentation-only campaign path for the first farm week. Completion is
    /// derived from persistent farm state, so it also works for host snapshots.
    /// </summary>
    public static class FarmFirstWeekProgress
    {
        public const int StepCount = 7;

        private static readonly string[] StepTitleKeys =
        {
            "week.step.first_harvest.title", "week.step.first_order.title",
            "week.step.crop_variety.title", "week.step.scarecrow.title",
            "week.step.expand_land.title", "week.step.tool_upgrade.title",
            "week.step.sprinkler.title"
        };

        private static readonly string[] StepTitleFallbacks =
        {
            "FIRST HARVEST", "VILLAGE FAVOR", "CROP VARIETY",
            "KEEP THE CROWS AWAY", "MAKE ROOM TO GROW", "WORK SMARTER",
            "MORNING AUTOMATION"
        };

        public static string BuildHudText(FarmGameState state)
        {
            if (state == null)
                return FarmLocalization.Get("week.hud.unavailable", "Farm-week progress is unavailable.");

            var completed = CompletedStepCount(state);
            if (completed >= StepCount)
                return FarmLocalization.Get("week.hud.complete", "FARM WEEK COMPLETE  •  Your farm is ready for its next chapter.");

            var step = FirstIncompleteStep(state);
            return FarmLocalization.Format(
                "week.hud.current",
                "FARM WEEK  •  DAY {0}/{1}  •  {2}\n{3}/{4}  •  {5}",
                step + 1, StepCount, GetStepTitle(step), CurrentValue(state, step),
                TargetValue(step), GetStepDescription(state, step));
        }

        public static int CompletedStepCount(FarmGameState state)
        {
            if (state == null) return 0;
            var completed = 0;
            for (var index = 0; index < StepCount; index++)
                if (IsComplete(state, index)) completed++;
            return completed;
        }

        public static int FirstIncompleteStep(FarmGameState state)
        {
            for (var index = 0; index < StepCount; index++)
                if (!IsComplete(state, index)) return index;
            return StepCount - 1;
        }

        public static bool IsComplete(FarmGameState state, int step) =>
            CurrentValue(state, step) >= TargetValue(step);

        public static int CurrentValue(FarmGameState state, int step)
        {
            if (state == null) return 0;
            var journal = state.Journal;
            return step switch
            {
                0 => state.Tutorial?.CompletedCount ?? 0,
                1 => journal?.OrdersDelivered ?? 0,
                2 => journal?.HarvestedCropIds?.Count ?? 0,
                3 => HasPlacedObject(state, "scarecrow_kit") ? 1 : 0,
                4 => Mathf.Max(0, state.LandLevel - FarmGameState.MinLandLevel),
                5 => journal?.ToolUpgrades ?? 0,
                6 => HasPlacedObject(state, "sprinkler_kit") ? 1 : 0,
                _ => 0
            };
        }

        public static int TargetValue(int step) => step switch
        {
            0 => 6,
            1 => 1,
            2 => 2,
            _ => 1
        };

        public static string GetStepTitle(int step)
        {
            step = Mathf.Clamp(step, 0, StepCount - 1);
            return FarmLocalization.Get(StepTitleKeys[step], StepTitleFallbacks[step]);
        }

        private static string GetStepDescription(FarmGameState state, int step)
        {
            if (step == 0)
                return state.Tutorial?.CurrentObjectiveText ?? FarmLocalization.Get("week.step.first_harvest.description", "Learn the basic farming tools.");

            return step switch
            {
                1 => FarmLocalization.Get("week.step.first_order.description", "Deliver a completed request at the village order board."),
                2 => FarmLocalization.Get("week.step.crop_variety.description", "Harvest two different crops. Buy seeds at the market crate."),
                3 => FarmLocalization.Get("week.step.scarecrow.description", "Collect materials, craft a Scarecrow Kit at the workbench, then place it."),
                4 => FarmLocalization.Get("week.step.expand_land.description", "Buy the first adjacent field upgrade at the market crate."),
                5 => FarmLocalization.Get("week.step.tool_upgrade.description", "Select a tool and improve it at the market crate."),
                6 => FarmLocalization.Get("week.step.sprinkler.description", "Craft and place a Sprinkler Kit to water crops at 06:00."),
                _ => string.Empty
            };
        }

        private static bool HasPlacedObject(FarmGameState state, string itemId)
        {
            foreach (var placed in state.PlacedObjects)
                if (placed != null && string.Equals(placed.ItemId, itemId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
