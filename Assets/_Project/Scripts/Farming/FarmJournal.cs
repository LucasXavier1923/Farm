using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmJournalMetric
    {
        Tilled,
        Planted,
        Watered,
        HarvestedUnits,
        SoldUnits,
        SeedPacksBought,
        WorldPickups,
        ToolUpgrades,
        OrdersDelivered,
        UniqueCrops
    }

    [Serializable]
    public sealed class FarmJournalProgress
    {
        public int Tilled;
        public int Planted;
        public int Watered;
        public int HarvestedUnits;
        public int SoldUnits;
        public int SeedPacksBought;
        public int WorldPickups;
        public int ToolUpgrades;
        public int OrdersDelivered;
        public List<string> HarvestedCropIds = new();
        public List<string> ClaimedQuestIds = new();

        public FarmJournalProgress Clone() => new()
        {
            Tilled = Tilled,
            Planted = Planted,
            Watered = Watered,
            HarvestedUnits = HarvestedUnits,
            SoldUnits = SoldUnits,
            SeedPacksBought = SeedPacksBought,
            WorldPickups = WorldPickups,
            ToolUpgrades = ToolUpgrades,
            OrdersDelivered = OrdersDelivered,
            HarvestedCropIds = new List<string>(HarvestedCropIds ?? new List<string>()),
            ClaimedQuestIds = new List<string>(ClaimedQuestIds ?? new List<string>())
        };

        public bool HasClaimed(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId) || ClaimedQuestIds == null) return false;
            foreach (var id in ClaimedQuestIds)
                if (string.Equals(id, questId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public void RecordCrop(string cropId)
        {
            if (string.IsNullOrWhiteSpace(cropId)) return;
            HarvestedCropIds ??= new List<string>();
            foreach (var id in HarvestedCropIds)
                if (string.Equals(id, cropId, StringComparison.OrdinalIgnoreCase)) return;
            HarvestedCropIds.Add(cropId);
        }
    }

    public sealed class FarmJournalQuestDefinition
    {
        public string Id { get; }
        private readonly string categoryKey;
        private readonly string titleKey;
        private readonly string descriptionKey;
        public string Category => FarmLocalization.Get(categoryKey, categoryKey);
        public string Title => FarmLocalization.Get(titleKey, titleKey);
        public string Description => FarmLocalization.Get(descriptionKey, descriptionKey);
        public FarmJournalMetric Metric { get; }
        public int Target { get; }
        public int RewardMoney { get; }

        public FarmJournalQuestDefinition(string id, string categoryKey, string titleKey, string descriptionKey, FarmJournalMetric metric, int target, int rewardMoney)
        {
            Id = id;
            this.categoryKey = categoryKey;
            this.titleKey = titleKey;
            this.descriptionKey = descriptionKey;
            Metric = metric;
            Target = Mathf.Max(1, target);
            RewardMoney = Mathf.Max(0, rewardMoney);
        }

        public int Current(FarmJournalProgress progress)
        {
            if (progress == null) return 0;
            return Metric switch
            {
                FarmJournalMetric.Tilled => progress.Tilled,
                FarmJournalMetric.Planted => progress.Planted,
                FarmJournalMetric.Watered => progress.Watered,
                FarmJournalMetric.HarvestedUnits => progress.HarvestedUnits,
                FarmJournalMetric.SoldUnits => progress.SoldUnits,
                FarmJournalMetric.SeedPacksBought => progress.SeedPacksBought,
                FarmJournalMetric.WorldPickups => progress.WorldPickups,
                FarmJournalMetric.ToolUpgrades => progress.ToolUpgrades,
                FarmJournalMetric.OrdersDelivered => progress.OrdersDelivered,
                FarmJournalMetric.UniqueCrops => progress.HarvestedCropIds?.Count ?? 0,
                _ => 0
            };
        }

        public bool IsComplete(FarmJournalProgress progress) => Current(progress) >= Target;
    }

    public static class FarmJournalDatabase
    {
        private static readonly FarmJournalQuestDefinition[] definitions =
        {
            new("cultivated_land", "journal.cultivated_land.category", "journal.cultivated_land.title", "journal.cultivated_land.description", FarmJournalMetric.Tilled, 10, 50),
            new("harvest_basket", "journal.harvest_basket.category", "journal.harvest_basket.title", "journal.harvest_basket.description", FarmJournalMetric.HarvestedUnits, 12, 75),
            new("crop_variety", "journal.crop_variety.category", "journal.crop_variety.title", "journal.crop_variety.description", FarmJournalMetric.UniqueCrops, 4, 125),
            new("better_tools", "journal.better_tools.category", "journal.better_tools.title", "journal.better_tools.description", FarmJournalMetric.ToolUpgrades, 2, 125),
            new("world_collector", "journal.world_collector.category", "journal.world_collector.title", "journal.world_collector.description", FarmJournalMetric.WorldPickups, 3, 50),
            new("village_regular", "journal.village_regular.category", "journal.village_regular.title", "journal.village_regular.description", FarmJournalMetric.OrdersDelivered, 5, 150)
        };

        public static IReadOnlyList<FarmJournalQuestDefinition> Definitions => definitions;

        public static FarmJournalQuestDefinition Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var definition in definitions)
                if (string.Equals(definition.Id, id, StringComparison.OrdinalIgnoreCase)) return definition;
            return null;
        }
    }
}
