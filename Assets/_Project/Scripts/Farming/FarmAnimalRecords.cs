using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    [Serializable]
    public sealed class FarmAnimalRecord
    {
        public string Id;
        public string DisplayName;
        public int Affection;
        public int LastCareDay;
        public string TraitId;
        public string FavoriteItemId;
        public FarmAnimalRecord Clone() => new()
        {
            Id = Id,
            DisplayName = DisplayName,
            Affection = Affection,
            LastCareDay = LastCareDay,
            TraitId = TraitId,
            FavoriteItemId = FavoriteItemId
        };
    }

    [Serializable]
    public sealed class FarmAnimalRecords
    {
        public List<FarmAnimalRecord> Chickens = new();
        public int ExpansionWood;
        public int ExpansionStone;
        public bool CoopExpanded;

        public static FarmAnimalRecords CreateStarter()
        {
            var result = new FarmAnimalRecords();
            result.EnsureNormalized();
            return result;
        }

        public void EnsureNormalized()
        {
            Chickens ??= new List<FarmAnimalRecord>();
            EnsureChicken("chicken_a", "Pip", "steady_layer", "wildflower");
            EnsureChicken("chicken_b", "Clover", "generous_layer", "forest_mushroom");
            if (CoopExpanded) EnsureChicken("chicken_c", "Maple", "speckled_layer", "wildflower_tea");
        }

        public FarmAnimalRecord Find(string animalId)
        {
            if (string.IsNullOrWhiteSpace(animalId)) return null;
            foreach (var chicken in Chickens)
                if (chicken != null && string.Equals(chicken.Id, animalId, StringComparison.OrdinalIgnoreCase)) return chicken;
            return null;
        }

        private void EnsureChicken(string id, string displayName, string traitId, string favoriteItemId)
        {
            var chicken = Find(id);
            if (chicken == null)
            {
                Chickens.Add(new FarmAnimalRecord
                {
                    Id = id,
                    DisplayName = displayName,
                    TraitId = traitId,
                    FavoriteItemId = favoriteItemId
                });
                return;
            }

            chicken.DisplayName = string.IsNullOrWhiteSpace(chicken.DisplayName) ? displayName : chicken.DisplayName;
            chicken.TraitId = string.IsNullOrWhiteSpace(chicken.TraitId) ? traitId : chicken.TraitId;
            chicken.FavoriteItemId = string.IsNullOrWhiteSpace(chicken.FavoriteItemId) ? favoriteItemId : chicken.FavoriteItemId;
        }

        public FarmAnimalRecords Clone()
        {
            EnsureNormalized();
            var clone = new FarmAnimalRecords();
            foreach (var chicken in Chickens)
                if (chicken != null && !string.IsNullOrWhiteSpace(chicken.Id)) clone.Chickens.Add(chicken.Clone());
            clone.ExpansionWood = ExpansionWood;
            clone.ExpansionStone = ExpansionStone;
            clone.CoopExpanded = CoopExpanded;
            return clone;
        }
    }
}
