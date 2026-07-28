using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmItemQuality
    {
        Normal = 0,
        Silver = 1,
        Gold = 2
    }

    public static class FarmItemQualityRules
    {
        public static FarmItemQuality Clamp(FarmItemQuality quality) =>
            quality is >= FarmItemQuality.Normal and <= FarmItemQuality.Gold
                ? quality
                : FarmItemQuality.Normal;

        public static FarmItemQuality EvaluateHarvest(CropDefinition crop, FarmSeason season, int harvestingLevel, bool fertilized = false, bool rotated = false)
        {
            var score = crop != null && crop.IsPreferredSeason(season) ? 1 : 0;
            score += harvestingLevel >= 3 ? 2 : harvestingLevel >= 2 ? 1 : 0;
            if (fertilized) score++;
            if (rotated) score++;
            return score >= 3 ? FarmItemQuality.Gold
                : score >= 1 ? FarmItemQuality.Silver
                : FarmItemQuality.Normal;
        }

        public static float SellMultiplier(FarmItemQuality quality) => Clamp(quality) switch
        {
            FarmItemQuality.Silver => 1.25f,
            FarmItemQuality.Gold => 1.60f,
            _ => 1f
        };

        public static int UnitSellPrice(ItemDefinition definition, FarmItemQuality quality) =>
            definition == null ? 0 : Mathf.CeilToInt(FarmEconomyRules.BaseSellPrice(definition) * SellMultiplier(quality));

        public static string DisplayName(FarmItemQuality quality) => Clamp(quality) switch
        {
            FarmItemQuality.Silver => FarmLocalization.Get("quality.silver", "Silver"),
            FarmItemQuality.Gold => FarmLocalization.Get("quality.gold", "Gold"),
            _ => FarmLocalization.Get("quality.normal", "Normal")
        };

        public static string ShortMark(FarmItemQuality quality) => Clamp(quality) switch
        {
            FarmItemQuality.Silver => "\u25C6",
            FarmItemQuality.Gold => "\u2605",
            _ => string.Empty
        };
    }
}
