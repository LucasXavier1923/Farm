using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FarmPrototype.Farming
{
    [Serializable]
    public sealed class CraftingIngredient
    {
        public ItemDefinition Item;
        [Min(1)] public int Quantity = 1;
    }

    [CreateAssetMenu(menuName = "Farm/Crafting Recipe", fileName = "CraftingRecipe")]
    public sealed class CraftingRecipe : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        [Tooltip("The shared farm must discover an ingredient before this recipe can be crafted.")]
        public bool RequiresDiscovery;
        public List<CraftingIngredient> Ingredients = new();
        public ItemDefinition OutputItem;
        [Min(1)] public int OutputQuantity = 1;
        public GameObject PreviewPrefab;
        public FarmMasterySkill RequiredMastery = FarmMasterySkill.Harvesting;
        [Range(1, 3)] public int RequiredMasteryLevel = 1;

        public string LocalizedName => FarmLocalization.Get($"recipe.{Id}.name", DisplayName);
        public string LocalizedDescription => FarmLocalization.Get($"recipe.{Id}.description", Description);

        public string IngredientText(FarmGameState state)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < Ingredients.Count; index++)
            {
                var ingredient = Ingredients[index];
                if (ingredient == null || ingredient.Item == null) continue;
                if (builder.Length > 0) builder.Append("  \u2022  ");
                var owned = state != null ? state.GetQuantity(ingredient.Item.Id) : 0;
                builder.Append($"{ingredient.Item.LocalizedName} {owned}/{ingredient.Quantity}");
            }
            return builder.ToString();
        }

        public bool HasIngredients(FarmGameState state)
        {
            if (state == null || !IsUnlocked(state) || OutputItem == null || OutputQuantity <= 0 || Ingredients == null || Ingredients.Count == 0) return false;
            foreach (var ingredient in Ingredients)
                if (ingredient == null || ingredient.Item == null || ingredient.Quantity <= 0 || state.GetQuantity(ingredient.Item.Id) < ingredient.Quantity) return false;
            return true;
        }

        public bool IsUnlocked(FarmGameState state) => state != null &&
            (!RequiresDiscovery || state.IsRecipeDiscovered(Id)) &&
            state.GetMasteryLevel(RequiredMastery) >= Mathf.Clamp(RequiredMasteryLevel, 1, 3);
    }

    public static class FarmCraftingDatabase
    {
        private static List<CraftingRecipe> recipes;
        public static IReadOnlyList<CraftingRecipe> Recipes
        {
            get
            {
                if (recipes == null)
                {
                    recipes = new List<CraftingRecipe>(Resources.LoadAll<CraftingRecipe>("GameData/Recipes"));
                    recipes.RemoveAll(recipe => recipe == null || string.IsNullOrWhiteSpace(recipe.Id) || recipe.OutputItem == null);
                    recipes.Sort((left, right) => string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase));
                }
                return recipes;
            }
        }

        public static void Reload() => recipes = null;
    }
}
