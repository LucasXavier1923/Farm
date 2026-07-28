using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmMilestone { Tilled, Planted, Watered, Harvested, Sold, BoughtSeeds }

    [Serializable]
    public sealed class FarmTutorialProgress
    {
        public bool Tilled;
        public bool Planted;
        public bool Watered;
        public bool Harvested;
        public bool Sold;
        public bool BoughtSeeds;
        public bool RewardClaimed;

        public int CompletedCount =>
            (Tilled ? 1 : 0) + (Planted ? 1 : 0) + (Watered ? 1 : 0) +
            (Harvested ? 1 : 0) + (Sold ? 1 : 0) + (BoughtSeeds ? 1 : 0);
        public bool IsComplete => CompletedCount >= 6;

        public string CurrentObjectiveText
        {
            get
            {
                if (!Tilled) return FarmLocalization.Get("tutorial.till", "Prepare a plot tile with the hoe.");
                if (!Planted) return FarmLocalization.Get("tutorial.plant", "Plant a seed in the plot tile.");
                if (!Watered) return FarmLocalization.Get("tutorial.water", "Water the planted seed.");
                if (!Harvested) return FarmLocalization.Get("tutorial.harvest", "Wait for the crop to grow, then harvest it.");
                if (!Sold) return FarmLocalization.Get("tutorial.sell", "Sell your harvest at the market crate.");
                if (!BoughtSeeds) return FarmLocalization.Get("tutorial.buy", "Buy a seed pack.");
                return FarmLocalization.Get("tutorial.complete", "First harvest complete!");
            }
        }

        public bool Mark(FarmMilestone milestone)
        {
            switch (milestone)
            {
                case FarmMilestone.Tilled when !Tilled: Tilled = true; return true;
                case FarmMilestone.Planted when !Planted: Planted = true; return true;
                case FarmMilestone.Watered when !Watered: Watered = true; return true;
                case FarmMilestone.Harvested when !Harvested: Harvested = true; return true;
                case FarmMilestone.Sold when !Sold: Sold = true; return true;
                case FarmMilestone.BoughtSeeds when !BoughtSeeds: BoughtSeeds = true; return true;
                default: return false;
            }
        }

        public FarmTutorialProgress Clone() => new()
        {
            Tilled = Tilled,
            Planted = Planted,
            Watered = Watered,
            Harvested = Harvested,
            Sold = Sold,
            BoughtSeeds = BoughtSeeds,
            RewardClaimed = RewardClaimed
        };
    }

    [Serializable]
    public sealed class InventoryStack
    {
        public string ItemId;
        public int Quantity;
        public FarmItemQuality Quality;

        public InventoryStack() { }
        public InventoryStack(string itemId, int quantity, FarmItemQuality quality = FarmItemQuality.Normal)
        {
            ItemId = itemId;
            Quantity = quantity;
            Quality = FarmItemQualityRules.Clamp(quality);
        }
    }
    [Serializable]
    public sealed class FarmTileSaveData
    {
        public int Index;
        public int State;
        public string CropId;
        public float GrowthSecondsRemaining;
        public bool Fertilized;
        public string LastHarvestedCropId;
        public bool Rotated;
    }

    [Serializable]
    public sealed class FarmPlacedObjectSaveData
    {
        public string PersistentId;
        public string ItemId;
        public float X;
        public float Y;
        public float Z;
        public float RotationY;

        public FarmPlacedObjectSaveData Clone() => new()
        {
            PersistentId = PersistentId,
            ItemId = ItemId,
            X = X,
            Y = Y,
            Z = Z,
            RotationY = RotationY
        };
    }

    [Serializable]
    public sealed class FarmResourceNodeSaveData
    {
        public string NodeId;
        public int Hits;
        public int RespawnDay;
        public bool Stewarded;

        public FarmResourceNodeSaveData Clone() => new() { NodeId = NodeId, Hits = Hits, RespawnDay = RespawnDay, Stewarded = Stewarded };
    }

    [Serializable]
    public sealed class FarmProcessingJobSaveData
    {
        public string JobId;
        public string RecipeId;
        public string OutputItemId;
        public int OutputAmount;
        public FarmItemQuality OutputQuality;
        public float CompletionGameMinutes;
        public FarmProcessingJobSaveData Clone() => new() { JobId = JobId, RecipeId = RecipeId, OutputItemId = OutputItemId, OutputAmount = OutputAmount, OutputQuality = FarmItemQualityRules.Clamp(OutputQuality), CompletionGameMinutes = CompletionGameMinutes };
    }

    [Serializable]
    public sealed class FarmSaveData
    {
        public int Version = 36;
        public int LandLevel = FarmGameState.MinLandLevel;
        public int Money;
        public List<InventoryStack> Inventory = new();
        public List<FarmTileSaveData> Tiles = new();
        public List<string> Hotbar = new();
        public int SelectedHotbarIndex;
        public FarmTutorialProgress Tutorial = new();
        public List<InventoryStack> Storage = new();
        public List<string> CollectedPickupIds = new();
        public int DayNumber = 1;
        public float MinutesOfDay = 480f;
        public int WorldSeed = FarmGameState.DefaultWorldSeed;
        public int HoeLevel = 1;
        public int WateringCanLevel = 1;
        public int HarvestLevel = 1;
        public FarmJournalProgress Journal = new();
        public FarmDailyOrderProgress DailyOrders = new();
        public FarmCommunityProgress Community = new();
        public FarmFestivalProgress Festival = new();
        public FarmCommunityProjectProgress CommunityProjects = new();
        public FarmAnimalRecords Animals = new();
        public int Energy = FarmGameState.MaxEnergy;
        public FarmMasteryProgress Mastery = new();
        public List<FarmPlacedObjectSaveData> PlacedObjects = new();
        public List<FarmResourceNodeSaveData> ResourceNodes = new();
        public List<FarmProcessingJobSaveData> ProcessingJobs = new();
        public int LastMorningAutomationDay;
        public List<string> ReadMailIds = new();
        public List<string> ClaimedMailIds = new();
        public List<string> DiscoveredItemIds = new();
        public List<string> DiscoveredRecipeIds = new();
        public int CollectionMilestoneMask;
        public FarmCoopRoleProgress CoopRoles = new();
        public FarmForecastPlan ForecastPlan = new();
        public FarmHomesteadRestProgress HomesteadRest = new();
        public int PumpkinSeeds;
        public int Pumpkins;
    }

    public sealed class FarmGameState : MonoBehaviour
    {
        public const string PumpkinSeedId = "pumpkin_seed";
        public const string PumpkinId = "pumpkin";
        public const string CompostId = "compost";
        public const string ToolPrefix = "tool:";
        public const string ItemPrefix = "item:";
        public const int HotbarSlotCount = 8;
        public const int DefaultWorldSeed = 7122040;
        public const int MaxToolLevel = 3;
        public const int MaxEnergy = 100;
        public const int MinLandLevel = 1;
        public const int MaxLandLevel = 3;
        public const string FarmShedKitId = "farm_shed_kit";
        public const int FarmShedStorageSlotBonus = 15;
        public const int BaseProcessingQueueCapacity = 3;

        private static readonly string[] DefaultHotbarEntries =
        {
            "tool:hoe", "item:pumpkin_seed", "tool:watering_can", "tool:harvest", "tool:pickaxe", "tool:axe", "", ""
        };

        [SerializeField] private int money = 100;
        [SerializeField, Min(1)] private int slotCapacity = 20;
        [SerializeField] private List<InventoryStack> inventory = new();
        [SerializeField] private List<string> hotbar = new();
        [SerializeField] private int selectedHotbarIndex;
        [SerializeField] private FarmTutorialProgress tutorial = new();
        [SerializeField, Min(1)] private int storageSlotCapacity = 30;
        [SerializeField] private List<InventoryStack> storage = new();
        [SerializeField] private List<string> collectedPickupIds = new();
        [SerializeField, Min(1)] private int dayNumber = 1;
        [SerializeField, Range(0f, 1439.99f)] private float minutesOfDay = 480f;
        [SerializeField] private int worldSeed = DefaultWorldSeed;
        [SerializeField, Range(1, MaxToolLevel)] private int hoeLevel = 1;
        [SerializeField, Range(1, MaxToolLevel)] private int wateringCanLevel = 1;
        [SerializeField, Range(1, MaxToolLevel)] private int harvestLevel = 1;
        [SerializeField] private FarmJournalProgress journal = new();
        [SerializeField] private FarmDailyOrderProgress dailyOrders = new();
        [SerializeField] private FarmCommunityProgress community = new();
        [SerializeField] private FarmFestivalProgress festival = new();
        [SerializeField] private FarmCommunityProjectProgress communityProjects = new();
        [SerializeField] private FarmAnimalRecords animals = new();
        [SerializeField, Range(0, MaxEnergy)] private int energy = MaxEnergy;
        [SerializeField] private FarmMasteryProgress mastery = new();
        [SerializeField] private List<FarmPlacedObjectSaveData> placedObjects = new();
        [SerializeField] private List<FarmResourceNodeSaveData> resourceNodes = new();
        [SerializeField] private List<FarmProcessingJobSaveData> processingJobs = new();
        [SerializeField] private int lastMorningAutomationDay;
        [SerializeField] private List<string> readMailIds = new();
        [SerializeField] private List<string> claimedMailIds = new();
        [SerializeField] private List<string> discoveredItemIds = new();
        [SerializeField] private List<string> discoveredRecipeIds = new();
        [SerializeField] private int collectionMilestoneMask;
        [SerializeField] private FarmCoopRoleProgress coopRoles = new();
        [SerializeField] private FarmForecastPlan forecastPlan = new();
        [SerializeField] private FarmHomesteadRestProgress homesteadRest = new();
        [SerializeField, Range(MinLandLevel, MaxLandLevel)] private int landLevel = MinLandLevel;
        private bool lastEnergyActionWasFree;

        public int Money => money;
        public int SlotCapacity => slotCapacity;
        public int UsedSlots => inventory.Count;
        public IReadOnlyList<InventoryStack> Inventory => inventory;
        public IReadOnlyList<string> Hotbar => hotbar;
        public int SelectedHotbarIndex => selectedHotbarIndex;
        public string SelectedHotbarEntry => GetHotbarEntry(selectedHotbarIndex);
        public FarmTutorialProgress Tutorial => tutorial;
        public int StorageSlotCapacity => storageSlotCapacity + (CountPlacedItem(FarmShedKitId) * FarmShedStorageSlotBonus);
        public int StorageUsedSlots => storage.Count;
        public IReadOnlyList<InventoryStack> Storage => storage;
        public IReadOnlyList<string> CollectedPickupIds => collectedPickupIds;
        public int DayNumber => dayNumber;
        public float MinutesOfDay => minutesOfDay;
        public int WorldSeed => worldSeed;
        public int HoeLevel => hoeLevel;
        public int WateringCanLevel => wateringCanLevel;
        public int HarvestLevel => harvestLevel;
        public FarmJournalProgress Journal => journal;
        public FarmDailyOrderProgress DailyOrders
        {
            get
            {
                EnsureDailyOrdersForCurrentDay(false);
                return dailyOrders;
            }
        }
        public FarmCommunityProgress Community => community ??= new FarmCommunityProgress();
        public FarmFestivalProgress Festival => festival ??= new FarmFestivalProgress();
        public FarmCommunityProjectProgress CommunityProjects => communityProjects ??= new FarmCommunityProjectProgress();
        public FarmAnimalRecords Animals
        {
            get
            {
                if (animals == null || animals.Chickens == null || animals.Chickens.Count == 0) animals = CreateStarterAnimalRecords();
                animals.EnsureNormalized();
                return animals;
            }
        }
        public int Energy => energy;
        public float EnergyRatio => energy / (float)MaxEnergy;
        public bool IsExhausted => energy <= 0;
        public FarmMasteryProgress Mastery => mastery;
        public FarmSpecialization Specialization => (mastery ??= new FarmMasteryProgress()).Specialization;
        public IReadOnlyList<FarmPlacedObjectSaveData> PlacedObjects => placedObjects;
        public IReadOnlyList<FarmResourceNodeSaveData> ResourceNodes => resourceNodes;
        public IReadOnlyList<FarmProcessingJobSaveData> ProcessingJobs => processingJobs;
        public int ProcessingQueueCount => processingJobs.Count;
        public int EffectiveProcessingQueueCapacity => BaseProcessingQueueCapacity + (HasNeighborhoodUnlock("niko") ? 1 : 0);
        public int LastMorningAutomationDay => lastMorningAutomationDay;
        public IReadOnlyList<string> ReadMailIds => readMailIds;
        public IReadOnlyList<string> ClaimedMailIds => claimedMailIds;
        public IReadOnlyList<string> DiscoveredItemIds => discoveredItemIds;
        public IReadOnlyList<string> DiscoveredRecipeIds => discoveredRecipeIds;
        public int CollectionMilestoneMask => collectionMilestoneMask;
        public IReadOnlyList<FarmPlayerRoleProfile> CoopRoleProfiles => CoopRoles.Players;
        public FarmForecastPlan ForecastPlan => forecastPlan ??= new FarmForecastPlan();
        public FarmHomesteadRestProgress HomesteadRest => homesteadRest ??= new FarmHomesteadRestProgress();
        public int ComfortCharges => HomesteadRest.ComfortDay == dayNumber ? HomesteadRest.ComfortCharges : 0;
        public FarmCoopRoleProgress CoopRoles
        {
            get
            {
                coopRoles ??= new FarmCoopRoleProgress();
                coopRoles.EnsureNormalized();
                return coopRoles;
            }
        }
        public bool HasNeighborhoodUnlock(string contactId) => FarmCommunityCatalog.HasNeighborhoodUnlock(Community, contactId);

        public bool HasForecastPlanForRoute(int day, string routeKey) =>
            ForecastPlan.TargetDay == Mathf.Max(1, day) &&
            !string.IsNullOrWhiteSpace(routeKey) &&
            string.Equals(ForecastPlan.RouteKey, routeKey, StringComparison.OrdinalIgnoreCase);

        public bool TryPrepareTomorrowForecastPlan(out string routeKey, out string error)
        {
            routeKey = string.Empty;
            error = string.Empty;
            var targetDay = Mathf.Max(1, dayNumber + 1);
            if (HomesteadRest.PreparedOnDay == dayNumber)
            {
                error = FarmLocalization.Get("evening.choice.comfort_active", "Tomorrow is already prepared for comfort. Choose one evening preparation per farm.");
                return false;
            }
            routeKey = FarmForecastPlanRules.RouteKeyForWeather(FarmWeatherSystem.WeatherForDay(worldSeed, targetDay));
            if (string.IsNullOrWhiteSpace(routeKey))
            {
                error = FarmLocalization.Get("forecast.plan.unavailable", "No route preparation is available for that forecast.");
                return false;
            }
            if (ForecastPlan.TargetDay == targetDay && string.Equals(ForecastPlan.RouteKey, routeKey, StringComparison.OrdinalIgnoreCase)) return true;
            forecastPlan = new FarmForecastPlan { TargetDay = targetDay, RouteKey = routeKey };
            NotifyChanged();
            return true;
        }

        public bool TryPrepareEveningTea(out string error)
        {
            error = string.Empty;
            if (ForecastPlan.TargetDay == dayNumber + 1)
            {
                error = FarmLocalization.Get("evening.choice.route_active", "Tomorrow's route is already prepared. Choose one evening preparation per farm.");
                return false;
            }
            if (HomesteadRest.PreparedOnDay == dayNumber)
            {
                error = FarmLocalization.Get("rest.already_prepared", "The homestead is already prepared for tomorrow.");
                return false;
            }
            if (!TryRemoveItem(FarmHomesteadRestRules.EveningTeaItemId, 1))
            {
                error = FarmLocalization.Get("rest.tea_missing", "You need Wildflower Tea for an evening tea.");
                return false;
            }
            HomesteadRest.PreparedOnDay = dayNumber;
            NotifyChanged();
            return true;
        }

        public bool BeginHomesteadRestDay(int newDay)
        {
            newDay = Mathf.Max(1, newDay);
            var prepared = HomesteadRest.PreparedOnDay == newDay - 1;
            HomesteadRest.ComfortDay = newDay;
            HomesteadRest.ComfortCharges = prepared ? FarmHomesteadRestRules.ComfortCharges : 0;
            if (HomesteadRest.PreparedOnDay < newDay) HomesteadRest.PreparedOnDay = 0;
            NotifyChanged();
            return prepared;
        }
        public int LandLevel => landLevel;
        public int LandTileCount => GetLandTileCount(landLevel);
        public bool IsLandMaxed => landLevel >= MaxLandLevel;
        public bool LastEnergyActionWasFree => lastEnergyActionWasFree;
        public int PumpkinSeeds => GetQuantity(PumpkinSeedId);
        public int Pumpkins => GetQuantity(PumpkinId);
        public FarmMarketQuote GetMarketQuote(string itemId, int dayOffset = 0) =>
            FarmMarketRules.Quote(worldSeed, Mathf.Max(1, dayNumber + dayOffset), itemId);
        public int GetMarketUnitPrice(string itemId, FarmItemQuality quality, int dayOffset = 0) =>
            FarmMarketRules.UnitPrice(
                FarmContentDatabase.GetItem(itemId),
                quality,
                worldSeed,
                Mathf.Max(1, dayNumber + dayOffset));

        /// <summary>
        /// Deterministic coverage query for host-authoritative farm rules.
        /// Uses persisted placements rather than scene colliders or visuals.
        /// </summary>
        public bool IsCoveredByBuildableFunction(Vector3 worldPosition, FarmBuildableFunction function)
        {
            foreach (var placed in placedObjects)
            {
                if (placed == null) continue;
                var definition = FarmBuildableDatabase.GetByItemId(placed.ItemId);
                if (definition == null || definition.Function != function || definition.EffectRadius <= 0f) continue;
                var delta = worldPosition - new Vector3(placed.X, worldPosition.y, placed.Z);
                delta.y = 0f;
                var radius = Mathf.Max(0.5f, definition.EffectRadius);
                if (delta.sqrMagnitude <= radius * radius) return true;
            }
            return false;
        }
        public event Action Changed;

        /// <summary>
        /// Atualiza apenas o cache de apresentacao a partir de uma resposta confirmada pelo backend.
        /// Nao valida nem calcula economia localmente.
        /// </summary>
        public void ApplyAuthoritativeInventorySnapshot(
            IReadOnlyList<FarmInventoryEntry> confirmedItems,
            IReadOnlyList<string> confirmedHotbar,
            int confirmedSelectedHotbarIndex)
        {
            inventory ??= new List<InventoryStack>();
            hotbar ??= new List<string>();
            inventory.Clear();
            foreach (var entry in confirmedItems ?? Array.Empty<FarmInventoryEntry>())
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId) || entry.Quantity <= 0) continue;
                if (FarmContentDatabase.GetItem(entry.ItemId) == null) continue;
                AddInternal(entry.ItemId, entry.Quantity, entry.Quality);
            }

            hotbar.Clear();
            foreach (var entry in confirmedHotbar ?? Array.Empty<string>())
            {
                if (hotbar.Count >= HotbarSlotCount) break;
                hotbar.Add(entry ?? string.Empty);
            }
            while (hotbar.Count < HotbarSlotCount) hotbar.Add(string.Empty);
            selectedHotbarIndex = Mathf.Clamp(confirmedSelectedHotbarIndex, 0, HotbarSlotCount - 1);
            NotifyChanged();
        }

        private void Awake()
        {
            if (inventory.Count == 0)
            {
                AddInternal(PumpkinSeedId, 12);
                AddInternal(CompostId, 2);
            }
            energy = Mathf.Clamp(energy, 0, MaxEnergy);
            mastery ??= new FarmMasteryProgress();
            if (animals == null || animals.Chickens == null || animals.Chickens.Count == 0) animals = CreateStarterAnimalRecords();
            else animals.EnsureNormalized();
            readMailIds ??= new List<string>();
            claimedMailIds ??= new List<string>();
            discoveredItemIds ??= new List<string>();
            discoveredRecipeIds ??= new List<string>();
            CoopRoles.EnsureNormalized();
            DiscoverRecipesForKnownItems(false);
            EnsureHotbar();
        }

        public bool IsItemDiscovered(string itemId)
        {
            if (discoveredItemIds == null || string.IsNullOrWhiteSpace(itemId)) return false;
            foreach (var id in discoveredItemIds)
                if (string.Equals(id, itemId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public bool RecordDiscoveredItem(string itemId, bool notify = true)
        {
            if (string.IsNullOrWhiteSpace(itemId) || FarmContentDatabase.GetItem(itemId) == null || IsItemDiscovered(itemId)) return false;
            discoveredItemIds ??= new List<string>();
            discoveredItemIds.Add(itemId);
            DiscoverRecipesForIngredient(itemId, false);
            if (notify) NotifyChanged();
            return true;
        }

        public bool IsRecipeDiscovered(string recipeId)
        {
            if (discoveredRecipeIds == null || string.IsNullOrWhiteSpace(recipeId)) return false;
            foreach (var id in discoveredRecipeIds)
                if (string.Equals(id, recipeId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public bool DiscoverRecipe(string recipeId, bool notify = true)
        {
            if (string.IsNullOrWhiteSpace(recipeId) || IsRecipeDiscovered(recipeId)) return false;
            CraftingRecipe recipe = null;
            foreach (var candidate in FarmCraftingDatabase.Recipes)
                if (candidate != null && string.Equals(candidate.Id, recipeId, StringComparison.OrdinalIgnoreCase)) { recipe = candidate; break; }
            if (recipe == null || !recipe.RequiresDiscovery) return false;
            discoveredRecipeIds ??= new List<string>();
            discoveredRecipeIds.Add(recipe.Id);
            if (notify) NotifyChanged();
            return true;
        }

        private void DiscoverRecipesForIngredient(string itemId, bool notify)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            var any = false;
            foreach (var recipe in FarmCraftingDatabase.Recipes)
            {
                if (recipe == null || !recipe.RequiresDiscovery || recipe.Ingredients == null) continue;
                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient?.Item == null || !string.Equals(ingredient.Item.Id, itemId, StringComparison.OrdinalIgnoreCase)) continue;
                    any |= DiscoverRecipe(recipe.Id, false);
                    break;
                }
            }
            if (notify && any) NotifyChanged();
        }

        private void DiscoverRecipesForKnownItems(bool notify)
        {
            var any = false;
            foreach (var itemId in discoveredItemIds ?? new List<string>())
            {
                var before = discoveredRecipeIds?.Count ?? 0;
                DiscoverRecipesForIngredient(itemId, false);
                any |= (discoveredRecipeIds?.Count ?? 0) > before;
            }
            if (notify && any) NotifyChanged();
        }

        public int CountDiscoveredItems()
        {
            var count = 0;
            foreach (var definition in FarmContentDatabase.Items)
                if (definition != null && IsItemDiscovered(definition.Id)) count++;
            return count;
        }

        public bool TryClaimNextCollectionMilestone(out FarmCollectionMilestone milestone, out string error)
        {
            milestone = default; error = string.Empty;
            var discovered = CountDiscoveredItems();
            for (var index = 0; index < FarmCollectionMilestoneCatalog.Count; index++)
            {
                if ((collectionMilestoneMask & (1 << index)) != 0) continue;
                var candidate = FarmCollectionMilestoneCatalog.Get(index);
                if (discovered < candidate.Threshold) { error = FarmLocalization.Format("collection.milestone.locked", "Discover {0} items to claim the next collection reward.", candidate.Threshold); return false; }
                if (!CanAdd(candidate.RewardItemId, candidate.RewardAmount)) { error = FarmLocalization.Get("collection.milestone.inventory_full", "Make room before claiming the collection reward."); return false; }
                collectionMilestoneMask |= 1 << index; AddInternal(candidate.RewardItemId, candidate.RewardAmount); NotifyChanged(); milestone = candidate; return true;
            }
            error = FarmLocalization.Get("collection.milestone.complete", "All collection rewards have been claimed."); return false;
        }

        public static int EnergyCostPerTile(FarmTool tool) => tool switch
        {
            FarmTool.Hoe => 4,
            FarmTool.Seeds => 1,
            FarmTool.WateringCan => 3,
            FarmTool.Fertilizer => 2,
            FarmTool.Harvest => 2,
            FarmTool.Axe => 3,
            FarmTool.Pickaxe => 3,
            _ => 0
        };

        private bool CanUseComfortCharge() => HomesteadRest.ComfortDay == dayNumber && HomesteadRest.ComfortCharges > 0;

        private void ConsumeComfortCharge()
        {
            if (!CanUseComfortCharge()) return;
            HomesteadRest.ComfortCharges--;
        }

        public int SpendEnergy(FarmTool tool, int changedTiles)
        {
            lastEnergyActionWasFree = false;
            var requested = EnergyCostPerTile(tool) * Mathf.Max(0, changedTiles);
            if (requested <= 0) return 0;
            var cultivationAction = tool is FarmTool.Hoe or FarmTool.Seeds or FarmTool.WateringCan or FarmTool.Fertilizer;
            if (cultivationAction && GetMasteryLevel(FarmMasterySkill.Cultivation) >= 2 && mastery.FreeCultivationDay != dayNumber)
            {
                mastery.FreeCultivationDay = dayNumber;
                lastEnergyActionWasFree = true;
                NotifyChanged();
                return 0;
            }
            var usedComfort = CanUseComfortCharge() && energy > 0;
            if (usedComfort) requested = Mathf.Max(0, requested - FarmHomesteadRestRules.EnergyDiscountPerCharge);
            var previous = energy;
            energy = Mathf.Max(0, energy - requested);
            if (usedComfort) ConsumeComfortCharge();
            if (energy != previous || usedComfort) NotifyChanged();
            return previous - energy;
        }

        public bool TrySpendEnergy(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount <= 0) return true;
            var usedComfort = CanUseComfortCharge() && energy >= Mathf.Max(0, amount - FarmHomesteadRestRules.EnergyDiscountPerCharge);
            var requested = usedComfort ? Mathf.Max(0, amount - FarmHomesteadRestRules.EnergyDiscountPerCharge) : amount;
            if (energy < requested) return false;
            energy -= requested;
            if (usedComfort) ConsumeComfortCharge();
            NotifyChanged();
            return true;
        }

        public int RestoreEnergy()
        {
            var recovered = MaxEnergy - energy;
            if (recovered <= 0) return 0;
            energy = MaxEnergy;
            NotifyChanged();
            return recovered;
        }

        public int RestoreEnergy(int amount)
        {
            var requested = Mathf.Max(0, amount);
            if (requested <= 0 || energy >= MaxEnergy) return 0;
            var recovered = Mathf.Min(requested, MaxEnergy - energy);
            energy += recovered;
            NotifyChanged();
            return recovered;
        }

        public int GetQuantity(string itemId)
        {
            var total = 0;
            foreach (var stack in inventory)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase)) total += stack.Quantity;
            return total;
        }

        /// <summary>
        /// Records a server-confirmed world action which has no inventory reward.
        /// The identifier is persisted with world pickups so daily world content can
        /// be safely reconstructed after load and later verified by a co-op host.
        /// </summary>
        public bool TryRecordWorldAction(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId) || IsPickupCollected(actionId)) return false;
            collectedPickupIds.Add(actionId);
            NotifyChanged();
            return true;
        }

        /// <summary>Refreshes UI/save observers after a host-approved mutation of persistent animal records.</summary>
        public void NotifyAnimalsChanged() => NotifyChanged();

        public int GetQuantity(string itemId, FarmItemQuality quality) =>
            GetQuantityInList(inventory, itemId, quality);

        public bool TryGetHighestQualityWithQuantity(string itemId, int amount, out FarmItemQuality quality)
        {
            quality = FarmItemQuality.Normal;
            if (string.IsNullOrWhiteSpace(itemId) || amount < 1) return false;
            for (var value = (int)FarmItemQuality.Gold; value >= (int)FarmItemQuality.Normal; value--)
            {
                var candidate = (FarmItemQuality)value;
                if (GetQuantity(itemId, candidate) < amount) continue;
                quality = candidate;
                return true;
            }
            return false;
        }

        public bool CanAdd(string itemId, int amount) =>
            CanAdd(itemId, amount, FarmItemQuality.Normal);

        public bool CanAdd(string itemId, int amount, FarmItemQuality quality) =>
            CanAddToList(inventory, slotCapacity, itemId, amount, quality);

        public bool AddItem(string itemId, int amount) =>
            AddItem(itemId, amount, FarmItemQuality.Normal);

        public bool AddItem(string itemId, int amount, FarmItemQuality quality)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 || !CanAdd(itemId, amount, quality)) return false;
            AddInternal(itemId, amount, quality);
            NotifyChanged();
            return true;
        }

        public bool TryRemoveItem(string itemId, int amount)
        {
            if (amount <= 0) return true;
            if (GetQuantity(itemId) < amount) return false;
            RemoveFromList(inventory, itemId, amount);
            NotifyChanged();
            return true;
        }

        public bool TryRemoveItem(string itemId, FarmItemQuality quality, int amount)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            if (amount <= 0) return true;
            if (GetQuantity(itemId, quality) < amount) return false;
            RemoveFromListExact(inventory, itemId, quality, amount);
            NotifyChanged();
            return true;
        }
        public string GetHotbarEntry(int index)
        {
            EnsureHotbar();
            return index >= 0 && index < hotbar.Count ? hotbar[index] ?? string.Empty : string.Empty;
        }

        public bool ContainsHotbarEntry(string entry)
        {
            EnsureHotbar();
            foreach (var currentEntry in hotbar)
                if (string.Equals(currentEntry, entry, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static bool IsCoreToolEntry(string entry) =>
            entry is "tool:hoe" or "tool:watering_can" or "tool:harvest" or "tool:axe" or "tool:pickaxe";

        public static bool TryGetDefaultCoreTool(int slotIndex, out string entry)
        {
            entry = slotIndex switch
            {
                0 => "tool:hoe",
                2 => "tool:watering_can",
                3 => "tool:harvest",
                4 => "tool:pickaxe",
                5 => "tool:axe",
                _ => string.Empty
            };
            return !string.IsNullOrEmpty(entry);
        }

        public bool AssignHotbarSlot(int index, string entry)
        {
            EnsureHotbar();
            if (index < 0 || index >= HotbarSlotCount || !IsValidHotbarEntry(entry)) return false;
            entry ??= string.Empty;
            if (entry.StartsWith(ItemPrefix, StringComparison.OrdinalIgnoreCase) &&
                GetQuantity(entry[ItemPrefix.Length..]) <= 0) return false;
            if (string.Equals(hotbar[index], entry, StringComparison.OrdinalIgnoreCase)) return true;
            hotbar[index] = entry;
            NotifyChanged();
            return true;
        }

        public void ClearHotbarSlot(int index)
        {
            if (index < 0 || index >= HotbarSlotCount) return;
            EnsureHotbar();
            if (string.IsNullOrEmpty(hotbar[index])) return;
            hotbar[index] = string.Empty;
            NotifyChanged();
        }

        public bool SwapHotbarSlots(int sourceIndex, int targetIndex)
        {
            EnsureHotbar();
            if (sourceIndex < 0 || sourceIndex >= HotbarSlotCount || targetIndex < 0 || targetIndex >= HotbarSlotCount) return false;
            if (sourceIndex == targetIndex) return true;
            (hotbar[sourceIndex], hotbar[targetIndex]) = (hotbar[targetIndex], hotbar[sourceIndex]);
            if (selectedHotbarIndex == sourceIndex) selectedHotbarIndex = targetIndex;
            else if (selectedHotbarIndex == targetIndex) selectedHotbarIndex = sourceIndex;
            NotifyChanged();
            return true;
        }

        public bool SelectHotbarSlot(int index)
        {
            EnsureHotbar();
            if (index < 0 || index >= HotbarSlotCount) return false;
            if (selectedHotbarIndex == index) return true;
            selectedHotbarIndex = index;
            NotifyChanged();
            return true;
        }

        public int GetStorageQuantity(string itemId)
        {
            var total = 0;
            foreach (var stack in storage)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase)) total += stack.Quantity;
            return total;
        }

        public int GetStorageQuantity(string itemId, FarmItemQuality quality) =>
            GetQuantityInList(storage, itemId, quality);

        public bool TransferToStorage(string itemId, int amount) =>
            TransferToStorage(itemId, FarmItemQuality.Normal, amount);

        public bool TransferToStorage(string itemId, FarmItemQuality quality, int amount)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 ||
                GetQuantity(itemId, quality) < amount || !CanAddToStorage(itemId, amount, quality)) return false;
            RemoveFromListExact(inventory, itemId, quality, amount);
            AddToStorageInternal(itemId, amount, quality);
            NotifyChanged();
            return true;
        }

        public bool TransferFromStorage(string itemId, int amount) =>
            TransferFromStorage(itemId, FarmItemQuality.Normal, amount);

        public bool TransferFromStorage(string itemId, FarmItemQuality quality, int amount)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 ||
                GetStorageQuantity(itemId, quality) < amount || !CanAdd(itemId, amount, quality)) return false;
            RemoveFromListExact(storage, itemId, quality, amount);
            AddInternal(itemId, amount, quality);
            NotifyChanged();
            return true;
        }
        public bool SortInventory()
        {
            return SortStacks(inventory);
        }

        public bool SortStorage()
        {
            return SortStacks(storage);
        }

        private bool SortStacks(List<InventoryStack> stacks)
        {
            if (stacks == null || stacks.Count <= 1) return false;
            var previous = new List<InventoryStack>(stacks.Count);
            foreach (var stack in stacks) previous.Add(new InventoryStack(stack.ItemId, stack.Quantity, stack.Quality));
            stacks.Sort(CompareInventoryStacks);
            var changed = false;
            for (var index = 0; index < stacks.Count; index++)
            {
                if (string.Equals(stacks[index].ItemId, previous[index].ItemId, StringComparison.OrdinalIgnoreCase) &&
                    stacks[index].Quantity == previous[index].Quantity &&
                    stacks[index].Quality == previous[index].Quality) continue;
                changed = true;
                break;
            }
            if (changed) NotifyChanged();
            return changed;
        }

        private static int CompareInventoryStacks(InventoryStack left, InventoryStack right)
        {
            var leftDefinition = left != null ? FarmContentDatabase.GetItem(left.ItemId) : null;
            var rightDefinition = right != null ? FarmContentDatabase.GetItem(right.ItemId) : null;
            var category = InventoryCategoryOrder(leftDefinition).CompareTo(InventoryCategoryOrder(rightDefinition));
            if (category != 0) return category;
            var name = string.Compare(
                leftDefinition != null ? leftDefinition.LocalizedName : left?.ItemId,
                rightDefinition != null ? rightDefinition.LocalizedName : right?.ItemId,
                StringComparison.CurrentCultureIgnoreCase);
            if (name != 0) return name;
            var itemId = string.Compare(left?.ItemId, right?.ItemId, StringComparison.OrdinalIgnoreCase);
            if (itemId != 0) return itemId;
            return FarmItemQualityRules.Clamp(right != null ? right.Quality : FarmItemQuality.Normal)
                .CompareTo(FarmItemQualityRules.Clamp(left != null ? left.Quality : FarmItemQuality.Normal));
        }
        private static int InventoryCategoryOrder(ItemDefinition definition)
        {
            if (definition == null) return 5;
            if (definition.Category == ItemCategory.Seed) return 0;
            if (definition.Category == ItemCategory.Crop) return 1;
            if (definition.Category == ItemCategory.Material &&
                !definition.Id.EndsWith("_kit", StringComparison.OrdinalIgnoreCase)) return 2;
            if (definition.Category == ItemCategory.Material) return 3;
            if (definition.Category == ItemCategory.Tool) return 4;
            return 5;
        }

        public bool IsPickupCollected(string pickupId)
        {
            if (string.IsNullOrWhiteSpace(pickupId)) return false;
            foreach (var collectedId in collectedPickupIds)
                if (string.Equals(collectedId, pickupId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public bool TryCollectPickup(string pickupId, string itemId, int amount) =>
            TryCollectPickup(pickupId, itemId, amount, FarmItemQuality.Normal);

        public bool TryCollectPickup(string pickupId, string itemId, int amount, FarmItemQuality quality)
        {
            if (string.IsNullOrWhiteSpace(pickupId) || string.IsNullOrWhiteSpace(itemId) || amount <= 0) return false;
            quality = FarmItemQualityRules.Clamp(quality);
            if (IsPickupCollected(pickupId) || !CanAdd(itemId, amount, quality)) return false;
            AddInternal(itemId, amount, quality);
            collectedPickupIds.Add(pickupId);
            RecordDiscoveredItem(itemId, false);
            RecordJournal(FarmJournalMetric.WorldPickups, 1, null, false);
            AddMasteryExperience(FarmMasterySkill.Harvesting, 1, false);
            NotifyChanged();
            return true;
        }

        public bool AdvanceClock(float gameMinutes, bool notify = false)
        {
            var previousDay = dayNumber;
            var totalMinutes = Mathf.Max(0f, ((dayNumber - 1) * 1440f) + minutesOfDay + gameMinutes);
            dayNumber = Mathf.FloorToInt(totalMinutes / 1440f) + 1;
            minutesOfDay = Mathf.Repeat(totalMinutes, 1440f);
            var dayChanged = dayNumber != previousDay;
            if (dayChanged) EnsureDailyOrdersForCurrentDay(false);
            if (notify) NotifyChanged();
            return dayChanged;
        }

        public void SetClock(int day, float minute, bool notify = false)
        {
            var totalMinutes = Mathf.Max(0f, ((Mathf.Max(1, day) - 1) * 1440f) + minute);
            dayNumber = Mathf.FloorToInt(totalMinutes / 1440f) + 1;
            minutesOfDay = Mathf.Repeat(totalMinutes, 1440f);
            EnsureDailyOrdersForCurrentDay(false);
            if (ForecastPlan.TargetDay > 0 && ForecastPlan.TargetDay < dayNumber) forecastPlan = new FarmForecastPlan();
            if (notify) NotifyChanged();
        }

        public void SetWorldSeed(int seed, bool notify = false)
        {
            worldSeed = seed == 0 ? DefaultWorldSeed : seed;
            if (notify) NotifyChanged();
        }

        public void RecordJournal(FarmJournalMetric metric, int amount = 1, string cropId = null, bool notify = true)
        {
            journal ??= new FarmJournalProgress();
            amount = Mathf.Max(0, amount);
            switch (metric)
            {
                case FarmJournalMetric.Tilled: journal.Tilled += amount; break;
                case FarmJournalMetric.Planted: journal.Planted += amount; break;
                case FarmJournalMetric.Watered: journal.Watered += amount; break;
                case FarmJournalMetric.HarvestedUnits:
                    journal.HarvestedUnits += amount;
                    journal.RecordCrop(cropId);
                    break;
                case FarmJournalMetric.SoldUnits: journal.SoldUnits += amount; break;
                case FarmJournalMetric.SeedPacksBought: journal.SeedPacksBought += amount; break;
                case FarmJournalMetric.WorldPickups: journal.WorldPickups += amount; break;
                case FarmJournalMetric.ToolUpgrades: journal.ToolUpgrades += amount; break;
                case FarmJournalMetric.OrdersDelivered: journal.OrdersDelivered += amount; break;
            }
            if (notify) NotifyChanged();
        }

        public FarmPlayerRoleProfile GetCoopRoleProfile(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return null;
            foreach (var profile in CoopRoles.Players)
                if (profile != null && string.Equals(profile.PlayerId, playerId.Trim(), StringComparison.OrdinalIgnoreCase)) return profile;
            return null;
        }

        public bool TrySetCoopRole(string playerId, FarmSpecialization role, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(playerId) || role == FarmSpecialization.None)
            {
                error = FarmLocalization.Get("roles.invalid", "Choose a valid co-op role.");
                return false;
            }

            var profile = GetCoopRoleProfile(playerId);
            if (profile == null)
            {
                if (CoopRoles.Players.Count >= FarmCoopRoleRules.MaxPlayers)
                {
                    error = FarmLocalization.Get("roles.full", "This farm already has four co-op role profiles.");
                    return false;
                }
                profile = new FarmPlayerRoleProfile { PlayerId = playerId.Trim() };
                CoopRoles.Players.Add(profile);
            }
            if (profile.MatchingOrderContributions > 0 && profile.Role != role)
            {
                error = FarmLocalization.Get("roles.locked", "Your co-op role is committed after contributing an order.");
                return false;
            }

            if (profile.Role == role) return true;
            profile.Role = role;
            NotifyChanged();
            return true;
        }

        public bool TryCompleteDailyOrder(FarmDailyOrder order, int index, out int earned, out int completionBonus, out FarmCommunityDeliveryResult communityResult, out string error) =>
            TryCompleteDailyOrder(order, index, "local", out earned, out completionBonus, out communityResult, out _, out error);

        public bool TryCompleteDailyOrder(FarmDailyOrder order, int index, string requestedBy, out int earned, out int completionBonus, out FarmCommunityDeliveryResult communityResult, out FarmRoleOrderContribution roleContribution, out string error)
        {
            earned = 0;
            completionBonus = 0;
            communityResult = default;
            roleContribution = default;
            error = string.Empty;
            EnsureDailyOrdersForCurrentDay(false);
            if (order == null || order.Day != dayNumber || index < 0 || index >= FarmDailyOrderGenerator.OrderCount)
            {
                error = FarmLocalization.Get("state.order.wrong_day", "This order does not belong to the current day.");
                return false;
            }
            if (dailyOrders.IsCompleted(index))
            {
                error = FarmLocalization.Get("state.order.delivered", "This order has already been delivered.");
                return false;
            }
            var item = order.Item;
            if (item == null || string.IsNullOrWhiteSpace(order.ItemId))
            {
                error = FarmLocalization.Get("state.order.invalid_item", "The order has an invalid item.");
                return false;
            }
            var inventoryQuantity = GetQuantity(item.Id);
            var storageQuantity = (GetMasteryLevel(FarmMasterySkill.Commerce) >= 3 || CommunityProjects.MarketRouteComplete) ? GetStorageQuantity(item.Id) : 0;
            var available = inventoryQuantity + storageQuantity;
            if (available < order.Quantity)
            {
                error = FarmLocalization.Format("state.order.missing_item", "Missing {0} {1}.", order.Quantity - available, item.LocalizedName.ToLowerInvariant());
                return false;
            }

            var fromInventory = Mathf.Min(inventoryQuantity, order.Quantity);
            RemoveFromList(inventory, item.Id, fromInventory);
            if (fromInventory < order.Quantity) RemoveFromList(storage, item.Id, order.Quantity - fromInventory);
            dailyOrders.MarkCompleted(index);
            if (dailyOrders.IsBoardComplete(FarmDailyOrderGenerator.OrderCount)) completionBonus = FarmDailyOrderGenerator.BoardCompletionBonus;
            earned = order.Reward + completionBonus;
            money += earned;
            var contactId = order.RequesterId;
            var favorBefore = Community.GetFavor(contactId);
            var levelBefore = FarmCommunityCatalog.GetBondLevel(favorBefore);
            var favorAfter = Community.AddFavor(contactId, 1);
            var levelAfter = FarmCommunityCatalog.GetBondLevel(favorAfter);
            var milestoneReward = levelAfter > levelBefore ? FarmCommunityCatalog.GetMilestoneReward(levelAfter) : 0;
            if (milestoneReward > 0) money += milestoneReward;
            communityResult = new FarmCommunityDeliveryResult(contactId, 1, favorAfter, levelAfter, milestoneReward);
            RecordJournal(FarmJournalMetric.SoldUnits, order.Quantity, null, false);
            RecordJournal(FarmJournalMetric.OrdersDelivered, 1, null, false);
            AddMasteryExperience(FarmMasterySkill.Commerce, order.Quantity, false);
            roleContribution = RecordCoopRoleContribution(requestedBy, order);
            if (roleContribution.TeamworkBonus > 0)
            {
                earned += roleContribution.TeamworkBonus;
                money += roleContribution.TeamworkBonus;
            }
            NotifyChanged();
            return true;
        }

        private FarmRoleOrderContribution RecordCoopRoleContribution(string playerId, FarmDailyOrder order)
        {
            var recommended = order != null ? FarmCoopRoleRules.RecommendedRole(order.Type) : FarmSpecialization.None;
            var profile = GetCoopRoleProfile(playerId);
            if (profile == null || profile.Role != recommended || recommended == FarmSpecialization.None)
                return new FarmRoleOrderContribution(recommended, false, 0);

            var progress = CoopRoles;
            if (progress.TeamworkDay != dayNumber)
            {
                progress.TeamworkDay = dayNumber;
                progress.TeamworkRoleMask = 0;
            }
            profile.MatchingOrderContributions++;
            profile.LastContributionDay = dayNumber;
            progress.TeamworkRoleMask |= FarmCoopRoleRules.Mask(recommended);
            var teamworkBonus = progress.TeamworkRoleMask == FarmCoopRoleRules.RequiredRoleMask && progress.LastTeamworkBonusDay != dayNumber
                ? FarmCoopRoleRules.TeamworkBonus
                : 0;
            if (teamworkBonus > 0) progress.LastTeamworkBonusDay = dayNumber;
            return new FarmRoleOrderContribution(recommended, true, teamworkBonus);
        }

        public bool TryGiveCommunityGift(string contactId, string itemId, out FarmCommunityGiftResult giftResult, out string error)
        {
            giftResult = default;
            error = string.Empty;
            if (!FarmCommunityCatalog.IsKnownContact(contactId))
            {
                error = FarmLocalization.Get("gift.invalid", "That is not a suitable gift for this neighbor.");
                return false;
            }
            var contact = FarmCommunityCatalog.GetContact(contactId);
            var favorGained = FarmCommunityCatalog.FavorForGift(contact.Id, itemId);
            if (favorGained <= 0)
            {
                error = FarmLocalization.Get("gift.invalid", "That is not a suitable gift for this neighbor.");
                return false;
            }
            var bond = Community.Bonds.Find(candidate => candidate != null && string.Equals(candidate.ContactId, contact.Id, StringComparison.OrdinalIgnoreCase));
            if (bond != null && bond.LastGiftDay == dayNumber)
            {
                error = FarmLocalization.Get("gift.already_today", "This neighbor has already received a gift today.");
                return false;
            }
            if (!TryRemoveItem(itemId, 1))
            {
                error = FarmLocalization.Get("gift.missing", "That gift is no longer in the inventory.");
                return false;
            }
            var favorBefore = Community.GetFavor(contact.Id);
            var levelBefore = FarmCommunityCatalog.GetBondLevel(favorBefore);
            var favorAfter = Community.AddFavor(contact.Id, favorGained);
            bond = Community.Bonds.Find(candidate => candidate != null && string.Equals(candidate.ContactId, contact.Id, StringComparison.OrdinalIgnoreCase));
            if (bond != null) bond.LastGiftDay = dayNumber;
            var levelAfter = FarmCommunityCatalog.GetBondLevel(favorAfter);
            var milestoneReward = levelAfter > levelBefore ? FarmCommunityCatalog.GetMilestoneReward(levelAfter) : 0;
            if (milestoneReward > 0) money += milestoneReward;
            giftResult = new FarmCommunityGiftResult(contact.Id, itemId, favorGained, favorAfter, levelAfter, milestoneReward);
            NotifyChanged();
            return true;
        }

        public bool TryContributeFestival(string itemId, out int contributions, out bool completed, out int reward, out string error)
        {
            contributions = 0; completed = false; reward = 0; error = string.Empty;
            if (!FarmFestivalCatalog.TryGetActive(dayNumber, out var definition)) { error = FarmLocalization.Get("festival.inactive", "There is no active festival contribution today."); return false; }
            var item = FarmContentDatabase.GetItem(itemId);
            if (item == null || item.Category != ItemCategory.Crop) { error = FarmLocalization.Get("festival.crop_required", "Select a harvested crop to contribute."); return false; }
            if (!string.Equals(Festival.FestivalId, definition.Id, StringComparison.OrdinalIgnoreCase)) festival = new FarmFestivalProgress { FestivalId = definition.Id };
            if (Festival.Completed) { error = FarmLocalization.Get("festival.already_complete", "This festival target is already complete."); return false; }
            if (!TryRemoveItem(itemId, 1)) { error = FarmLocalization.Get("festival.missing", "That crop is no longer in the inventory."); return false; }
            Festival.Contributions++;
            contributions = Festival.Contributions;
            if (Festival.Contributions >= definition.Target)
            {
                Festival.Completed = true; completed = true; reward = definition.Reward; money += reward;
                foreach (var contact in FarmCommunityCatalog.AllContacts) Community.AddFavor(contact.Id, 1);
            }
            NotifyChanged();
            return true;
        }

        public bool TryContributeCommunityProject(string itemId, out bool completed, out string error)
        {
            completed = false; error = string.Empty;
            if (CommunityProjects.MarketRouteComplete) { error = FarmLocalization.Get("project.already_complete", "Market Route is already complete."); return false; }
            if (!FarmCommunityProjectCatalog.IsProjectMaterial(itemId) || !TryRemoveItem(itemId, 1)) { error = FarmLocalization.Get("project.material_required", "Select Wood, Stone, or Refined Stone from the inventory."); return false; }
            if (itemId == "wood" && CommunityProjects.Wood < FarmCommunityProjectCatalog.WoodTarget) CommunityProjects.Wood++;
            else if (itemId == "stone" && CommunityProjects.Stone < FarmCommunityProjectCatalog.StoneTarget) CommunityProjects.Stone++;
            else if (itemId == "refined_stone" && CommunityProjects.RefinedStone < FarmCommunityProjectCatalog.RefinedStoneTarget) CommunityProjects.RefinedStone++;
            else { AddItem(itemId, 1); error = FarmLocalization.Get("project.material_full", "That material target is already full."); return false; }
            completed = CommunityProjects.Wood >= FarmCommunityProjectCatalog.WoodTarget && CommunityProjects.Stone >= FarmCommunityProjectCatalog.StoneTarget && CommunityProjects.RefinedStone >= FarmCommunityProjectCatalog.RefinedStoneTarget;
            if (completed) CommunityProjects.MarketRouteComplete = true;
            NotifyChanged();
            return true;
        }

        private void EnsureDailyOrdersForCurrentDay(bool notify)
        {
            dailyOrders ??= new FarmDailyOrderProgress();
            if (dailyOrders.Day == dayNumber) return;
            dailyOrders.Day = dayNumber;
            dailyOrders.CompletedMask = 0;
            if (notify) NotifyChanged();
        }

        public bool TryClaimJournalReward(string questId, out int reward)
        {
            reward = 0;
            journal ??= new FarmJournalProgress();
            var definition = FarmJournalDatabase.Get(questId);
            if (definition == null || journal.HasClaimed(questId) || !definition.IsComplete(journal)) return false;
            journal.ClaimedQuestIds ??= new List<string>();
            journal.ClaimedQuestIds.Add(definition.Id);
            reward = definition.RewardMoney;
            money += reward;
            NotifyChanged();
            return true;
        }

        public bool MarkMilestone(FarmMilestone milestone)
        {
            tutorial ??= new FarmTutorialProgress();
            if (!tutorial.Mark(milestone)) return false;
            if (tutorial.IsComplete && !tutorial.RewardClaimed)
            {
                tutorial.RewardClaimed = true;
                money += 50;
            }
            NotifyChanged();
            return true;
        }

        public int GetToolLevel(FarmTool tool) => tool switch
        {
            FarmTool.Hoe => hoeLevel,
            FarmTool.WateringCan => wateringCanLevel,
            FarmTool.Harvest => harvestLevel,
            _ => 1
        };

        public static int GetLandTileCount(int level) => Mathf.Clamp(level, MinLandLevel, MaxLandLevel) switch
        {
            1 => 9,
            2 => 15,
            _ => 25
        };

        public int GetLandUpgradeCost()
        {
            if (landLevel >= MaxLandLevel) return 0;
            return FarmEconomyRules.LandUpgradeCost(landLevel);
        }

        public bool CanUpgradeLand()
        {
            var cost = GetLandUpgradeCost();
            return cost > 0 && money >= cost;
        }

        public bool TryUpgradeLand(out int newLevel, out int cost)
        {
            newLevel = landLevel;
            cost = GetLandUpgradeCost();
            if (cost <= 0 || money < cost) return false;
            money -= cost;
            landLevel = Mathf.Clamp(landLevel + 1, MinLandLevel, MaxLandLevel);
            newLevel = landLevel;
            NotifyChanged();
            return true;
        }

        public int GetToolUpgradeCost(FarmTool tool)
        {
            if (!IsUpgradeableTool(tool)) return 0;
            var level = GetToolLevel(tool);
            if (level >= MaxToolLevel) return 0;
            return FarmEconomyRules.ToolUpgradeCost(level);
        }

        public bool CanUpgradeTool(FarmTool tool)
        {
            var cost = GetToolUpgradeCost(tool);
            return cost > 0 && money >= cost && GetMasteryLevel(MasteryForTool(tool)) >= RequiredMasteryLevelForNextToolUpgrade(tool);
        }

        public bool TryUpgradeTool(FarmTool tool, out int newLevel, out int cost)
        {
            newLevel = GetToolLevel(tool);
            cost = GetToolUpgradeCost(tool);
            if (cost <= 0 || money < cost || GetMasteryLevel(MasteryForTool(tool)) < RequiredMasteryLevelForNextToolUpgrade(tool)) return false;
            money -= cost;
            SetToolLevelInternal(tool, newLevel + 1);
            newLevel = GetToolLevel(tool);
            RecordJournal(FarmJournalMetric.ToolUpgrades, 1, null, false);
            NotifyChanged();
            return true;
        }

        public static bool IsUpgradeableTool(FarmTool tool) =>
            tool is FarmTool.Hoe or FarmTool.WateringCan or FarmTool.Harvest;

        public static FarmMasterySkill MasteryForTool(FarmTool tool) =>
            tool is FarmTool.Hoe or FarmTool.WateringCan ? FarmMasterySkill.Cultivation : FarmMasterySkill.Harvesting;

        public int RequiredMasteryLevelForNextToolUpgrade(FarmTool tool) =>
            GetToolLevel(tool) >= 2 ? 2 : 1;

        private void SetToolLevelInternal(FarmTool tool, int value)
        {
            value = Mathf.Clamp(value, 1, MaxToolLevel);
            if (tool == FarmTool.Hoe) hoeLevel = value;
            else if (tool == FarmTool.WateringCan) wateringCanLevel = value;
            else if (tool == FarmTool.Harvest) harvestLevel = value;
        }

        public bool TrySellAll(CropDefinition crop, out int quantity, out int earned)
        {
            quantity = crop != null && crop.HarvestItem != null ? GetQuantity(crop.HarvestItem.Id) : 0;
            earned = crop != null && crop.HarvestItem != null
                ? SaleValueInList(inventory, crop.HarvestItem)
                : 0;
            if (quantity <= 0) return false;
            RemoveFromList(inventory, crop.HarvestItem.Id, quantity);
            money += earned;
            AddMasteryExperience(FarmMasterySkill.Commerce, quantity, false);
            NotifyChanged();
            return true;
        }

        public bool TrySellAllCrops(out int quantity, out int earned)
        {
            quantity = 0;
            earned = 0;
            var soldItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sellableItems = new List<ItemDefinition>();
            foreach (var stack in inventory)
            {
                var item = stack != null ? FarmContentDatabase.GetItem(stack.ItemId) : null;
                if (item == null || item.Category != ItemCategory.Crop || !soldItemIds.Add(item.Id)) continue;
                sellableItems.Add(item);
            }
            foreach (var item in sellableItems)
            {
                var cropQuantity = GetQuantity(item.Id);
                if (cropQuantity <= 0) continue;
                earned += SaleValueInList(inventory, item);
                RemoveFromList(inventory, item.Id, cropQuantity);
                quantity += cropQuantity;
            }
            if (quantity <= 0) return false;
            money += earned;
            RecordJournal(FarmJournalMetric.SoldUnits, quantity, null, false);
            AddMasteryExperience(FarmMasterySkill.Commerce, quantity, false);
            NotifyChanged();
            return true;
        }
        public bool TryBuySeedPack(CropDefinition crop, out int amount, out int cost)
        {
            amount = FarmEconomyRules.SeedPackAmount(crop) + (HasNeighborhoodUnlock("elara") ? 1 : 0);
            cost = FarmEconomyRules.SeedPackPrice(crop);
            if (crop == null || crop.SeedItem == null || amount <= 0 || money < cost || !CanAdd(crop.SeedItem.Id, amount)) return false;
            money -= cost;
            AddInternal(crop.SeedItem.Id, amount);
            RecordJournal(FarmJournalMetric.SeedPacksBought, 1, null, false);
            AddMasteryExperience(FarmMasterySkill.Commerce, 2, false);
            NotifyChanged();
            return true;
        }

        public bool TryConsumePumpkinSeed() => TryRemoveItem(PumpkinSeedId, 1);
        public void AddPumpkins(int amount) => AddItem(PumpkinId, amount);
        public bool TrySellAllPumpkins(out int quantity, out int earned) => TrySellAll(FarmContentDatabase.GetCrop(PumpkinId), out quantity, out earned);
        public bool TryBuyPumpkinSeedPack(out int amount, out int cost) => TryBuySeedPack(FarmContentDatabase.GetCrop(PumpkinId), out amount, out cost);

        public FarmSaveData CreateSaveData(List<FarmTileSaveData> tiles)
        {
            EnsureHotbar();
            var stacks = new List<InventoryStack>(inventory.Count);
            foreach (var stack in inventory) stacks.Add(new InventoryStack(stack.ItemId, stack.Quantity, stack.Quality));
            return new FarmSaveData
            {
                Version = 36,
                LandLevel = landLevel,
                Money = money,
                Inventory = stacks,
                Tiles = tiles ?? new List<FarmTileSaveData>(),
                Hotbar = new List<string>(hotbar),
                SelectedHotbarIndex = selectedHotbarIndex,
                Tutorial = (tutorial ?? new FarmTutorialProgress()).Clone(),
                Storage = CloneStacks(storage),
                CollectedPickupIds = new List<string>(collectedPickupIds),
                DayNumber = dayNumber,
                MinutesOfDay = minutesOfDay,
                WorldSeed = worldSeed,
                HoeLevel = hoeLevel,
                WateringCanLevel = wateringCanLevel,
                HarvestLevel = harvestLevel,
                Journal = (journal ?? new FarmJournalProgress()).Clone(),
                DailyOrders = DailyOrders.Clone(),
                Community = Community.Clone(),
                Festival = Festival.Clone(),
                CommunityProjects = CommunityProjects.Clone(),
                Animals = Animals.Clone(),
                Energy = energy,
                Mastery = (mastery ?? new FarmMasteryProgress()).Clone(),
                PlacedObjects = ClonePlacedObjects(placedObjects),
                ResourceNodes = CloneResourceNodes(resourceNodes),
                ProcessingJobs = CloneProcessingJobs(processingJobs),
                LastMorningAutomationDay = lastMorningAutomationDay,
                ReadMailIds = new List<string>(readMailIds ?? new List<string>()),
                ClaimedMailIds = new List<string>(claimedMailIds ?? new List<string>()),
                DiscoveredItemIds = new List<string>(discoveredItemIds ?? new List<string>()),
                DiscoveredRecipeIds = new List<string>(discoveredRecipeIds ?? new List<string>()),
                CollectionMilestoneMask = collectionMilestoneMask,
                CoopRoles = CoopRoles.Clone(),
                ForecastPlan = ForecastPlan.Clone(),
                HomesteadRest = HomesteadRest.Clone(),
                PumpkinSeeds = PumpkinSeeds,
                Pumpkins = Pumpkins
            };
        }

        public void Restore(FarmSaveData data)
        {
            if (data == null) return;
            discoveredItemIds = new List<string>();
            discoveredRecipeIds = new List<string>();
            money = Mathf.Max(0, data.Money);
            inventory.Clear();
            if (data.Version >= 2 && data.Inventory != null && data.Inventory.Count > 0)
            {
                foreach (var stack in data.Inventory)
                    if (stack != null && stack.Quantity > 0 && !string.IsNullOrWhiteSpace(stack.ItemId)) AddInternal(stack.ItemId, stack.Quantity, data.Version >= 18 ? stack.Quality : FarmItemQuality.Normal);
            }
            else
            {
                if (data.PumpkinSeeds > 0) AddInternal(PumpkinSeedId, data.PumpkinSeeds);
                if (data.Pumpkins > 0) AddInternal(PumpkinId, data.Pumpkins);
            }

            hotbar.Clear();
            if (data.Version >= 3 && data.Hotbar != null && data.Hotbar.Count > 0)
            {
                for (var index = 0; index < HotbarSlotCount; index++)
                {
                    var entry = index < data.Hotbar.Count ? data.Hotbar[index] : string.Empty;
                    hotbar.Add(IsValidHotbarEntry(entry) ? entry ?? string.Empty : string.Empty);
                }
                selectedHotbarIndex = Mathf.Clamp(data.SelectedHotbarIndex, 0, HotbarSlotCount - 1);
            }
            else ResetHotbarToDefaults();
            tutorial = data.Version >= 4 && data.Tutorial != null ? data.Tutorial.Clone() : new FarmTutorialProgress();
            storage.Clear();
            if (data.Version >= 5 && data.Storage != null)
                foreach (var stack in data.Storage)
                    if (stack != null && stack.Quantity > 0 && !string.IsNullOrWhiteSpace(stack.ItemId)) AddToStorageInternal(stack.ItemId, stack.Quantity, data.Version >= 18 ? stack.Quality : FarmItemQuality.Normal);
            collectedPickupIds.Clear();
            if (data.Version >= 6 && data.CollectedPickupIds != null)
                foreach (var pickupId in data.CollectedPickupIds)
                    if (!string.IsNullOrWhiteSpace(pickupId) && !IsPickupCollected(pickupId)) collectedPickupIds.Add(pickupId);
            if (data.Version >= 7) SetClock(data.DayNumber, data.MinutesOfDay);
            else SetClock(1, 480f);
            SetWorldSeed(data.Version >= 8 ? data.WorldSeed : DefaultWorldSeed);
            if (data.Version >= 9)
            {
                SetToolLevelInternal(FarmTool.Hoe, data.HoeLevel);
                SetToolLevelInternal(FarmTool.WateringCan, data.WateringCanLevel);
                SetToolLevelInternal(FarmTool.Harvest, data.HarvestLevel);
            }
            else
            {
                hoeLevel = 1;
                wateringCanLevel = 1;
                harvestLevel = 1;
            }
            journal = data.Version >= 10 && data.Journal != null ? data.Journal.Clone() : new FarmJournalProgress();
            dailyOrders = data.Version >= 11 && data.DailyOrders != null
                ? data.DailyOrders.Clone()
                : new FarmDailyOrderProgress { Day = dayNumber, CompletedMask = 0 };
            EnsureDailyOrdersForCurrentDay(false);
            community = data.Version >= 24 && data.Community != null ? data.Community.Clone() : new FarmCommunityProgress();
            festival = data.Version >= 26 && data.Festival != null ? data.Festival.Clone() : new FarmFestivalProgress();
            communityProjects = data.Version >= 27 && data.CommunityProjects != null ? data.CommunityProjects.Clone() : new FarmCommunityProjectProgress();
            animals = data.Version >= 28 && data.Animals != null ? data.Animals.Clone() : CreateStarterAnimalRecords();
            animals.EnsureNormalized();
            energy = data.Version >= 12 ? Mathf.Clamp(data.Energy, 0, MaxEnergy) : MaxEnergy;
            mastery = data.Version >= 13 && data.Mastery != null ? data.Mastery.Clone() : new FarmMasteryProgress();
            placedObjects.Clear();
            if (data.Version >= 14 && data.PlacedObjects != null)
                foreach (var placed in data.PlacedObjects)
                    if (IsValidPlacedObject(placed)) placedObjects.Add(placed.Clone());
            resourceNodes.Clear();
            if (data.Version >= 20 && data.ResourceNodes != null)
                foreach (var node in data.ResourceNodes)
                    if (node != null && !string.IsNullOrWhiteSpace(node.NodeId)) resourceNodes.Add(node.Clone());
            processingJobs.Clear();
            if (data.Version >= 21 && data.ProcessingJobs != null)
                foreach (var job in data.ProcessingJobs)
                    if (job != null && !string.IsNullOrWhiteSpace(job.JobId) && !string.IsNullOrWhiteSpace(job.OutputItemId) && job.OutputAmount > 0) processingJobs.Add(job.Clone());
            lastMorningAutomationDay = data.Version >= 15 ? Mathf.Max(0, data.LastMorningAutomationDay) : 0;
            readMailIds = new List<string>();
            claimedMailIds = new List<string>();
            if (data.Version >= 16)
            {
                AddUniqueMailIds(readMailIds, data.ReadMailIds);
                AddUniqueMailIds(claimedMailIds, data.ClaimedMailIds);
            }
            else
            {
                foreach (var mail in FarmMailDatabase.GetInbox(dayNumber))
                    if (mail != null && !string.IsNullOrWhiteSpace(mail.Id)) readMailIds.Add(mail.Id);
            }
            if (data.Version >= 17)
            {
                AddUniqueDiscoveredItemIds(data.DiscoveredItemIds);
            }
            else if (journal != null && journal.HarvestedCropIds != null)
            {
                foreach (var cropId in journal.HarvestedCropIds)
                {
                    var crop = FarmContentDatabase.GetCrop(cropId);
                    if (crop == null) continue;
                    if (crop.SeedItem != null) RecordDiscoveredItem(crop.SeedItem.Id, false);
                    if (crop.HarvestItem != null) RecordDiscoveredItem(crop.HarvestItem.Id, false);
                }
            }
            if (data.Version >= 30 && data.DiscoveredRecipeIds != null)
                foreach (var recipeId in data.DiscoveredRecipeIds) DiscoverRecipe(recipeId, false);
            collectionMilestoneMask = data.Version >= 33 ? Mathf.Max(0, data.CollectionMilestoneMask) : 0;
            coopRoles = data.Version >= 34 && data.CoopRoles != null ? data.CoopRoles.Clone() : new FarmCoopRoleProgress();
            coopRoles.EnsureNormalized();
            forecastPlan = data.Version >= 35 && data.ForecastPlan != null ? data.ForecastPlan.Clone() : new FarmForecastPlan();
            if (forecastPlan.TargetDay < dayNumber) forecastPlan = new FarmForecastPlan();
            homesteadRest = data.Version >= 36 && data.HomesteadRest != null ? data.HomesteadRest.Clone() : new FarmHomesteadRestProgress();
            if (homesteadRest.ComfortDay != dayNumber) homesteadRest.ComfortCharges = 0;
            DiscoverRecipesForKnownItems(false);
            landLevel = data.Version >= 19
                ? Mathf.Clamp(data.LandLevel, MinLandLevel, MaxLandLevel)
                : MinLandLevel;
            lastEnergyActionWasFree = false;
            NotifyChanged();
        }

        public bool IsMailRead(string mailId) => ContainsMailId(readMailIds, mailId);

        public bool IsMailClaimed(string mailId) => ContainsMailId(claimedMailIds, mailId);

        public bool MarkMailRead(string mailId)
        {
            if (string.IsNullOrWhiteSpace(mailId) || IsMailRead(mailId)) return false;
            if (FarmMailDatabase.Get(mailId, dayNumber) == null) return false;
            readMailIds ??= new List<string>();
            readMailIds.Add(mailId);
            NotifyChanged();
            return true;
        }

        public int CountUnreadMail()
        {
            var count = 0;
            foreach (var mail in FarmMailDatabase.GetInbox(dayNumber))
                if (mail != null && !IsMailRead(mail.Id)) count++;
            return count;
        }

        public int CountClaimableMail()
        {
            var count = 0;
            foreach (var mail in FarmMailDatabase.GetInbox(dayNumber))
                if (mail != null && mail.HasReward && !IsMailClaimed(mail.Id)) count++;
            return count;
        }

        public bool TryClaimMail(FarmMailDefinition mail, out string error)
        {
            error = string.Empty;
            if (mail == null || mail.DeliveredDay > dayNumber ||
                FarmMailDatabase.Get(mail.Id, dayNumber) == null)
            {
                error = FarmLocalization.Get("state.mail.unavailable", "Mail is unavailable.");
                return false;
            }
            if (IsMailClaimed(mail.Id))
            {
                error = FarmLocalization.Get("state.mail.claimed", "This attachment has already been claimed.");
                return false;
            }
            if (!mail.HasReward)
            {
                error = FarmLocalization.Get("state.mail.no_attachment", "This mail has no attachment.");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(mail.RewardItemId) && mail.RewardQuantity > 0)
            {
                if (FarmContentDatabase.GetItem(mail.RewardItemId) == null)
                {
                    error = FarmLocalization.Get("state.mail.item_unknown", "The attached item is not in the catalog.");
                    return false;
                }
                if (!CanAdd(mail.RewardItemId, mail.RewardQuantity))
                {
                    error = FarmLocalization.Get("state.mail.inventory_full", "Inventory full. Make room before claiming it.");
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(mail.RewardItemId) && mail.RewardQuantity > 0)
                AddInternal(mail.RewardItemId, mail.RewardQuantity);
            money += Mathf.Max(0, mail.RewardMoney);
            readMailIds ??= new List<string>();
            claimedMailIds ??= new List<string>();
            if (!IsMailRead(mail.Id)) readMailIds.Add(mail.Id);
            claimedMailIds.Add(mail.Id);
            NotifyChanged();
            return true;
        }

        private void AddUniqueDiscoveredItemIds(List<string> source)
        {
            if (source == null) return;
            foreach (var id in source) RecordDiscoveredItem(id, false);
        }

        private static FarmAnimalRecords CreateStarterAnimalRecords() => FarmAnimalRecords.CreateStarter();

        private static bool ContainsMailId(List<string> source, string mailId)
        {
            if (source == null || string.IsNullOrWhiteSpace(mailId)) return false;
            foreach (var id in source)
                if (string.Equals(id, mailId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void AddUniqueMailIds(List<string> target, List<string> source)
        {
            if (target == null || source == null) return;
            foreach (var id in source)
                if (!string.IsNullOrWhiteSpace(id) && !ContainsMailId(target, id)) target.Add(id);
        }

        public bool TryBeginMorningAutomation(int day)
        {
            day = Mathf.Max(1, day);
            if (lastMorningAutomationDay >= day) return false;
            lastMorningAutomationDay = day;
            NotifyChanged();
            return true;
        }

        public bool AddPlacedObject(FarmPlacedObjectSaveData data)
        {
            if (!IsValidPlacedObject(data)) return false;
            foreach (var placed in placedObjects)
                if (string.Equals(placed.PersistentId, data.PersistentId, StringComparison.OrdinalIgnoreCase))
                    return false;
            placedObjects.Add(data.Clone());
            NotifyChanged();
            return true;
        }

        public bool UpdatePlacedObject(FarmPlacedObjectSaveData data)
        {
            if (!IsValidPlacedObject(data)) return false;
            for (var index = 0; index < placedObjects.Count; index++)
            {
                var existing = placedObjects[index];
                if (!string.Equals(existing.PersistentId, data.PersistentId, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(existing.ItemId, data.ItemId, StringComparison.OrdinalIgnoreCase)) return false;
                placedObjects[index] = data.Clone();
                NotifyChanged();
                return true;
            }
            return false;
        }

        public bool RemovePlacedObject(string persistentId)
        {
            if (string.IsNullOrWhiteSpace(persistentId)) return false;
            for (var index = placedObjects.Count - 1; index >= 0; index--)
            {
                if (!string.Equals(placedObjects[index].PersistentId, persistentId, StringComparison.OrdinalIgnoreCase)) continue;
                placedObjects.RemoveAt(index);
                NotifyChanged();
                return true;
            }
            return false;
        }

        public bool CanReclaimPlacedObject(string persistentId)
        {
            if (string.IsNullOrWhiteSpace(persistentId)) return false;
            foreach (var placed in placedObjects)
            {
                if (!string.Equals(placed.PersistentId, persistentId, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(placed.ItemId, FarmShedKitId, StringComparison.OrdinalIgnoreCase)) return true;
                return StorageUsedSlots <= StorageSlotCapacity - FarmShedStorageSlotBonus;
            }
            return false;
        }

        private static bool IsValidPlacedObject(FarmPlacedObjectSaveData data) =>
            data != null &&
            !string.IsNullOrWhiteSpace(data.PersistentId) &&
            FarmBuildableDatabase.GetByItemId(data.ItemId) != null &&
            float.IsFinite(data.X) &&
            float.IsFinite(data.Y) &&
            float.IsFinite(data.Z) &&
            float.IsFinite(data.RotationY);

        public int GetResourceNodeHits(string nodeId, int maxHits)
        {
            var node = FindResourceNode(nodeId);
            if (node == null || node.RespawnDay > dayNumber) return 0;
            return Mathf.Clamp(node.Hits, 0, Mathf.Max(1, maxHits));
        }

        public bool IsResourceNodeDepleted(string nodeId)
        {
            var node = FindResourceNode(nodeId);
            return node != null && node.RespawnDay > dayNumber;
        }

        public bool IsResourceNodeStewarded(string nodeId)
        {
            var node = FindResourceNode(nodeId);
            // Keep this state visible during the resting day as well: the mining node
            // uses it both for its "already prepared" prompt and for its return yield.
            return node != null && node.Stewarded;
        }

        public bool TryStewardResourceNode(string nodeId, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(nodeId)) { error = FarmLocalization.Get("stewardship.invalid", "That gathering site cannot be restored."); return false; }
            var node = FindResourceNode(nodeId);
            if (node == null || node.RespawnDay <= dayNumber) { error = FarmLocalization.Get("stewardship.requires_depleted", "Only a resting outcrop can be renewed."); return false; }
            if (node.Stewarded) { error = FarmLocalization.Get("stewardship.already", "This outcrop is already prepared for its next return."); return false; }
            if (FarmWeatherSystem.WeatherForDay(worldSeed, dayNumber) != FarmWeather.Rain) { error = FarmLocalization.Get("stewardship.requires_rain", "Renewal works only while it is raining."); return false; }
            if (!TryRemoveItem(CompostId, 1)) { error = FarmLocalization.Get("stewardship.compost_missing", "You need Compost to renew this outcrop."); return false; }
            node.Stewarded = true;
            NotifyChanged();
            return true;
        }

        public bool TryHitResourceNode(string nodeId, int maxHits, int respawnDays, string yieldItemId, int yieldAmount, out int hitsRemaining, out bool depleted)
        {
            hitsRemaining = 0;
            depleted = false;
            if (string.IsNullOrWhiteSpace(nodeId) || maxHits < 1 || respawnDays < 1 || string.IsNullOrWhiteSpace(yieldItemId) || yieldAmount < 1) return false;
            var node = FindResourceNode(nodeId);
            if (node == null)
            {
                node = new FarmResourceNodeSaveData { NodeId = nodeId };
                resourceNodes.Add(node);
            }
            if (node.RespawnDay > dayNumber)
            {
                hitsRemaining = 0;
                return false;
            }
            if (node.RespawnDay > 0 && node.RespawnDay <= dayNumber)
            {
                node.Hits = 0;
                node.RespawnDay = 0;
            }
            var nextHits = Mathf.Clamp(node.Hits + 1, 1, maxHits);
            if (nextHits < maxHits)
            {
                node.Hits = nextHits;
                hitsRemaining = maxHits - nextHits;
                NotifyChanged();
                return true;
            }
            if (!CanAdd(yieldItemId, yieldAmount)) return false;
            node.Hits = maxHits;
            node.RespawnDay = dayNumber + respawnDays;
            node.Stewarded = false;
            AddInternal(yieldItemId, yieldAmount);
            hitsRemaining = 0;
            depleted = true;
            RecordJournal(FarmJournalMetric.WorldPickups, 1, null, false);
            AddMasteryExperience(FarmMasterySkill.Harvesting, 1, false);
            NotifyChanged();
            return true;
        }

        private FarmResourceNodeSaveData FindResourceNode(string nodeId)
        {
            foreach (var node in resourceNodes)
                if (node != null && string.Equals(node.NodeId, nodeId, StringComparison.OrdinalIgnoreCase)) return node;
            return null;
        }

        public bool TryStartProcessing(string recipeId, string inputItemId, int inputAmount, string outputItemId, int outputAmount, float durationMinutes, out string jobId) =>
            TryStartProcessing(recipeId, inputItemId, FarmItemQuality.Normal, inputAmount, outputItemId, FarmItemQuality.Normal, outputAmount, durationMinutes, out jobId);

        public bool TryStartProcessing(string recipeId, string inputItemId, FarmItemQuality inputQuality, int inputAmount, string outputItemId, FarmItemQuality outputQuality, int outputAmount, float durationMinutes, out string jobId)
        {
            jobId = string.Empty;
            inputQuality = FarmItemQualityRules.Clamp(inputQuality);
            outputQuality = FarmItemQualityRules.Clamp(outputQuality);
            if (string.IsNullOrWhiteSpace(recipeId) || string.IsNullOrWhiteSpace(inputItemId) || string.IsNullOrWhiteSpace(outputItemId) || inputAmount < 1 || outputAmount < 1 || durationMinutes <= 0f) return false;
            if (processingJobs.Count >= EffectiveProcessingQueueCapacity) return false;
            if (GetQuantity(inputItemId, inputQuality) < inputAmount || !TryRemoveItem(inputItemId, inputQuality, inputAmount)) return false;
            jobId = $"process:{recipeId}:{Guid.NewGuid():N}";
            var completion = (dayNumber * 1440f) + minutesOfDay;
            // A shared workbench processes one job at a time. Queued jobs begin after
            // the latest existing job, so skip-time and save/load cannot make outputs
            // appear in parallel or out of order.
            foreach (var job in processingJobs)
                if (job != null) completion = Mathf.Max(completion, job.CompletionGameMinutes);
            processingJobs.Add(new FarmProcessingJobSaveData { JobId = jobId, RecipeId = recipeId, OutputItemId = outputItemId, OutputAmount = outputAmount, OutputQuality = outputQuality, CompletionGameMinutes = completion + durationMinutes });
            NotifyChanged();
            return true;
        }

        public bool IsProcessingComplete(FarmProcessingJobSaveData job) => job != null && ((dayNumber * 1440f) + minutesOfDay) >= job.CompletionGameMinutes;

        public bool TryCollectProcessing(string jobId, out string outputItemId, out int outputAmount) =>
            TryCollectProcessing(jobId, out outputItemId, out outputAmount, out _);

        public bool TryCollectProcessing(string jobId, out string outputItemId, out int outputAmount, out FarmItemQuality outputQuality)
        {
            outputItemId = string.Empty; outputAmount = 0; outputQuality = FarmItemQuality.Normal;
            for (var index = 0; index < processingJobs.Count; index++)
            {
                var job = processingJobs[index];
                if (job == null || !string.Equals(job.JobId, jobId, StringComparison.OrdinalIgnoreCase)) continue;
                var quality = FarmItemQualityRules.Clamp(job.OutputQuality);
                if (!IsProcessingComplete(job) || !CanAdd(job.OutputItemId, job.OutputAmount, quality)) return false;
                outputItemId = job.OutputItemId; outputAmount = job.OutputAmount; outputQuality = quality;
                AddInternal(outputItemId, outputAmount, quality);
                processingJobs.RemoveAt(index);
                NotifyChanged();
                return true;
            }
            return false;
        }

        /// <summary>
        /// The placed Farm Shed turns completed workshop jobs into a morning routine.
        /// Inputs were already consumed when each job was explicitly queued; this only
        /// routes host-approved outputs to the shared storage when room exists.
        /// </summary>
        public int CollectCompletedProcessingToStorage()
        {
            var collected = 0;
            var changed = false;
            for (var index = 0; index < processingJobs.Count;)
            {
                var job = processingJobs[index];
                if (job == null)
                {
                    processingJobs.RemoveAt(index);
                    changed = true;
                    continue;
                }
                var quality = FarmItemQualityRules.Clamp(job.OutputQuality);
                if (!IsProcessingComplete(job) || !CanAddToStorage(job.OutputItemId, job.OutputAmount, quality))
                {
                    index++;
                    continue;
                }
                AddToStorageInternal(job.OutputItemId, job.OutputAmount, quality);
                collected += job.OutputAmount;
                processingJobs.RemoveAt(index);
                changed = true;
            }
            if (changed) NotifyChanged();
            return collected;
        }
        public bool TryCraft(CraftingRecipe recipe, out string error)
        {
            error = string.Empty;
            if (recipe == null || recipe.OutputItem == null || recipe.OutputQuantity <= 0 || recipe.Ingredients == null || recipe.Ingredients.Count == 0)
            {
                error = FarmLocalization.Get("state.crafting.invalid_recipe", "Invalid recipe.");
                return false;
            }
            if (recipe.RequiresDiscovery && !IsRecipeDiscovered(recipe.Id))
            {
                error = FarmLocalization.Get("state.crafting.discovery_locked", "Discover an ingredient to learn this recipe.");
                return false;
            }
            if (!recipe.IsUnlocked(this))
            {
                error = FarmLocalization.Format("state.crafting.mastery_locked", "Requires {0} Mastery level {1}.", FarmMasteryRules.DisplayName(recipe.RequiredMastery), recipe.RequiredMasteryLevel);
                return false;
            }
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient == null || ingredient.Item == null || ingredient.Quantity <= 0)
                {
                    error = FarmLocalization.Get("state.crafting.invalid_ingredient", "The recipe has an invalid ingredient.");
                    return false;
                }
                var owned = GetQuantity(ingredient.Item.Id);
                if (owned < ingredient.Quantity)
                {
                    error = FarmLocalization.Format("state.crafting.missing_ingredient", "Missing {0} {1}.", ingredient.Quantity - owned, ingredient.Item.LocalizedName.ToLowerInvariant());
                    return false;
                }
            }

            foreach (var ingredient in recipe.Ingredients)
                RemoveFromList(inventory, ingredient.Item.Id, ingredient.Quantity);
            if (!CanAdd(recipe.OutputItem.Id, recipe.OutputQuantity))
            {
                foreach (var ingredient in recipe.Ingredients)
                    AddInternal(ingredient.Item.Id, ingredient.Quantity);
                error = FarmLocalization.Get("state.crafting.inventory_full", "Inventory does not have room for the crafted item.");
                return false;
            }
            AddInternal(recipe.OutputItem.Id, recipe.OutputQuantity);
            NotifyChanged();
            return true;
        }

        public int GetMasteryExperience(FarmMasterySkill skill)
        {
            mastery ??= new FarmMasteryProgress();
            return mastery.GetExperience(skill);
        }

        public int GetMasteryLevel(FarmMasterySkill skill) =>
            FarmMasteryRules.LevelForExperience(GetMasteryExperience(skill));

        public bool CanAdvanceMasterySkill(FarmMasterySkill skill)
        {
            mastery ??= new FarmMasteryProgress();
            var specializationForSkill = FarmMasteryRules.SpecializationFor(skill);
            return specializationForSkill == mastery.Specialization ||
                GetMasteryExperience(skill) < FarmMasteryRules.LevelThreeExperience - 1;
        }

        public bool AddMasteryExperience(FarmMasterySkill skill, int amount, bool notify = true)
        {
            if (amount <= 0) return false;
            mastery ??= new FarmMasteryProgress();
            if (!CanAdvanceMasterySkill(skill)) return false;
            var previousLevel = GetMasteryLevel(skill);
            if (mastery.Specialization == FarmMasteryRules.SpecializationFor(skill)) amount = Mathf.CeilToInt(amount * 1.25f);
            if (mastery.Specialization != FarmMasteryRules.SpecializationFor(skill))
                amount = Mathf.Min(amount, (FarmMasteryRules.LevelThreeExperience - 1) - GetMasteryExperience(skill));
            if (amount <= 0) return false;
            mastery.AddExperience(skill, amount);
            if (notify) NotifyChanged();
            return GetMasteryLevel(skill) > previousLevel;
        }

        /// <summary>
        /// Commits this prototype profile to one level-three specialization.
        /// Authority is checked by the caller; the state method is deterministic
        /// for the future per-player backend profile command.
        /// </summary>
        public bool TrySetSpecialization(FarmSpecialization specialization, out string error)
        {
            error = string.Empty;
            mastery ??= new FarmMasteryProgress();
            if (!FarmMasteryRules.TryGetSkill(specialization, out var skill))
            {
                error = FarmLocalization.Get("mastery.focus.invalid", "Choose a valid mastery focus.");
                return false;
            }
            if (GetMasteryLevel(skill) < 2)
            {
                error = FarmLocalization.Format("mastery.focus.requires_level", "{0} focus requires {0} Mastery level 2.", FarmMasteryRules.DisplayName(skill));
                return false;
            }
            if (mastery.Specialization != FarmSpecialization.None && mastery.Specialization != specialization)
            {
                error = FarmLocalization.Get("mastery.specialization.committed", "Specialization is already committed for this profile.");
                return false;
            }
            mastery.Specialization = specialization;
            foreach (FarmMasterySkill otherSkill in Enum.GetValues(typeof(FarmMasterySkill)))
            {
                if (otherSkill == skill) continue;
                var cap = FarmMasteryRules.LevelThreeExperience - 1;
                if (mastery.GetExperience(otherSkill) > cap) mastery.SetExperience(otherSkill, cap);
            }
            NotifyChanged();
            return true;
        }

        public bool TryAddToStorage(string itemId, int amount) =>
            TryAddToStorage(itemId, amount, FarmItemQuality.Normal);

        public bool TryAddToStorage(string itemId, int amount, FarmItemQuality quality)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 || !CanAddToStorage(itemId, amount, quality)) return false;
            AddToStorageInternal(itemId, amount, quality);
            NotifyChanged();
            return true;
        }
        private void EnsureHotbar()
        {
            if (hotbar.Count == HotbarSlotCount) return;
            if (hotbar.Count == 0) { ResetHotbarToDefaults(); return; }
            while (hotbar.Count < HotbarSlotCount) hotbar.Add(string.Empty);
            if (hotbar.Count > HotbarSlotCount) hotbar.RemoveRange(HotbarSlotCount, hotbar.Count - HotbarSlotCount);
            selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, HotbarSlotCount - 1);
        }

        private void ResetHotbarToDefaults()
        {
            hotbar.Clear();
            hotbar.AddRange(DefaultHotbarEntries);
            selectedHotbarIndex = 0;
        }

        private static bool IsValidHotbarEntry(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry)) return true;
            if (entry.StartsWith(ItemPrefix, StringComparison.OrdinalIgnoreCase)) return FarmContentDatabase.GetItem(entry[ItemPrefix.Length..]) != null;
            if (!entry.StartsWith(ToolPrefix, StringComparison.OrdinalIgnoreCase)) return false;
            return IsCoreToolEntry(entry);
        }

        private void AddInternal(string itemId, int amount, FarmItemQuality quality = FarmItemQuality.Normal)
        {
            AddToList(inventory, slotCapacity, itemId, amount, quality);
        }

        private int CountPlacedItem(string itemId)
        {
            var count = 0;
            foreach (var placed in placedObjects)
                if (placed != null && string.Equals(placed.ItemId, itemId, StringComparison.OrdinalIgnoreCase)) count++;
            return count;
        }

        public bool HasPlacedItem(string itemId) =>
            !string.IsNullOrWhiteSpace(itemId) && CountPlacedItem(itemId) > 0;

        private bool CanAddToStorage(string itemId, int amount, FarmItemQuality quality = FarmItemQuality.Normal) =>
            CanAddToList(storage, StorageSlotCapacity, itemId, amount, quality);

        private void AddToStorageInternal(string itemId, int amount, FarmItemQuality quality = FarmItemQuality.Normal)
        {
            AddToList(storage, StorageSlotCapacity, itemId, amount, quality);
        }

        private void AddToList(List<InventoryStack> stacks, int capacity, string itemId, int amount, FarmItemQuality quality)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            var item = FarmContentDatabase.GetItem(itemId);
            if (item != null && amount > 0) RecordDiscoveredItem(itemId, false);
            var maxStack = item != null ? Mathf.Max(1, item.MaxStack) : 99;
            foreach (var stack in stacks)
            {
                if (!string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase) ||
                    FarmItemQualityRules.Clamp(stack.Quality) != quality || stack.Quantity >= maxStack) continue;
                var moved = Mathf.Min(maxStack - stack.Quantity, amount);
                stack.Quantity += moved;
                amount -= moved;
                if (amount <= 0) return;
            }
            while (amount > 0 && stacks.Count < capacity)
            {
                var moved = Mathf.Min(maxStack, amount);
                stacks.Add(new InventoryStack(itemId, moved, quality));
                amount -= moved;
            }
        }

        private static bool CanAddToList(List<InventoryStack> stacks, int capacity, string itemId, int amount, FarmItemQuality quality)
        {
            if (amount <= 0) return true;
            if (string.IsNullOrWhiteSpace(itemId)) return false;
            quality = FarmItemQualityRules.Clamp(quality);
            var item = FarmContentDatabase.GetItem(itemId);
            var maxStack = item != null ? Mathf.Max(1, item.MaxStack) : 99;
            var free = Mathf.Max(0, capacity - stacks.Count) * maxStack;
            foreach (var stack in stacks)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase) &&
                    FarmItemQualityRules.Clamp(stack.Quality) == quality)
                    free += Mathf.Max(0, maxStack - stack.Quantity);
            return free >= amount;
        }

        private static int GetQuantityInList(List<InventoryStack> stacks, string itemId, FarmItemQuality quality)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            var total = 0;
            foreach (var stack in stacks)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase) &&
                    FarmItemQualityRules.Clamp(stack.Quality) == quality)
                    total += stack.Quantity;
            return total;
        }

        private static void RemoveFromList(List<InventoryStack> stacks, string itemId, int amount)
        {
            for (var quality = FarmItemQuality.Normal; quality <= FarmItemQuality.Gold && amount > 0; quality++)
            {
                var available = GetQuantityInList(stacks, itemId, quality);
                var remove = Mathf.Min(available, amount);
                RemoveFromListExact(stacks, itemId, quality, remove);
                amount -= remove;
            }
        }

        private static void RemoveFromListExact(List<InventoryStack> stacks, string itemId, FarmItemQuality quality, int amount)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            for (var index = stacks.Count - 1; index >= 0 && amount > 0; index--)
            {
                var stack = stacks[index];
                if (!string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase) ||
                    FarmItemQualityRules.Clamp(stack.Quality) != quality) continue;
                var removed = Mathf.Min(stack.Quantity, amount);
                stack.Quantity -= removed;
                amount -= removed;
                if (stack.Quantity <= 0) stacks.RemoveAt(index);
            }
        }

        private int SaleValueInList(List<InventoryStack> stacks, ItemDefinition definition)
        {
            var total = 0;
            foreach (var stack in stacks)
                if (string.Equals(stack.ItemId, definition.Id, StringComparison.OrdinalIgnoreCase))
                    total += stack.Quantity * FarmMarketRules.UnitPrice(
                        definition, stack.Quality, worldSeed, dayNumber);
            return total;
        }
        private static List<FarmPlacedObjectSaveData> ClonePlacedObjects(List<FarmPlacedObjectSaveData> source)
        {
            var result = new List<FarmPlacedObjectSaveData>(source.Count);
            foreach (var placed in source)
                if (IsValidPlacedObject(placed)) result.Add(placed.Clone());
            return result;
        }
        private static List<FarmResourceNodeSaveData> CloneResourceNodes(List<FarmResourceNodeSaveData> source)
        {
            var result = new List<FarmResourceNodeSaveData>(source?.Count ?? 0);
            if (source == null) return result;
            foreach (var node in source)
                if (node != null && !string.IsNullOrWhiteSpace(node.NodeId)) result.Add(node.Clone());
            return result;
        }
        private static List<FarmProcessingJobSaveData> CloneProcessingJobs(List<FarmProcessingJobSaveData> source)
        {
            var result = new List<FarmProcessingJobSaveData>(source?.Count ?? 0);
            if (source == null) return result;
            foreach (var job in source) if (job != null) result.Add(job.Clone());
            return result;
        }
        private static List<InventoryStack> CloneStacks(List<InventoryStack> source)
        {
            var result = new List<InventoryStack>(source.Count);
            foreach (var stack in source) result.Add(new InventoryStack(stack.ItemId, stack.Quantity, stack.Quality));
            return result;
        }

        private void NotifyChanged()
        {
            RemoveUnavailableItemHotbarEntries();
            Changed?.Invoke();
        }

        private void RemoveUnavailableItemHotbarEntries()
        {
            EnsureHotbar();
            for (var index = 0; index < hotbar.Count; index++)
            {
                var entry = hotbar[index];
                if (string.IsNullOrWhiteSpace(entry) ||
                    !entry.StartsWith(ItemPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var itemId = entry[ItemPrefix.Length..];
                if (GetQuantity(itemId) <= 0) hotbar[index] = string.Empty;
            }
        }
    }

    public static class FarmSaveSystem
    {
        private const string SaveFileName = "farm-prototype-save.json";
        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static string BackupPath => SavePath + ".bak";
        public static bool LastLoadUsedBackup { get; private set; }

        public static bool Save(FarmSaveData data, out string error) =>
            SaveToPath(data, SavePath, BackupPath, out error);

        public static bool SaveToPath(FarmSaveData data, string path, string backupPath, out string error)
        {
            error = null;
            var tempPath = string.IsNullOrWhiteSpace(path) ? null : path + ".tmp";
            try
            {
                if (data == null) throw new ArgumentNullException(nameof(data));
                if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("O caminho do save est\u00E1 vazio.", nameof(path));
                if (string.IsNullOrWhiteSpace(backupPath)) backupPath = path + ".bak";
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

                var json = JsonUtility.ToJson(data, true);
                if (string.IsNullOrWhiteSpace(json) || JsonUtility.FromJson<FarmSaveData>(json) == null)
                    throw new InvalidDataException("Os dados serializados do save s\u00E3o inv\u00E1lidos.");

                var bytes = new System.Text.UTF8Encoding(false).GetBytes(json);
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(tempPath, path, backupPath, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        ReplaceWithPortableFallback(tempPath, path, backupPath);
                    }
                    catch (IOException)
                    {
                        ReplaceWithPortableFallback(tempPath, path, backupPath);
                    }
                }
                else
                {
                    File.Move(tempPath, path);
                }
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                TryDelete(tempPath);
                return false;
            }
        }

        public static bool TryLoad(out FarmSaveData data, out string error) =>
            TryLoadFromPaths(SavePath, BackupPath, out data, out error);

        public static bool TryLoadFromPaths(string path, string backupPath, out FarmSaveData data, out string error)
        {
            LastLoadUsedBackup = false;
            error = null;
            if (TryRead(path, out data, out var primaryError)) return true;
            if (TryRead(backupPath, out data, out var backupError))
            {
                LastLoadUsedBackup = true;
                error = null;
                return true;
            }

            data = null;
            if (string.IsNullOrEmpty(primaryError) && string.IsNullOrEmpty(backupError))
            {
                error = null;
                return false;
            }
            error = string.IsNullOrEmpty(backupError)
                ? primaryError
                : $"Save principal: {primaryError} Backup: {backupError}";
            return false;
        }

        private static bool TryRead(string path, out FarmSaveData data, out string error)
        {
            data = null;
            error = null;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    throw new InvalidDataException("O arquivo est\u00E1 vazio.");
                data = JsonUtility.FromJson<FarmSaveData>(json);
                if (data == null) throw new InvalidDataException("O arquivo \u00E9 inv\u00E1lido.");
                return true;
            }
            catch (Exception exception)
            {
                data = null;
                error = exception.Message;
                return false;
            }
        }

        private static void ReplaceWithPortableFallback(string tempPath, string path, string backupPath)
        {
            File.Copy(path, backupPath, true);
            File.Delete(path);
            File.Move(tempPath, path);
        }

        private static void TryDelete(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Uma sobra temporaria nao deve esconder o erro original de salvamento.
            }
        }
    }
}
