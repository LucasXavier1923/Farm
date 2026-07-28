using System;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmMarketTrend
    {
        Weak,
        Stable,
        Strong,
        Peak
    }

    public readonly struct FarmMarketQuote
    {
        public FarmMarketQuote(FarmMarketTrend trend, float multiplier)
        {
            Trend = trend;
            Multiplier = multiplier;
        }

        public FarmMarketTrend Trend { get; }
        public float Multiplier { get; }
        public string Label => Trend switch
        {
            FarmMarketTrend.Weak => FarmLocalization.Get("market.trend.weak", "WEAK"),
            FarmMarketTrend.Strong => FarmLocalization.Get("market.trend.strong", "STRONG"),
            FarmMarketTrend.Peak => FarmLocalization.Get("market.trend.peak", "PEAK"),
            _ => FarmLocalization.Get("market.trend.stable", "STABLE")
        };
        public string Indicator => Trend switch
        {
            FarmMarketTrend.Weak => "\u2193",
            FarmMarketTrend.Strong => "\u2191",
            FarmMarketTrend.Peak => "\u2605",
            _ => "="
        };
        public string CompactText => $"{Indicator} {Label} x{Multiplier:0.00}";
    }

    public static class FarmMarketRules
    {
        public static FarmMarketQuote Quote(int worldSeed, int day, string itemId)
        {
            var bucket = StableHash(worldSeed, Mathf.Max(1, day), itemId) % 100u;
            var balance = FarmEconomyRules.Balance;
            var weakChance = Mathf.Clamp(balance != null ? balance.WeakChancePercent : 20, 0, 100);
            var strongChance = Mathf.Clamp(balance != null ? balance.StrongChancePercent : 30, 0, 100 - weakChance);
            var peakChance = Mathf.Clamp(balance != null ? balance.PeakChancePercent : 10, 0, 100 - weakChance - strongChance);
            var weakMultiplier = Mathf.Max(0.01f, balance != null ? balance.WeakMultiplier : 0.85f);
            var stableMultiplier = Mathf.Max(0.01f, balance != null ? balance.StableMultiplier : 1f);
            var strongMultiplier = Mathf.Max(0.01f, balance != null ? balance.StrongMultiplier : 1.20f);
            var peakMultiplier = Mathf.Max(0.01f, balance != null ? balance.PeakMultiplier : 1.45f);
            if (bucket < weakChance) return new FarmMarketQuote(FarmMarketTrend.Weak, weakMultiplier);
            if (bucket < weakChance + (100 - weakChance - strongChance - peakChance)) return new FarmMarketQuote(FarmMarketTrend.Stable, stableMultiplier);
            if (bucket < 100 - peakChance) return new FarmMarketQuote(FarmMarketTrend.Strong, strongMultiplier);
            return new FarmMarketQuote(FarmMarketTrend.Peak, peakMultiplier);
        }

        public static int UnitPrice(ItemDefinition definition, FarmItemQuality quality, int worldSeed, int day)
        {
            if (definition == null) return 0;
            var quote = Quote(worldSeed, day, definition.Id);
            return Mathf.CeilToInt(
                FarmEconomyRules.BaseSellPrice(definition) *
                FarmItemQualityRules.SellMultiplier(quality) *
                quote.Multiplier);
        }

        private static uint StableHash(int worldSeed, int day, string itemId)
        {
            unchecked
            {
                var hash = 2166136261u;
                Mix(ref hash, (uint)worldSeed);
                Mix(ref hash, (uint)day);
                var normalized = itemId ?? string.Empty;
                for (var index = 0; index < normalized.Length; index++)
                    Mix(ref hash, char.ToUpperInvariant(normalized[index]));
                return hash;
            }
        }

        private static void Mix(ref uint hash, uint value)
        {
            hash ^= value;
            hash *= 16777619u;
        }
    }
}
