using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmDailyOrderType { Crop, Fishing, Animal, Production }

    [Serializable]
    public sealed class FarmDailyOrderProgress
    {
        public int Day = 1;
        public int CompletedMask;
        public bool IsCompleted(int index) => index >= 0 && index < 31 && (CompletedMask & (1 << index)) != 0;
        public void MarkCompleted(int index) { if (index >= 0 && index < 31) CompletedMask |= 1 << index; }
        public bool IsBoardComplete(int orderCount) => orderCount > 0 && (CompletedMask & ((1 << orderCount) - 1)) == ((1 << orderCount) - 1);
        public FarmDailyOrderProgress Clone() => new() { Day = Day, CompletedMask = CompletedMask };
    }

    public sealed class FarmDailyOrder
    {
        public string Id { get; set; }
        public int Day { get; set; }
        public int Slot { get; set; }
        public FarmDailyOrderType Type { get; set; }
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public int Reward { get; set; }
        public string RequesterId => FarmCommunityCatalog.GetRequesterId(Day, Slot);
        public ItemDefinition Item => FarmContentDatabase.GetItem(ItemId);
        public string TypeDisplayName => Type switch
        {
            FarmDailyOrderType.Fishing => FarmLocalization.Get("orders.type.fishing", "FISHING"),
            FarmDailyOrderType.Animal => FarmLocalization.Get("orders.type.animal", "ANIMAL CARE"),
            FarmDailyOrderType.Production => FarmLocalization.Get("orders.type.production", "WORKSHOP"),
            _ => FarmLocalization.Get("orders.type.crop", "FARMING")
        };
        public string DisplayText => Item != null
            ? FarmLocalization.Format("orders.item", "{0}: {1} x{2}", TypeDisplayName, Item.LocalizedName, Quantity)
            : FarmLocalization.Format("orders.item", "{0}: {1} x{2}", TypeDisplayName, ItemId, Quantity);
    }

    public static class FarmDailyOrderGenerator
    {
        public const int OrderCount = 3;
        public static int BoardCompletionBonus => FarmEconomyRules.BoardCompletionBonus;

        public static List<FarmDailyOrder> Generate(int worldSeed, int day)
        {
            day = Mathf.Max(1, day);
            var crops = new List<CropDefinition>();
            foreach (var crop in FarmContentDatabase.Crops)
                if (crop != null && crop.HarvestItem != null) crops.Add(crop);
            crops.Sort((left, right) => string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase));
            DeterministicShuffle(crops, Hash(worldSeed, day, 0));

            var result = new List<FarmDailyOrder>(OrderCount);
            if (crops.Count > 0)
            {
                var crop = crops[0];
                result.Add(Create(day, 0, FarmDailyOrderType.Crop, crop.HarvestItem.Id, 2 + (int)(Hash(worldSeed, day, 1) % 3u)));
            }

            var naturePool = day % 2 == 0
                ? new[] { "pond_fish", "dawn_trout", "rain_carp", "sunset_perch" }
                : new[] { "chicken_egg" };
            var natureItem = PickAvailable(naturePool, Hash(worldSeed, day, 2));
            if (!string.IsNullOrWhiteSpace(natureItem))
                result.Add(Create(day, 1, day % 2 == 0 ? FarmDailyOrderType.Fishing : FarmDailyOrderType.Animal, natureItem, day % 2 == 0 ? 1 + (int)(Hash(worldSeed, day, 3) % 2u) : 1));

            var productionItem = PickAvailable(new[] { "refined_stone", "fish_skewer" }, Hash(worldSeed, day, 4));
            if (!string.IsNullOrWhiteSpace(productionItem))
                result.Add(Create(day, 2, FarmDailyOrderType.Production, productionItem, 1));
            return result;
        }

        private static FarmDailyOrder Create(int day, int slot, FarmDailyOrderType type, string itemId, int quantity)
        {
            var item = FarmContentDatabase.GetItem(itemId);
            quantity = Mathf.Max(1, quantity);
            var baseValue = Mathf.Max(1, FarmEconomyRules.BaseSellPrice(item)) * quantity;
            return new FarmDailyOrder
            {
                Id = $"daily:{day}:{slot}:{type}:{itemId}:{quantity}",
                Day = day,
                Slot = slot,
                Type = type,
                ItemId = itemId,
                Quantity = quantity,
                Reward = FarmEconomyRules.DailyOrderReward(baseValue)
            };
        }

        private static string PickAvailable(IReadOnlyList<string> ids, uint seed)
        {
            if (ids == null || ids.Count == 0) return string.Empty;
            for (var offset = 0; offset < ids.Count; offset++)
            {
                var id = ids[(int)((seed + (uint)offset) % (uint)ids.Count)];
                if (FarmContentDatabase.GetItem(id) != null) return id;
            }
            return string.Empty;
        }

        private static void DeterministicShuffle(List<CropDefinition> crops, uint seed)
        {
            for (var index = crops.Count - 1; index > 0; index--)
            {
                seed = Mix(seed + (uint)index);
                var swap = (int)(seed % (uint)(index + 1));
                (crops[index], crops[swap]) = (crops[swap], crops[index]);
            }
        }

        private static uint Hash(int worldSeed, int day, int slot)
        {
            unchecked
            {
                var value = (uint)(worldSeed == 0 ? FarmGameState.DefaultWorldSeed : worldSeed);
                value ^= (uint)day * 747796405u;
                value ^= (uint)(slot + 1) * 2891336453u;
                return Mix(value);
            }
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 2246822519u;
            value ^= value >> 13;
            value *= 3266489917u;
            value ^= value >> 16;
            return value;
        }
    }
}
