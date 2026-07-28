using UnityEngine;

namespace FarmPrototype.Farming
{
    public readonly struct FarmFishCatch
    {
        public readonly string ItemId;
        public readonly int Quantity;
        public readonly FarmItemQuality Quality;

        public FarmFishCatch(string itemId, int quantity, FarmItemQuality quality)
        {
            ItemId = itemId;
            Quantity = Mathf.Max(1, quantity);
            Quality = FarmItemQualityRules.Clamp(quality);
        }
    }

    /// <summary>
    /// Pure, host-safe pond ecology. The output is derived from shared world
    /// state, so a client cannot select a valuable fish through its interface.
    /// </summary>
    public static class FarmFishingCatalog
    {
        public static FarmFishCatch Resolve(int worldSeed, int day, int catchIndex, FarmDayPhase phase, FarmWeather weather)
        {
            var season = FarmDayClock.SeasonForDay(day);
            var roll = Roll(worldSeed, day, catchIndex, phase, weather);
            if (weather == FarmWeather.Rain)
                return new FarmFishCatch("rain_carp", 1, roll < 18 ? FarmItemQuality.Gold : FarmItemQuality.Silver);
            if (phase == FarmDayPhase.Morning && season is FarmSeason.Spring or FarmSeason.Autumn && roll < 78)
                return new FarmFishCatch("dawn_trout", 1, roll < 14 ? FarmItemQuality.Gold : FarmItemQuality.Normal);
            if (phase == FarmDayPhase.Dusk && season is FarmSeason.Summer or FarmSeason.Autumn && roll < 78)
                return new FarmFishCatch("sunset_perch", 1, roll < 16 ? FarmItemQuality.Gold : FarmItemQuality.Normal);
            return new FarmFishCatch("pond_fish", 1, roll < 10 ? FarmItemQuality.Silver : FarmItemQuality.Normal);
        }

        public static string ConditionHint(int day, FarmDayPhase phase, FarmWeather weather)
        {
            if (weather == FarmWeather.Rain) return FarmLocalization.Get("fishing.condition.rain", "Rain is stirring up Carp.");
            var season = FarmDayClock.SeasonForDay(day);
            if (phase == FarmDayPhase.Morning && season is FarmSeason.Spring or FarmSeason.Autumn) return FarmLocalization.Get("fishing.condition.dawn", "Morning water may hold Trout.");
            if (phase == FarmDayPhase.Dusk && season is FarmSeason.Summer or FarmSeason.Autumn) return FarmLocalization.Get("fishing.condition.dusk", "Dusk water may hold Perch.");
            return FarmLocalization.Get("fishing.condition.calm", "Calm water favors common Pond Fish.");
        }

        private static uint Roll(int worldSeed, int day, int catchIndex, FarmDayPhase phase, FarmWeather weather)
        {
            unchecked
            {
                var hash = (uint)(worldSeed == 0 ? FarmGameState.DefaultWorldSeed : worldSeed);
                hash ^= (uint)Mathf.Max(1, day) * 747796405u;
                hash ^= (uint)Mathf.Max(0, catchIndex) * 2891336453u;
                hash ^= (uint)phase * 2246822519u;
                hash ^= (uint)weather * 3266489917u;
                hash ^= hash >> 16;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                return hash % 100u;
            }
        }
    }
}
