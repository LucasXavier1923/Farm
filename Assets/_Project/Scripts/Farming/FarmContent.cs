using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public static class FarmContentDatabase
    {
        private static Dictionary<string, ItemDefinition> items;
        private static Dictionary<string, CropDefinition> crops;

        public static ItemDefinition GetItem(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && items.TryGetValue(id, out var item) ? item : null;
        }

        public static CropDefinition GetCrop(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && crops.TryGetValue(id, out var crop) ? crop : null;
        }

        public static CropDefinition GetCropForSeed(string seedItemId)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(seedItemId)) return null;
            foreach (var crop in crops.Values)
                if (crop != null && crop.SeedItem != null && string.Equals(crop.SeedItem.Id, seedItemId, StringComparison.OrdinalIgnoreCase)) return crop;
            return null;
        }

        public static IReadOnlyCollection<ItemDefinition> Items
        {
            get { EnsureLoaded(); return items.Values; }
        }

        public static IReadOnlyCollection<CropDefinition> Crops
        {
            get { EnsureLoaded(); return crops.Values; }
        }

        public static void Reload()
        {
            items = null;
            crops = null;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (items != null && crops != null) return;
            items = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
            crops = new Dictionary<string, CropDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in Resources.LoadAll<ItemDefinition>("GameData/Items"))
                if (item != null && !string.IsNullOrWhiteSpace(item.Id)) items[item.Id] = item;
            foreach (var crop in Resources.LoadAll<CropDefinition>("GameData/Crops"))
                if (crop != null && !string.IsNullOrWhiteSpace(crop.Id)) crops[crop.Id] = crop;
        }
    }
}