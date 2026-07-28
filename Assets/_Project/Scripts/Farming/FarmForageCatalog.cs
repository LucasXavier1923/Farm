using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public readonly struct FarmForageOpportunity
    {
        public readonly string PickupId;
        public readonly string ItemId;
        public readonly int Quantity;

        public FarmForageOpportunity(string pickupId, string itemId, int quantity)
        {
            PickupId = pickupId;
            ItemId = itemId;
            Quantity = Mathf.Max(1, quantity);
        }
    }

    /// <summary>
    /// Deterministic daily route content. It deliberately has no scene state:
    /// every peer can render the same opportunities while the host alone grants
    /// the pickup through FarmGameState.
    /// </summary>
    public static class FarmForageCatalog
    {
        public const int OpportunitiesPerDay = 3;

        public static List<FarmForageOpportunity> Generate(int worldSeed, int day, FarmSeason season, FarmWeather weather)
        {
            var result = new List<FarmForageOpportunity>(OpportunitiesPerDay);
            day = Mathf.Max(1, day);
            for (var slot = 0; slot < OpportunitiesPerDay; slot++)
            {
                var roll = Hash(worldSeed, day, slot) % 100u;
                var itemId = ResolveItem(season, weather, roll, slot);
                var quantity = itemId == "wildflower" && weather == FarmWeather.Clear && roll >= 70u ? 2 : 1;
                result.Add(new FarmForageOpportunity($"forage:{day}:{slot}:{itemId}", itemId, quantity));
            }
            return result;
        }

        private static string ResolveItem(FarmSeason season, FarmWeather weather, uint roll, int slot)
        {
            if (weather == FarmWeather.Rain && (roll < 70u || slot == 0)) return "forest_mushroom";
            return season switch
            {
                FarmSeason.Spring => roll < 62u ? "wildflower" : "wood",
                FarmSeason.Summer => roll < 45u ? "wildflower" : roll < 82u ? "wood" : "forest_mushroom",
                FarmSeason.Autumn => roll < 62u ? "forest_mushroom" : "wood",
                FarmSeason.Winter => roll < 42u ? "stone" : "forest_mushroom",
                _ => "wood"
            };
        }

        private static uint Hash(int worldSeed, int day, int slot)
        {
            unchecked
            {
                var value = (uint)(worldSeed == 0 ? FarmGameState.DefaultWorldSeed : worldSeed);
                value ^= (uint)Mathf.Max(1, day) * 2246822519u;
                value ^= (uint)(slot + 1) * 3266489917u;
                value ^= value >> 16;
                value *= 2246822519u;
                value ^= value >> 13;
                return value ^ (value >> 16);
            }
        }
    }
}
