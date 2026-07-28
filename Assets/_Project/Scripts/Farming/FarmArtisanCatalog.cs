using System;
using System.Collections.Generic;

namespace FarmPrototype.Farming
{
    /// <summary>Small balanceable set of timed, single-input artisan conversions.</summary>
    public sealed class FarmArtisanRecipe
    {
        public string Id { get; }
        public string InputItemId { get; }
        public int InputAmount { get; }
        public string OutputItemId { get; }
        public int OutputAmount { get; }
        public float DurationMinutes { get; }

        public FarmArtisanRecipe(string id, string inputItemId, int inputAmount, string outputItemId, int outputAmount, float durationMinutes)
        {
            Id = id; InputItemId = inputItemId; InputAmount = inputAmount;
            OutputItemId = outputItemId; OutputAmount = outputAmount; DurationMinutes = durationMinutes;
        }
    }

    public static class FarmArtisanCatalog
    {
        private static readonly FarmArtisanRecipe[] Recipes =
        {
            new("preserve_eggs", "chicken_egg", 2, "egg_preserve", 1, 30f),
            new("cook_pumpkin_jam", "pumpkin", 3, "pumpkin_jam", 1, 35f),
            new("smoke_pond_fish", "pond_fish", 2, "smoked_fish", 1, 35f)
        };

        public static IReadOnlyList<FarmArtisanRecipe> All => Recipes;

        public static FarmArtisanRecipe Get(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) return null;
            foreach (var recipe in Recipes)
                if (string.Equals(recipe.Id, recipeId, StringComparison.OrdinalIgnoreCase)) return recipe;
            return null;
        }

        public static FarmArtisanRecipe ForInput(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return null;
            foreach (var recipe in Recipes)
                if (string.Equals(recipe.InputItemId, itemId, StringComparison.OrdinalIgnoreCase)) return recipe;
            return null;
        }
    }
}
