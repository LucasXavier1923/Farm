using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    [Serializable]
    public sealed class FarmCropEconomyEntry
    {
        public string CropId;
        [Min(1)] public int SeedPackAmount = 5;
        [Min(0)] public int SeedPackPrice = 20;
        [Min(0)] public int BaseSellPrice = 10;
    }

    /// <summary>
    /// The editable source of truth for the local first-week economy. Existing
    /// crop and item fields remain safe fallbacks while the catalog is expanded.
    /// </summary>
    [CreateAssetMenu(menuName = "Farm/Economy Balance", fileName = "FarmEconomyBalance")]
    public sealed class FarmEconomyBalance : ScriptableObject
    {
        [Header("Crop packs and normal crop value")]
        public List<FarmCropEconomyEntry> Crops = new();

        [Header("Investments")]
        [Min(0)] public int ToolLevelTwoCost = 150;
        [Min(0)] public int ToolLevelThreeCost = 500;
        [Min(0)] public int LandLevelTwoCost = 500;
        [Min(0)] public int LandLevelThreeCost = 1500;

        [Header("Daily orders")]
        [Min(1f)] public float DailyOrderRewardMultiplier = 1.6f;
        [Min(0)] public int DailyOrderMinimumBonus = 5;
        [Min(1)] public int DailyOrderRounding = 5;
        [Min(0)] public int BoardCompletionBonus = 25;

        [Header("Deterministic daily market")]
        [Range(0, 100)] public int WeakChancePercent = 20;
        [Range(0, 100)] public int StrongChancePercent = 30;
        [Range(0, 100)] public int PeakChancePercent = 10;
        [Min(0.01f)] public float WeakMultiplier = 0.85f;
        [Min(0.01f)] public float StableMultiplier = 1f;
        [Min(0.01f)] public float StrongMultiplier = 1.20f;
        [Min(0.01f)] public float PeakMultiplier = 1.45f;

        public FarmCropEconomyEntry GetCrop(string cropId)
        {
            if (string.IsNullOrWhiteSpace(cropId) || Crops == null) return null;
            foreach (var entry in Crops)
                if (entry != null && string.Equals(entry.CropId, cropId, StringComparison.OrdinalIgnoreCase)) return entry;
            return null;
        }
    }

    public static class FarmEconomyRules
    {
        private const string BalanceResourcePath = "GameData/Economy/FarmEconomyBalance";
        private static FarmEconomyBalance sourceBalance;
        private static FarmEconomyBalance balance;

        public static FarmEconomyBalance Balance
        {
            get
            {
                sourceBalance ??= Resources.Load<FarmEconomyBalance>(BalanceResourcePath);
                if (!Application.isPlaying) return sourceBalance;
                if (balance == null && sourceBalance != null)
                {
                    balance = UnityEngine.Object.Instantiate(sourceBalance);
                    balance.name = sourceBalance.name;
                    balance.hideFlags = HideFlags.DontSave;
                }
                return balance;
            }
        }

        public static void Reload()
        {
            balance = null;
            sourceBalance = null;
        }

        // Required when Enter Play Mode Options disables domain reload: a test
        // edit to a ScriptableObject must never leak into the next farm session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewRuntimeSession() => Reload();

        public static int SeedPackAmount(CropDefinition crop)
        {
            var entry = Balance != null && crop != null ? Balance.GetCrop(crop.Id) : null;
            return Mathf.Max(1, entry != null ? entry.SeedPackAmount : crop != null ? crop.SeedPackAmount : 0);
        }

        public static int SeedPackPrice(CropDefinition crop)
        {
            var entry = Balance != null && crop != null ? Balance.GetCrop(crop.Id) : null;
            return Mathf.Max(0, entry != null ? entry.SeedPackPrice : crop != null ? crop.SeedPackPrice : 0);
        }

        public static int BaseSellPrice(ItemDefinition item)
        {
            if (item == null) return 0;
            FarmCropEconomyEntry entry = null;
            if (Balance != null)
            {
                foreach (var crop in FarmContentDatabase.Crops)
                {
                    if (crop == null || crop.HarvestItem == null ||
                        !string.Equals(crop.HarvestItem.Id, item.Id, StringComparison.OrdinalIgnoreCase)) continue;
                    entry = Balance.GetCrop(crop.Id);
                    break;
                }
            }
            return Mathf.Max(0, entry != null ? entry.BaseSellPrice : item.BaseSellPrice);
        }

        public static int ToolUpgradeCost(int currentLevel)
        {
            var configured = Balance;
            return currentLevel switch
            {
                1 => Mathf.Max(0, configured != null ? configured.ToolLevelTwoCost : 150),
                2 => Mathf.Max(0, configured != null ? configured.ToolLevelThreeCost : 500),
                _ => 0
            };
        }

        public static int LandUpgradeCost(int currentLevel)
        {
            var configured = Balance;
            return currentLevel switch
            {
                1 => Mathf.Max(0, configured != null ? configured.LandLevelTwoCost : 500),
                2 => Mathf.Max(0, configured != null ? configured.LandLevelThreeCost : 1500),
                _ => 0
            };
        }

        public static int DailyOrderReward(int baseValue)
        {
            var configured = Balance;
            var multiplier = Mathf.Max(1f, configured != null ? configured.DailyOrderRewardMultiplier : 1.6f);
            var rounding = Mathf.Max(1, configured != null ? configured.DailyOrderRounding : 5);
            var minimumBonus = Mathf.Max(0, configured != null ? configured.DailyOrderMinimumBonus : 5);
            var rounded = Mathf.CeilToInt((Mathf.Max(0, baseValue) * multiplier) / rounding) * rounding;
            return Mathf.Max(Mathf.Max(0, baseValue) + minimumBonus, rounded);
        }

        public static int BoardCompletionBonus => Mathf.Max(0, Balance != null ? Balance.BoardCompletionBonus : 25);
    }
}
