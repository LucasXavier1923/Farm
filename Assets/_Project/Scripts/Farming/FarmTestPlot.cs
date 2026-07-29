using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace FarmPrototype.Farming
{
    public enum FarmTool { None, Hoe, Seeds, WateringCan, Harvest, Axe, Pickaxe, Fertilizer }

    public sealed class FarmTestPlot : MonoBehaviour
    {
        [SerializeField] private string playerName = "Player";
        [SerializeField] private string initialCropId = "pumpkin";
        [SerializeField] private int gridSize = 3;
        [SerializeField] private float tileSize = 2f;
        [SerializeField] private float plotDistanceFromPlayer = 7f;
        [SerializeField] private float interactionDistance = 7f;
        // Host-led co-op stores session state locally; no mock or remote backend is used.
        [SerializeField] private bool useAuthoritativeCore = false;
        private const float SellStationInteractionDistance = 2.2f;
        private const float StorageInteractionDistance = 2.2f;
        private const float SleepInteractionDistance = 2.5f;
        private const float OrderBoardInteractionDistance = 2.2f;
        private const float SnapshotPublishInterval = 0.10f;

        private readonly List<FarmTestTile> tiles = new();
        private readonly List<CropDefinition> shopCrops = new();
        private readonly List<FarmDailyOrder> dailyOrders = new();
        private int shopCropIndex;
        private int cachedOrdersDay = -1;
        private int cachedOrdersSeed;
        private Transform player;
        private FarmGameState gameState;
        private FarmAuthoritativeCore authoritativeCore;
        private FarmSessionCommerce sessionCommerce;
        private bool authoritativeActionPending;
        private CropDefinition activeCrop;
        private FarmSellStation sellStation;
        private FarmStorageStation storageStation;
        private FarmSleepStation sleepStation;
        private FarmSleepSession sleepSession;
        private bool advancingDay;
        private FarmOrderBoardStation orderBoardStation;
        private FarmHudController hud;
        private FarmDayClock dayClock;
        private FarmWeatherSystem weatherSystem;
        private FarmMealSystem mealSystem;
        private FarmFestivalSystem festivalSystem;
        private FarmCommunityProjectSystem communityProjectSystem;
        private FarmActionFeedback actionFeedback;
        private FarmCraftingSystem craftingSystem;
        private FarmProcessorSystem processorSystem;
        private FarmMiningSystem miningSystem;
        private FarmFishingSystem fishingSystem;
        private FarmAnimalSystem animalSystem;
        private FarmBuildingSystem buildingSystem;
        private FarmMailboxSystem mailboxSystem;
        private FarmCollectionBook collectionBook;
        private FarmNightLanterns nightLanterns;
        private FarmTestTile hoveredTile;
        private FarmSellStation hoveredStation;
        private FarmStorageStation hoveredStorage;
        private FarmSleepStation hoveredSleep;
        private FarmOrderBoardStation hoveredOrderBoard;
        private Transform pickupRoot;
        private Transform tileRoot;
        private Vector3 plotCenter;
        private Vector3 plotForward;
        private float plotGridOffset;
        private FarmTool activeTool = FarmTool.Hoe;
        private string selectedItemId;
        private string feedback;
        private readonly Dictionary<object, string> externalPrompts = new();
        private string saveStatus;
        private bool saveQueued;
        private bool worldSnapshotQueued;
        private int lastMorningPestAffected;
        private int lastMorningPestProtected;
        private int lastMorningProcessedStored;
        private float saveAt;
        private float nextWorldSnapshotPublishAt;
        private int nextWorldSnapshotRevision;
        private int lastAppliedWorldSnapshotRevision;

        public FarmGameState GameState => gameState;
        public bool UsesAuthoritativeCore => useAuthoritativeCore;
        public FarmSessionCommerce SessionCommerce => sessionCommerce;
        public FarmDayClock DayClock => dayClock;
        public FarmWeatherSystem WeatherSystem => weatherSystem;
        public FarmMealSystem MealSystem => mealSystem;
        public FarmSleepSession SleepSession => sleepSession;
        public string SleepSessionStatus => sleepSession != null ? sleepSession.StatusText : FarmLocalization.Get("sleep.unavailable", "Sleep session unavailable.");
        public FarmActionFeedback ActionFeedback => actionFeedback;
        public FarmCraftingSystem CraftingSystem => craftingSystem;
        public FarmProcessorSystem ProcessorSystem => processorSystem;
        public FarmMiningSystem MiningSystem => miningSystem;
        public FarmFishingSystem FishingSystem => fishingSystem;
        public FarmAnimalSystem AnimalSystem => animalSystem;
        public FarmBuildingSystem BuildingSystem => buildingSystem;
        public FarmMailboxSystem MailboxSystem => mailboxSystem;
        public FarmCollectionBook CollectionBook => collectionBook;
        public FarmNightLanterns NightLanterns => nightLanterns;
        public CropDefinition ActiveCrop => activeCrop;
        public CropDefinition ShopCrop => shopCrops.Count > 0 ? shopCrops[Mathf.Clamp(shopCropIndex, 0, shopCrops.Count - 1)] : activeCrop;
        public int ShopCropCount => shopCrops.Count;
        public int ShopCropIndex => shopCropIndex;
        public FarmTool ActiveTool => activeTool;
        public string SelectedItemId => selectedItemId;
        public int ActiveToolLevel => gameState != null ? gameState.GetToolLevel(activeTool) : 1;
        public int EffectiveToolLevel => gameState != null && gameState.IsExhausted && FarmGameState.IsUpgradeableTool(activeTool) ? 1 : ActiveToolLevel;
        public string ActiveToolAreaText => ToolAreaText(activeTool, EffectiveToolLevel);
        public string ActiveToolDisplayName => FarmGameState.IsUpgradeableTool(activeTool)
            ? $"{ToolName(activeTool)} L{ActiveToolLevel} \u2022 {ActiveToolAreaText}{(gameState != null && gameState.IsExhausted ? FarmLocalization.Get("tool.tired_suffix", " (tired)") : string.Empty)}"
            : ToolName(activeTool);
        public int ActiveToolUpgradeCost => gameState != null ? gameState.GetToolUpgradeCost(activeTool) : 0;
        public int LandLevel => gameState != null ? gameState.LandLevel : FarmGameState.MinLandLevel;
        public int LandTileCount => tiles.Count;
        public int LandUpgradeCost => gameState != null ? gameState.GetLandUpgradeCost() : 0;
        public bool CanUpgradeLand => gameState != null && ShopInRange && gameState.CanUpgradeLand();
        public bool CanUpgradeActiveTool => gameState != null && ShopInRange && gameState.CanUpgradeTool(activeTool);
        public string Feedback => feedback;
        public string SaveStatus => saveStatus;
        public int LastMorningPestAffected => lastMorningPestAffected;
        public int LastMorningPestProtected => lastMorningPestProtected;
        public int LastAppliedWorldSnapshotRevision => lastAppliedWorldSnapshotRevision;
        /// <summary>
        /// Raised on the authority after a shared-state change has settled for the frame.
        /// A future Steam adapter subscribes here and owns serialization and transport.
        /// </summary>
        public event Action<FarmWorldSessionSnapshot> WorldSnapshotReady;
        public string PestForecastText => FarmPestRules.ForecastText(gameState != null ? gameState.DayNumber : 1);
        public bool PestThreatToday => gameState != null && FarmPestRules.IsVisitDay(gameState.DayNumber);
        public Vector3 PlotCenter => plotCenter;
        public Vector3 PlotForward => plotForward;
        public Transform PlayerTransform => player;
        public string PlayerName => playerName;
        public bool IsGreenhouseClimateAt(Vector3 worldPosition) =>
            gameState != null && gameState.IsCoveredByBuildableFunction(worldPosition, FarmBuildableFunction.Greenhouse);
        public FarmSeason EffectiveCropSeason(CropDefinition crop, Vector3 worldPosition)
        {
            if (crop != null && IsGreenhouseClimateAt(worldPosition)) return crop.PreferredSeason;
            return dayClock != null ? dayClock.CurrentSeason : FarmSeason.Spring;
        }
        public string CurrentPrompt => ResolveExternalPrompt() ?? HoverPrompt();
        public bool ShopVisible => hud != null && hud.IsShopOpen;
        public bool ShopInRange => sellStation != null && IsStationInRange();
        public bool StorageVisible => storageStation != null && IsStorageInRange();
        public bool SleepVisible => sleepStation != null && IsSleepInRange();
        public bool SleepInRange => SleepVisible;
        public bool OrderBoardVisible => orderBoardStation != null && IsOrderBoardInRange();
        public bool MailboxVisible => mailboxSystem != null && mailboxSystem.IsInRange;
        public IReadOnlyList<FarmDailyOrder> DailyOrders
        {
            get
            {
                RefreshDailyOrdersCache();
                return dailyOrders;
            }
        }

        private void Start()
        {
            // Existing prototype scenes serialized this flag as true. Force the new
            // host-led local model even when opening an older scene revision.
            useAuthoritativeCore = false;
            FarmEconomyRules.Reload();
            feedback = FarmLocalization.Get("feedback.core_loop", "Prepare the soil, plant, water, and harvest.");
            saveStatus = FarmLocalization.Get("save.not_created", "Save not created yet.");
            activeCrop = FarmContentDatabase.GetCrop(initialCropId);
            if (activeCrop == null)
            {
                Debug.LogError($"Crop '{initialCropId}' was not found in Resources/GameData/Crops.");
                enabled = false;
                return;
            }

            RefreshShopCatalog();
            var playerObject = GameObject.Find(playerName);
            player = playerObject != null ? playerObject.transform : null;
            gameState = GetComponent<FarmGameState>();
            if (gameState == null) gameState = gameObject.AddComponent<FarmGameState>();
            gameState.Changed += HandleGameStateChanged;
            sessionCommerce = GetComponent<FarmSessionCommerce>();
            if (sessionCommerce == null) sessionCommerce = gameObject.AddComponent<FarmSessionCommerce>();
            sessionCommerce.Initialize(gameState);
            if (GetComponent<FarmSessionCoordinator>() == null) gameObject.AddComponent<FarmSessionCoordinator>();
            if (GetComponent<FarmSteamSession>() == null) gameObject.AddComponent<FarmSteamSession>();
            if (GetComponent<FarmSteamP2PTransport>() == null) gameObject.AddComponent<FarmSteamP2PTransport>();
            CreatePlotAndStation();
            if (!useAuthoritativeCore) LoadGame(false);
            if (useAuthoritativeCore)
            {
                authoritativeCore = GetComponent<FarmAuthoritativeCore>();
                if (authoritativeCore == null) authoritativeCore = gameObject.AddComponent<FarmAuthoritativeCore>();
                var tileIndexes = new List<int>(tiles.Count);
                foreach (var tile in tiles) tileIndexes.Add(tile.Index);
                authoritativeCore.Initialize(
                    gameState,
                    tileIndexes,
                    () => dayClock != null ? dayClock.CurrentSeason : FarmSeason.Spring,
                    index => index >= 0 && index < tiles.Count && IsGreenhouseClimateAt(tiles[index].transform.position));
                feedback = FarmLocalization.Get("feedback.backend.connected", "Test backend connected. Soil, planting, watering, and harvesting await confirmation.");
            }
            ApplySelectedHotbarEntry(false);

            hud = GetComponent<FarmHudController>();
            if (hud == null) hud = gameObject.AddComponent<FarmHudController>();
            hud.Initialize(this);
            var developerPanel = GetComponent<FarmDeveloperPanel>();
            if (developerPanel == null) developerPanel = gameObject.AddComponent<FarmDeveloperPanel>();
            developerPanel.Initialize(this);
            collectionBook = GetComponent<FarmCollectionBook>();
            if (collectionBook == null) collectionBook = gameObject.AddComponent<FarmCollectionBook>();
            collectionBook.Initialize(gameState, hud);
            var seasonalPlanner = GetComponent<FarmSeasonalPlanner>();
            if (seasonalPlanner == null) seasonalPlanner = gameObject.AddComponent<FarmSeasonalPlanner>();
            seasonalPlanner.Initialize(this, hud);
            var settingsMenu = GetComponent<FarmSettingsMenu>();
            if (settingsMenu == null) settingsMenu = gameObject.AddComponent<FarmSettingsMenu>();
            settingsMenu.Initialize(hud);
            var masteryMenu = GetComponent<FarmMasteryMenu>();
            if (masteryMenu == null) masteryMenu = gameObject.AddComponent<FarmMasteryMenu>();
            masteryMenu.Initialize(hud, gameState);
            var economyLedger = GetComponent<FarmEconomyLedgerMenu>();
            if (economyLedger == null) economyLedger = gameObject.AddComponent<FarmEconomyLedgerMenu>();
            economyLedger.Initialize(hud, gameState);
            craftingSystem = GetComponent<FarmCraftingSystem>();
            if (craftingSystem == null) craftingSystem = gameObject.AddComponent<FarmCraftingSystem>();
            craftingSystem.Initialize(this, gameState, hud, player);
            processorSystem = GetComponent<FarmProcessorSystem>();
            if (processorSystem == null) processorSystem = gameObject.AddComponent<FarmProcessorSystem>();
            processorSystem.Initialize(this, gameState, hud, player);
            miningSystem = GetComponent<FarmMiningSystem>();
            if (miningSystem == null) miningSystem = gameObject.AddComponent<FarmMiningSystem>();
            miningSystem.Initialize(this, gameState, hud, player);
            fishingSystem = GetComponent<FarmFishingSystem>();
            if (fishingSystem == null) fishingSystem = gameObject.AddComponent<FarmFishingSystem>();
            fishingSystem.Initialize(this, gameState, hud, player);
            animalSystem = GetComponent<FarmAnimalSystem>();
            if (animalSystem == null) animalSystem = gameObject.AddComponent<FarmAnimalSystem>();
            animalSystem.Initialize(this, gameState, hud, player);
            buildingSystem = GetComponent<FarmBuildingSystem>();
            if (buildingSystem == null) buildingSystem = gameObject.AddComponent<FarmBuildingSystem>();
            buildingSystem.Initialize(this, gameState, hud, player);
            dayClock = GetComponent<FarmDayClock>();
            if (dayClock == null) dayClock = gameObject.AddComponent<FarmDayClock>();
            dayClock.Initialize(this, gameState);
            sleepSession = GetComponent<FarmSleepSession>();
            if (sleepSession == null) sleepSession = gameObject.AddComponent<FarmSleepSession>();
            sleepSession.Initialize(playerName);
            sleepSession.Changed += HandleSleepReadinessChanged;
            mailboxSystem = GetComponent<FarmMailboxSystem>();
            if (mailboxSystem == null) mailboxSystem = gameObject.AddComponent<FarmMailboxSystem>();
            var mailboxRight = Vector3.Cross(Vector3.up, plotForward).normalized;
            var mailboxPosition = PlaceOnGround(
                plotCenter - (plotForward * (plotGridOffset + 4.3f)) - (mailboxRight * 1.8f));
            mailboxSystem.Initialize(this, gameState, hud, player, mailboxPosition, plotForward);
            weatherSystem = GetComponent<FarmWeatherSystem>();
            if (weatherSystem == null) weatherSystem = gameObject.AddComponent<FarmWeatherSystem>();
            weatherSystem.Initialize(this, gameState, dayClock, player);
            mealSystem = GetComponent<FarmMealSystem>();
            if (mealSystem == null) mealSystem = gameObject.AddComponent<FarmMealSystem>();
            mealSystem.Initialize(gameState, hud, playerName);
            festivalSystem = GetComponent<FarmFestivalSystem>();
            if (festivalSystem == null) festivalSystem = gameObject.AddComponent<FarmFestivalSystem>();
            festivalSystem.Initialize(this, gameState, hud);
            communityProjectSystem = GetComponent<FarmCommunityProjectSystem>();
            if (communityProjectSystem == null) communityProjectSystem = gameObject.AddComponent<FarmCommunityProjectSystem>();
            communityProjectSystem.Initialize(this, gameState, hud);
            actionFeedback = GetComponent<FarmActionFeedback>();
            if (actionFeedback == null) actionFeedback = gameObject.AddComponent<FarmActionFeedback>();
            actionFeedback.Initialize();
            RebuildWorldPickups();
            nightLanterns = GetComponent<FarmNightLanterns>();
            if (nightLanterns == null) nightLanterns = gameObject.AddComponent<FarmNightLanterns>();
            nightLanterns.Initialize(this);
            var dailyRhythm = GetComponent<FarmDailyRhythm>();
            if (dailyRhythm == null) dailyRhythm = gameObject.AddComponent<FarmDailyRhythm>();
            dailyRhythm.Initialize(this, gameState, dayClock);
        }

        private void OnDestroy()
        {
            if (gameState != null) gameState.Changed -= HandleGameStateChanged;
            if (sleepSession != null) sleepSession.Changed -= HandleSleepReadinessChanged;
        }

        private void HandleGameStateChanged()
        {
            ApplySelectedHotbarEntry(false);
            if (!useAuthoritativeCore) QueueSave();
            QueueWorldSnapshot();
        }

        private void LateUpdate()
        {
            var listeners = WorldSnapshotReady;
            if (!worldSnapshotQueued || !FarmSessionTime.IsSimulationAuthority || listeners == null) return;
            if (Time.unscaledTime < nextWorldSnapshotPublishAt) return;
            worldSnapshotQueued = false;
            var snapshot = CaptureWorldSessionSnapshot();
            if (snapshot == null) return;
            nextWorldSnapshotPublishAt = Time.unscaledTime + SnapshotPublishInterval;
            foreach (Action<FarmWorldSessionSnapshot> listener in listeners.GetInvocationList())
            {
                try { listener.Invoke(snapshot); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
        private void OnApplicationQuit()
        {
            if (FarmSessionTime.IsSimulationAuthority && !useAuthoritativeCore && gameState != null && tiles.Count > 0) SaveGame(false);
        }

        private void Update()
        {
            if (FarmHudController.IsModalOpen)
            {
                if (saveQueued && Time.unscaledTime >= saveAt) SaveGame(false);
                return;
            }

            UpdateHotbarSelection();
            if (buildingSystem != null && buildingSystem.IsPlacing)
            {
                if (saveQueued && Time.unscaledTime >= saveAt) SaveGame(false);
                return;
            }
            UpdateHoveredTarget();

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f5Key.wasPressedThisFrame) SaveGame(true);
                if (keyboard.f9Key.wasPressedThisFrame) LoadGame(true);
                if (keyboard.fKey.wasPressedThisFrame)
                {
                    if (MailboxVisible) RequestMailbox();
                    else if (SleepVisible) RequestSleep();
                    else if (OrderBoardVisible) RequestOrderBoard();
                    else if (StorageVisible) RequestStorage();
                    else if (ShopInRange) RequestShop();
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) TryPrimaryInteraction();
            if (saveQueued && Time.unscaledTime >= saveAt) SaveGame(false);
        }

        public void RequestMailbox()
        {
            if (mailboxSystem == null || !mailboxSystem.IsInRange)
            {
                feedback = FarmLocalization.Get("interaction.mailbox.too_far", "Move closer to the mailbox.");
                return;
            }
            if (mailboxSystem.Open()) feedback = FarmLocalization.Get("interaction.mailbox.opened", "Mailbox opened.");
        }

        public void RequestShop()
        {
            if (!IsStationInRange())
            {
                feedback = FarmLocalization.Get("interaction.market.too_far", "Move closer to the market crate.");
                return;
            }
            feedback = FarmLocalization.Get("interaction.market.opened", "Market opened.");
            hud?.SetShopOpen(true);
        }

        public void RequestSell()
        {
            InteractWithStation(false);
        }

        public void RequestBuySeeds()
        {
            InteractWithStation(true);
        }

        public void RequestUpgradeActiveTool()
        {
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                feedback = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
                return;
            }
            if (!IsStationInRange())
            {
                feedback = FarmLocalization.Get("interaction.market.too_far", "Move closer to the market crate.");
                return;
            }
            if (!FarmGameState.IsUpgradeableTool(activeTool))
            {
                feedback = FarmLocalization.Get("upgrade.select_tool", "Select the hoe, watering can, or harvest tool.");
                return;
            }
            var currentLevel = gameState.GetToolLevel(activeTool);
            if (currentLevel >= FarmGameState.MaxToolLevel)
            {
                feedback = FarmLocalization.Format("upgrade.maxed", "{0} is already at the maximum level.", ToolName(activeTool));
                return;
            }
            var masterySkill = FarmGameState.MasteryForTool(activeTool);
            var requiredMasteryLevel = gameState.RequiredMasteryLevelForNextToolUpgrade(activeTool);
            if (gameState.GetMasteryLevel(masterySkill) < requiredMasteryLevel)
            {
                feedback = FarmLocalization.Format("upgrade.mastery_required", "Requires {0} Mastery level {1}.", FarmMasteryRules.DisplayName(masterySkill), requiredMasteryLevel);
                return;
            }
            var required = gameState.GetToolUpgradeCost(activeTool);
            if (gameState.TryUpgradeTool(activeTool, out var newLevel, out var cost))
            {
                feedback = FarmLocalization.Format("upgrade.success", "{0} upgraded to L{1}: {2} for ${3}.", ToolName(activeTool), newLevel, ToolAreaText(activeTool, newLevel), cost);
                actionFeedback?.PlayReward(sellStation.transform.position, newLevel);
                RefreshHoveredArea();
            }
            else feedback = FarmLocalization.Format("upgrade.insufficient_funds", "Not enough money. The upgrade costs ${0}.", required);
        }

        public void RequestUpgradeLand()
        {
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                feedback = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
                return;
            }
            if (!IsStationInRange())
            {
                feedback = FarmLocalization.Get("interaction.market.too_far", "Move closer to the market crate.");
                return;
            }
            if (gameState.IsLandMaxed)
            {
                feedback = FarmLocalization.Get("land.maxed", "All available land has already been purchased.");
                return;
            }
            var required = gameState.GetLandUpgradeCost();
            if (gameState.TryUpgradeLand(out var newLevel, out var cost))
            {
                EnsureFarmTilesForLandLevel();
                feedback = FarmLocalization.Format("land.purchased", "Land expanded to {0} plot tiles for ${1}.", gameState.LandTileCount, cost);
                actionFeedback?.PlayReward(sellStation.transform.position, newLevel + 2);
                RefreshHoveredArea();
            }
            else feedback = FarmLocalization.Format("land.insufficient_funds", "Not enough money. The expansion costs ${0}.", required);
        }

        public void CycleShopCrop(int direction)
        {
            if (shopCrops.Count <= 1 || direction == 0) return;
            shopCropIndex = (shopCropIndex + (direction > 0 ? 1 : -1) + shopCrops.Count) % shopCrops.Count;
            feedback = FarmLocalization.Format("catalog.selected", "Catalog: {0}.", ShopCrop.LocalizedName);
        }

        public void RequestStorage()
        {
            if (!IsStorageInRange())
            {
                feedback = FarmLocalization.Get("interaction.storage.too_far", "Move closer to the storage chest.");
                return;
            }
            feedback = FarmLocalization.Get("interaction.storage.opened", "Storage opened.");
            hud?.SetStorageOpen(true);
        }

        public void RequestOrderBoard()
        {
            if (!IsOrderBoardInRange())
            {
                feedback = FarmLocalization.Get("interaction.orders.too_far", "Move closer to the order board.");
                return;
            }
            RefreshDailyOrdersCache();
            feedback = FarmLocalization.Get("interaction.orders.opened", "Daily orders opened.");
            hud?.SetDailyOrdersOpen(true);
        }

        public void TryCompleteDailyOrder(int index)
        {
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.DailyOrder, playerName, $"index={index}");
                feedback = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
                return;
            }
            if (!IsOrderBoardInRange())
            {
                feedback = FarmLocalization.Get("interaction.orders.too_far", "Move closer to the order board.");
                return;
            }
            RefreshDailyOrdersCache();
            if (index < 0 || index >= dailyOrders.Count)
            {
                feedback = FarmLocalization.Get("orders.invalid", "Invalid order.");
                return;
            }
            if (gameState.TryCompleteDailyOrder(dailyOrders[index], index, playerName, out var earned, out var bonus, out var community, out var roleContribution, out var error))
            {
                var contact = FarmCommunityCatalog.GetContact(community.ContactId);
                feedback = roleContribution.TeamworkBonus > 0
                    ? FarmLocalization.Format("roles.order_teamwork_bonus", "Order delivered: +${0}. All three co-op roles contributed today: +${1} teamwork bonus.", earned, roleContribution.TeamworkBonus)
                    : community.ReachedMilestone
                    ? FarmLocalization.Format("orders.completed_community_milestone", "Order delivered: +${0}; +{1} Favor with {2}. Community bond {3} reached: +${4}.", earned, community.FavorGained, contact.LocalizedName, community.NewBondLevel, community.MilestoneReward)
                    : bonus > 0
                    ? FarmLocalization.Format("orders.completed_bonus", "Order delivered: +${0} (includes full-board bonus of ${1}).", earned, bonus)
                    : FarmLocalization.Format("orders.completed_community", "Order delivered: +${0}; +{1} Favor with {2}.", earned, community.FavorGained, contact.LocalizedName);
                actionFeedback?.PlayReward(orderBoardStation.transform.position, bonus > 0 ? 3 : 1);
                SaveGame(false);
            }
            else feedback = error;
        }

        private void RefreshDailyOrdersCache()
        {
            if (gameState == null) return;
            if (cachedOrdersDay == gameState.DayNumber && cachedOrdersSeed == gameState.WorldSeed && dailyOrders.Count > 0) return;
            cachedOrdersDay = gameState.DayNumber;
            cachedOrdersSeed = gameState.WorldSeed;
            dailyOrders.Clear();
            dailyOrders.AddRange(FarmDailyOrderGenerator.Generate(gameState.WorldSeed, gameState.DayNumber));
        }

        public void RequestSleep()
        {
            if (!IsSleepInRange())
            {
                feedback = FarmLocalization.Get("interaction.bed.too_far", "Move closer to the bed.");
                return;
            }

            feedback = sleepSession != null
                ? FarmLocalization.Format("sleep.confirm_prompt", "{0} Confirm your readiness.", sleepSession.StatusText)
                : FarmLocalization.Get("sleep.confirm_end_day", "Confirm to end the day.");
            hud?.SetSleepConfirmationOpen(true);
        }

        public void ConfirmSleep()
        {
            if (gameState == null || dayClock == null || sleepSession == null || !IsSleepInRange())
            {
                hud?.SetSleepConfirmationOpen(false);
                feedback = FarmLocalization.Get("sleep.too_far", "Unable to set sleep readiness while away from the bed.");
                return;
            }
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.SleepReadiness, playerName, "ready=true");
                hud?.SetSleepConfirmationOpen(false);
                feedback = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
                return;
            }

            sleepSession.SetLocalReady(true);
            hud?.SetSleepConfirmationOpen(false);
            if (!sleepSession.CanAdvanceDay)
                feedback = FarmLocalization.Format("sleep.ready", "You are ready. {0}", sleepSession.StatusText);
        }

        public void CancelSleepReady()
        {
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.SleepReadiness, playerName, "ready=false");
                hud?.SetSleepConfirmationOpen(false);
                feedback = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
                return;
            }
            sleepSession?.SetLocalReady(false);
            hud?.SetSleepConfirmationOpen(false);
            feedback = FarmLocalization.Get("sleep.cancelled", "Sleep readiness cancelled.");
        }

        public void PrepareEveningTea()
        {
            if (gameState == null || !IsSleepInRange())
            {
                feedback = FarmLocalization.Get("sleep.too_far", "Unable to set sleep readiness while away from the bed.");
                return;
            }
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.RestRecovery, playerName, "action=evening_tea");
                feedback = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
                return;
            }
            if (gameState.TryPrepareEveningTea(out var error))
                feedback = FarmLocalization.Get("rest.prepared", "Evening Tea prepared. Tomorrow begins with 3 Comfort charges.");
            else feedback = error;
        }

        public string EveningPreparationStatus
        {
            get
            {
                if (gameState == null) return string.Empty;
                if (gameState.HomesteadRest.PreparedOnDay == gameState.DayNumber)
                    return FarmLocalization.Get("rest.status.prepared", "EVENING PLAN: Comfort prepared. Tomorrow's next 3 Energy costs are reduced by 1.");
                if (gameState.ForecastPlan.TargetDay == gameState.DayNumber + 1)
                    return FarmLocalization.Format("evening.status.route", "EVENING PLAN: {0}", FarmForecastPlanRules.Description(gameState.ForecastPlan.RouteKey));
                return FarmLocalization.Get("evening.status.none", "EVENING PLAN: Choose Comfort at the bed or prepare tomorrow's route in the Planner.");
            }
        }

        private void HandleSleepReadinessChanged()
        {
            if (!FarmSessionTime.IsSimulationAuthority || sleepSession == null || advancingDay) return;
            QueueWorldSnapshot();
            if (sleepSession.CanAdvanceDay)
            {
                AdvanceDayAfterSessionApproval();
                return;
            }

            if (sleepSession.IsLocalReady)
                feedback = FarmLocalization.Format("sleep.ready", "You are ready. {0}", sleepSession.StatusText);
        }

        private void AdvanceDayAfterSessionApproval()
        {
            if (!FarmSessionTime.IsSimulationAuthority || advancingDay || gameState == null || dayClock == null) return;
            advancingDay = true;
            try
            {
                var currentMinute = Mathf.Repeat(gameState.MinutesOfDay, 1440f);
                var skippedGameMinutes = (1440f - currentMinute) + 360f;
                var growthSeconds = dayClock.RealSecondsForGameMinutes(skippedGameMinutes);
                var advancedCrops = 0;
                foreach (var tile in tiles)
                    if (tile.AdvanceGrowth(growthSeconds)) advancedCrops++;

                var recoveredEnergy = gameState.RestoreEnergy();
                var nextDay = Mathf.Max(1, gameState.DayNumber + 1);
                dayClock.SetClock(nextDay, 360f);
                var comfortPrepared = gameState.BeginHomesteadRestDay(nextDay);
                var sprinklerWatered = NotifyMorningStarted();
                sleepSession?.ClearReadiness();
                hud?.SetSleepConfirmationOpen(false);
                var morningContext = dayClock.SeasonDisplayText;
                if (weatherSystem != null) morningContext += $"  \u2022  {FarmWeatherSystem.WeatherName(weatherSystem.CurrentWeather)}";
                hud?.ShowDayTransition(nextDay, morningContext);
                feedback = advancedCrops > 0
                    ? FarmLocalization.Format("morning.crops_advanced", "Good morning! {0} crop(s) advanced overnight.", advancedCrops)
                    : FarmLocalization.Get("morning.new_day", "Good morning! A new day has begun on the farm.");
                feedback += recoveredEnergy > 0 ? FarmLocalization.Format("day.energy_restored", " Energy restored: +{0}.", recoveredEnergy) : FarmLocalization.Get("day.energy_full", " Energy was already full.");
                if (comfortPrepared) feedback += FarmLocalization.Get("rest.morning_bonus", " Evening Tea is ready: the next 3 Energy costs are reduced by 1.");
                if (sprinklerWatered > 0) feedback += FarmLocalization.Format("morning.sprinklers_suffix", " Sprinklers watered {0} plot tile(s).", sprinklerWatered);
                if (lastMorningProcessedStored > 0) feedback += FarmLocalization.Format("morning.processor_storage", " Workshop storage collected {0} processed item(s).", lastMorningProcessedStored);
                if (lastMorningPestProtected > 0) feedback += FarmLocalization.Format("morning.scarecrows_suffix", " Scarecrows protected {0} crop(s).", lastMorningPestProtected);
                if (lastMorningPestAffected > 0) feedback += FarmLocalization.Get("morning.crows_suffix", " Crows delayed one crop by 2 seconds.");
                SaveGame(false);
            }
            finally
            {
                advancingDay = false;
            }
        }

        public async void SelectHotbarSlot(int index)
        {
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.HotbarSelection, playerName, index.ToString());
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            if (gameState == null) return;
            if (!useAuthoritativeCore)
            {
                if (!gameState.SelectHotbarSlot(index)) return;
                ApplySelectedHotbarEntry(true);
                return;
            }
            if (authoritativeCore == null || authoritativeCore.IsCommandInFlight) return;
            var result = await authoritativeCore.SelectHotbarAsync(index);
            if (!result.Succeeded)
            {
                feedback = result.Message;
                return;
            }
            ApplySelectedHotbarEntry(true);
        }

        public async Task<bool> AssignItemToHotbarAsync(int index, string itemId)
        {
            if (gameState == null || string.IsNullOrWhiteSpace(itemId)) return false;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.HotbarAssignment, playerName, $"slot={index};item={itemId}");
                return false;
            }
            if (!useAuthoritativeCore)
            {
                if (gameState.GetQuantity(itemId) <= 0 || !gameState.AssignHotbarSlot(index, FarmGameState.ItemPrefix + itemId)) return false;
                gameState.SelectHotbarSlot(index);
                ApplySelectedHotbarEntry(true);
                return true;
            }
            if (authoritativeCore == null) return false;
            authoritativeCore.SynchronizePrototypeInventory();
            var result = await authoritativeCore.SetHotbarAsync(index, itemId);
            feedback = result.Message;
            if (!result.Succeeded) return false;
            ApplySelectedHotbarEntry(true);
            return true;
        }

        public async Task<bool> SwapHotbarSlotsAsync(int sourceIndex, int targetIndex)
        {
            if (gameState == null || sourceIndex < 0 || targetIndex < 0 ||
                sourceIndex >= FarmGameState.HotbarSlotCount || targetIndex >= FarmGameState.HotbarSlotCount) return false;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.HotbarAssignment, playerName, $"swap={sourceIndex}:{targetIndex}");
                return false;
            }
            if (!useAuthoritativeCore)
            {
                if (!gameState.SwapHotbarSlots(sourceIndex, targetIndex)) return false;
                ApplySelectedHotbarEntry(true);
                return true;
            }
            if (authoritativeCore == null) return false;
            var result = await authoritativeCore.SwapHotbarAsync(sourceIndex, targetIndex);
            feedback = result.Message;
            if (!result.Succeeded) return false;
            ApplySelectedHotbarEntry(true);
            return true;
        }

        public async Task ClearHotbarSlotAsync(int index)
        {
            if (gameState == null) return;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.HotbarAssignment, playerName, $"slot={index};item=");
                return;
            }
            if (!useAuthoritativeCore)
            {
                gameState.ClearHotbarSlot(index);
                if (gameState.SelectedHotbarIndex == index) ApplySelectedHotbarEntry(true);
                return;
            }
            if (authoritativeCore == null) return;
            var result = await authoritativeCore.SetHotbarAsync(index, string.Empty);
            feedback = result.Message;
            if (result.Succeeded) ApplySelectedHotbarEntry(true);
        }

        public async Task<bool> RestoreCoreToolToHotbarAsync(int index, string toolEntry)
        {
            if (gameState == null || !FarmGameState.IsCoreToolEntry(toolEntry)) return false;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.HotbarAssignment, playerName, $"slot={index};tool={toolEntry}");
                return false;
            }
            if (!useAuthoritativeCore)
            {
                if (!gameState.AssignHotbarSlot(index, toolEntry)) return false;
                ApplySelectedHotbarEntry(true);
                return true;
            }
            if (authoritativeCore == null) return false;
            var result = await authoritativeCore.SetHotbarAsync(index, toolEntry);
            feedback = result.Message;
            if (!result.Succeeded) return false;
            ApplySelectedHotbarEntry(true);
            return true;
        }

        public bool AssignItemToHotbar(int index, string itemId)
        {
            if (useAuthoritativeCore) return false;
            if (!FarmSessionTime.IsSimulationAuthority) return false;
            if (gameState == null || string.IsNullOrWhiteSpace(itemId) || gameState.GetQuantity(itemId) <= 0) return false;
            if (!gameState.AssignHotbarSlot(index, FarmGameState.ItemPrefix + itemId)) return false;
            gameState.SelectHotbarSlot(index);
            ApplySelectedHotbarEntry(true);
            return true;
        }

        public void ClearHotbarSlot(int index)
        {
            if (useAuthoritativeCore || gameState == null) return;
            if (!FarmSessionTime.IsSimulationAuthority) return;
            gameState.ClearHotbarSlot(index);
            if (gameState.SelectedHotbarIndex == index) ApplySelectedHotbarEntry(true);
        }

        public bool MarkMilestone(FarmMilestone milestone) => gameState != null && gameState.MarkMilestone(milestone);

        public void SetExternalPrompt(object source, string value)
        {
            if (source == null) return;
            if (string.IsNullOrWhiteSpace(value)) externalPrompts.Remove(source);
            else externalPrompts[source] = value;
        }

        private string ResolveExternalPrompt()
        {
            foreach (var entry in externalPrompts)
                if (!string.IsNullOrWhiteSpace(entry.Value)) return entry.Value;
            return null;
        }

        private void UpdateHotbarSelection()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit1Key.wasPressedThisFrame) SelectHotbarSlot(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectHotbarSlot(1);
            if (keyboard.digit3Key.wasPressedThisFrame) SelectHotbarSlot(2);
            if (keyboard.digit4Key.wasPressedThisFrame) SelectHotbarSlot(3);
            if (keyboard.digit5Key.wasPressedThisFrame) SelectHotbarSlot(4);
            if (keyboard.digit6Key.wasPressedThisFrame) SelectHotbarSlot(5);
            if (keyboard.digit7Key.wasPressedThisFrame) SelectHotbarSlot(6);
            if (keyboard.digit8Key.wasPressedThisFrame) SelectHotbarSlot(7);
        }

        private void ApplySelectedHotbarEntry(bool showFeedback)
        {
            selectedItemId = null;
            var entry = gameState != null ? gameState.SelectedHotbarEntry : string.Empty;
            if (entry == "tool:hoe") activeTool = FarmTool.Hoe;
            else if (entry == "tool:watering_can") activeTool = FarmTool.WateringCan;
            else if (entry == "tool:harvest") activeTool = FarmTool.Harvest;
            else if (entry == "tool:axe") activeTool = FarmTool.Axe;
            else if (entry == "tool:pickaxe") activeTool = FarmTool.Pickaxe;
            else if (entry.StartsWith(FarmGameState.ItemPrefix, StringComparison.OrdinalIgnoreCase))
            {
                selectedItemId = entry[FarmGameState.ItemPrefix.Length..];
                var item = FarmContentDatabase.GetItem(selectedItemId);
                activeTool = item != null && item.Category == ItemCategory.Seed ? FarmTool.Seeds
                    : item != null && item.Category == ItemCategory.Fertilizer ? FarmTool.Fertilizer
                    : FarmTool.None;
                var selectedCrop = FarmContentDatabase.GetCropForSeed(selectedItemId);
                if (selectedCrop != null) activeCrop = selectedCrop;
            }
            else activeTool = FarmTool.None;

            RefreshHoveredArea();
            if (!showFeedback) return;
            if (string.IsNullOrEmpty(entry)) feedback = FarmLocalization.Get("hotbar.empty", "Empty hotbar slot. Open the inventory with I and drag an item here.");
            else if (!string.IsNullOrEmpty(selectedItemId))
            {
                var item = FarmContentDatabase.GetItem(selectedItemId);
                feedback = item != null ? FarmLocalization.Format("hotbar.selected_item", "{0} selected.", item.LocalizedName) : FarmLocalization.Get("hotbar.item_selected", "Item selected.");
            }
            else feedback = FarmLocalization.Format("hotbar.selected_tool", "{0} selected.", ToolName(activeTool));
        }

        private void RefreshShopCatalog()
        {
            var selectedId = ShopCrop != null ? ShopCrop.Id : activeCrop != null ? activeCrop.Id : string.Empty;
            shopCrops.Clear();
            foreach (var crop in FarmContentDatabase.Crops)
                if (crop != null && crop.SeedItem != null && crop.HarvestItem != null) shopCrops.Add(crop);
            shopCrops.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.LocalizedName, right.LocalizedName));
            shopCropIndex = Mathf.Max(0, shopCrops.FindIndex(crop => string.Equals(crop.Id, selectedId, StringComparison.OrdinalIgnoreCase)));
        }

        private void CreatePlotAndStation()
        {
            if (player == null)
            {
                feedback = FarmLocalization.Get("player.missing", "Player not found.");
                return;
            }

            var forward = Camera.main != null ? Camera.main.transform.forward : player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            var center = PlaceOnGround(player.position + (forward * plotDistanceFromPlayer));
            plotCenter = center;
            plotForward = forward;
            tileRoot = new GameObject("Farm_Test_Grid").transform;
            tileRoot.SetParent(transform, true);
            var offset = (gridSize - 1) * tileSize * 0.5f;
            plotGridOffset = offset;
            EnsureFarmTilesForLandLevel();

            CreateSellStation(center, forward, offset);
            CreateStorageStation(center, forward, offset);
            CreateSleepStation(center, forward, offset);
            CreateOrderBoardStation(center, forward, offset);
            feedback = FarmLocalization.Get("plot.ready", "Farm plot ready. Planting uses inventory hotbar slots.");
        }

        public void EnsureFarmTilesForLandLevel()
        {
            if (tileRoot == null)
            {
                tileRoot = new GameObject("Farm_Test_Grid").transform;
                tileRoot.SetParent(transform, true);
            }

            var targetCount = gameState != null ? gameState.LandTileCount : FarmGameState.GetLandTileCount(FarmGameState.MinLandLevel);
            while (tiles.Count > targetCount)
            {
                var lastIndex = tiles.Count - 1;
                var removed = tiles[lastIndex];
                tiles.RemoveAt(lastIndex);
                if (removed == null) continue;
                removed.gameObject.SetActive(false);
                Destroy(removed.gameObject);
            }

            var coordinates = BuildLandCoordinates();
            while (tiles.Count < targetCount)
            {
                var index = tiles.Count;
                var coordinate = coordinates[index];
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = index < 9
                    ? $"Farm_Tile_{index / 3}_{index % 3}"
                    : $"Farm_Tile_Expansion_{index:00}";
                tile.transform.SetParent(tileRoot, true);
                tile.transform.position = plotCenter + new Vector3(coordinate.x * tileSize, 0.09f, coordinate.y * tileSize);
                tile.transform.localScale = new Vector3(tileSize - 0.12f, 0.18f, tileSize - 0.12f);
                var farmTile = tile.AddComponent<FarmTestTile>();
                farmTile.Initialize(this, index, activeCrop);
                tiles.Add(farmTile);
            }
        }

        private static List<Vector2Int> BuildLandCoordinates()
        {
            var result = new List<Vector2Int>(25);
            for (var x = -1; x <= 1; x++)
                for (var z = -1; z <= 1; z++)
                    result.Add(new Vector2Int(x, z));
            for (var z = -1; z <= 1; z++) result.Add(new Vector2Int(-2, z));
            for (var z = -1; z <= 1; z++) result.Add(new Vector2Int(2, z));
            for (var x = -2; x <= 2; x++) result.Add(new Vector2Int(x, -2));
            for (var x = -2; x <= 2; x++) result.Add(new Vector2Int(x, 2));
            return result;
        }

        public void RebuildWorldPickups()
        {
            if (pickupRoot != null) Destroy(pickupRoot.gameObject);
            if (gameState == null || hud == null || activeCrop == null || player == null) return;
            pickupRoot = new GameObject("Farm_World_Pickups").transform;
            pickupRoot.SetParent(transform, true);
            var right = Vector3.Cross(Vector3.up, plotForward).normalized;
            CreateWorldPickup("starter_seed_cache", activeCrop.SeedItem.Id, 3, plotCenter + (plotForward * (plotGridOffset + 2.3f)), activeCrop.SmallModel, 0.5f);
            CreateWorldPickup("starter_pumpkin_find", activeCrop.HarvestItem.Id, 1, plotCenter - (plotForward * (plotGridOffset + 2.3f)), activeCrop.LargeModel, 0.42f);
            CreateWorldPickup("starter_seed_fence", activeCrop.SeedItem.Id, 2, plotCenter - (right * (plotGridOffset + 1.7f)) + (plotForward * 1.2f), activeCrop.SmallModel, 0.5f);
            var season = dayClock != null ? dayClock.CurrentSeason : FarmSeason.Spring;
            var weather = weatherSystem != null ? weatherSystem.CurrentWeather : FarmWeather.Clear;
            var forage = FarmForageCatalog.Generate(gameState.WorldSeed, gameState.DayNumber, season, weather);
            var routeOffsets = new[]
            {
                new Vector2(-1.0f, 5.0f),
                new Vector2(3.6f, 3.2f),
                new Vector2(-4.2f, -2.8f)
            };
            for (var index = 0; index < forage.Count; index++)
            {
                var offset = routeOffsets[index % routeOffsets.Length];
                var position = plotCenter + (right * (plotGridOffset + offset.x)) + (plotForward * (plotGridOffset + offset.y));
                CreateWorldPickup(forage[index].PickupId, forage[index].ItemId, forage[index].Quantity, position, activeCrop.LargeModel, 0.30f);
            }
            if (FarmExplorationRouteCatalog.TryGetRoute(gameState.WorldSeed, gameState.DayNumber, season, weather, dayClock != null ? dayClock.Phase : FarmDayPhase.Morning, out var route))
            {
                if (gameState.HasForecastPlanForRoute(gameState.DayNumber, route.RouteKey)) route = route.WithForecastPreparation();
                var routePosition = plotCenter + (right * (plotGridOffset + 8.5f)) + (plotForward * (plotGridOffset + 7.5f));
                CreateWorldPickup(route.PickupId, route.ItemId, route.Quantity, routePosition, activeCrop.LargeModel, 0.38f, route.EnergyCost);
            }
        }

        private void CreateWorldPickup(string pickupId, string itemId, int quantity, Vector3 position, GameObject model, float modelScale, int routeEnergyCost = 0)
        {
            if (gameState.IsPickupCollected(pickupId)) return;
            var pickupObject = new GameObject($"Pickup_{pickupId}");
            pickupObject.transform.SetParent(pickupRoot, true);
            pickupObject.transform.position = PlaceOnGround(position) + (Vector3.up * 0.7f);
            var trigger = pickupObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.55f;

            Transform visual = null;
            if (model != null)
            {
                var modelObject = Instantiate(model, pickupObject.transform);
                modelObject.name = "ItemVisual";
                modelObject.transform.localPosition = Vector3.zero;
                modelObject.transform.localRotation = Quaternion.identity;
                modelObject.transform.localScale = Vector3.one * modelScale;
                foreach (var modelCollider in modelObject.GetComponentsInChildren<Collider>()) Destroy(modelCollider);
                visual = modelObject.transform;
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "PickupMarker";
            marker.transform.SetParent(pickupObject.transform, false);
            marker.transform.localPosition = new Vector3(0f, -0.56f, 0f);
            marker.transform.localScale = new Vector3(0.48f, 0.025f, 0.48f);
            var markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null) Destroy(markerCollider);
            marker.GetComponent<Renderer>().material.color = new Color(1f, 0.72f, 0.16f);

            pickupObject.AddComponent<FarmWorldPickup>().Initialize(pickupId, itemId, quantity, player, gameState, hud, visual, routeEnergyCost);
        }
        private void CreateSellStation(Vector3 plotCenter, Vector3 forward, float gridOffset)
        {
            var cratePrefab = Resources.Load<GameObject>("FarmProps/SellCrate");
            GameObject stationObject;
            if (cratePrefab != null)
            {
                stationObject = Instantiate(cratePrefab);
            }
            else
            {
                stationObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stationObject.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
                Debug.LogWarning("SellCrate n\u00E3o encontrado; usando marcador tempor\u00E1rio.");
            }

            stationObject.name = "Farm_Sell_Station";
            stationObject.transform.SetParent(transform, true);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            stationObject.transform.position = PlaceOnGround(plotCenter + (right * (gridOffset + 3.5f)));
            stationObject.transform.rotation = Quaternion.LookRotation(-right, Vector3.up);
            if (stationObject.GetComponentInChildren<Collider>() == null) stationObject.AddComponent<BoxCollider>();
            sellStation = stationObject.AddComponent<FarmSellStation>();
            sellStation.Initialize();
        }

        private void CreateStorageStation(Vector3 plotCenter, Vector3 forward, float gridOffset)
        {
            var cratePrefab = Resources.Load<GameObject>("FarmProps/SellCrate");
            GameObject stationObject;
            if (cratePrefab != null) stationObject = Instantiate(cratePrefab);
            else
            {
                stationObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stationObject.transform.localScale = new Vector3(1.35f, 0.9f, 1.35f);
                Debug.LogWarning("Prop de dep\u00F3sito n\u00E3o encontrado; usando marcador tempor\u00E1rio.");
            }

            stationObject.name = "Farm_Storage_Chest";
            stationObject.transform.SetParent(transform, true);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            stationObject.transform.position = PlaceOnGround(plotCenter - (right * (gridOffset + 3.5f)));
            stationObject.transform.rotation = Quaternion.LookRotation(right, Vector3.up);
            if (stationObject.GetComponentInChildren<Collider>() == null) stationObject.AddComponent<BoxCollider>();
            storageStation = stationObject.AddComponent<FarmStorageStation>();
            storageStation.Initialize();

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Storage_Blue_Marker";
            marker.transform.SetParent(stationObject.transform, false);
            marker.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            marker.transform.localScale = new Vector3(0.18f, 0.08f, 0.18f);
            var markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null) Destroy(markerCollider);
            marker.GetComponent<Renderer>().material.color = new Color(0.22f, 0.65f, 0.95f);
        }
        private void CreateSleepStation(Vector3 plotCenter, Vector3 forward, float gridOffset)
        {
            var bedPrefab = Resources.Load<GameObject>("FarmProps/SleepBed");
            GameObject stationObject;
            if (bedPrefab != null) stationObject = Instantiate(bedPrefab);
            else
            {
                stationObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stationObject.transform.localScale = new Vector3(1.2f, 0.55f, 2.1f);
                Debug.LogWarning("SleepBed n\u00E3o encontrado; usando cama tempor\u00E1ria.");
            }

            stationObject.name = "Farm_Sleep_Bed";
            stationObject.transform.SetParent(transform, true);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            stationObject.transform.position = PlaceOnGround(plotCenter - (forward * (gridOffset + 4.3f)) + (right * 1.8f));
            stationObject.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            stationObject.transform.localScale *= 0.82f;
            if (stationObject.GetComponentInChildren<Collider>() == null) stationObject.AddComponent<BoxCollider>();
            sleepStation = stationObject.AddComponent<FarmSleepStation>();
            sleepStation.Initialize();

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Sleep_Moon_Marker";
            marker.transform.SetParent(stationObject.transform, false);
            marker.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            marker.transform.localScale = new Vector3(0.22f, 0.06f, 0.22f);
            var markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null) Destroy(markerCollider);
            marker.GetComponent<Renderer>().material.color = new Color(0.50f, 0.42f, 0.95f);
        }

        private void CreateOrderBoardStation(Vector3 plotCenter, Vector3 forward, float gridOffset)
        {
            var boardPrefab = Resources.Load<GameObject>("FarmProps/OrderBoard");
            GameObject stationObject;
            if (boardPrefab != null) stationObject = Instantiate(boardPrefab);
            else
            {
                stationObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stationObject.transform.localScale = new Vector3(1.8f, 1.4f, 0.25f);
                Debug.LogWarning("OrderBoard n\u00E3o encontrado; usando quadro tempor\u00E1rio.");
            }
            stationObject.name = "Farm_Daily_Order_Board";
            stationObject.transform.SetParent(transform, true);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            stationObject.transform.position = PlaceOnGround(plotCenter + (forward * (gridOffset + 4.3f)) - (right * 1.8f));
            stationObject.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            stationObject.transform.localScale *= 0.9f;
            if (stationObject.GetComponentInChildren<Collider>() == null) stationObject.AddComponent<BoxCollider>();
            orderBoardStation = stationObject.AddComponent<FarmOrderBoardStation>();
            orderBoardStation.Initialize();

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Order_Board_Cyan_Marker";
            marker.transform.SetParent(stationObject.transform, false);
            marker.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            marker.transform.localScale = new Vector3(0.20f, 0.06f, 0.20f);
            var markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null) Destroy(markerCollider);
            marker.GetComponent<Renderer>().material.color = new Color(0.22f, 0.88f, 0.82f);
        }

        private static Vector3 PlaceOnGround(Vector3 position)
        {
            if (Physics.Raycast(position + (Vector3.up * 50f), Vector3.down, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore))
                position.y = hit.point.y;
            return position;
        }

        private void UpdateHoveredTarget()
        {
            SetHoveredTile(null);
            SetHoveredStation(null);
            SetHoveredStorage(null);
            SetHoveredSleep(null);
            SetHoveredOrderBoard(null);
            if (Camera.main == null || Mouse.current == null) return;

            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, 250f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                var tile = hit.collider.GetComponentInParent<FarmTestTile>();
                if (tile != null)
                {
                    SetHoveredTile(tile);
                    return;
                }

                var station = hit.collider.GetComponentInParent<FarmSellStation>();
                if (station != null)
                {
                    SetHoveredStation(station);
                    return;
                }

                var storage = hit.collider.GetComponentInParent<FarmStorageStation>();
                if (storage != null)
                {
                    SetHoveredStorage(storage);
                    return;
                }

                var sleep = hit.collider.GetComponentInParent<FarmSleepStation>();
                if (sleep != null)
                {
                    SetHoveredSleep(sleep);
                    return;
                }

                var orderBoard = hit.collider.GetComponentInParent<FarmOrderBoardStation>();
                if (orderBoard != null)
                {
                    SetHoveredOrderBoard(orderBoard);
                    return;
                }
            }
        }

        private void SetHoveredTile(FarmTestTile tile)
        {
            if (hoveredTile == tile) return;
            hoveredTile = tile;
            RefreshHoveredArea();
        }

        private void RefreshHoveredArea()
        {
            foreach (var tile in tiles) tile.SetHovered(false);
            if (hoveredTile == null) return;
            foreach (var tile in GetAffectedTiles(hoveredTile, activeTool)) tile.SetHovered(true);
        }

        private void SetHoveredStation(FarmSellStation station)
        {
            if (hoveredStation == station) return;
            if (hoveredStation != null) hoveredStation.SetHovered(false);
            hoveredStation = station;
            if (hoveredStation != null) hoveredStation.SetHovered(true);
        }

        private void SetHoveredStorage(FarmStorageStation station)
        {
            if (hoveredStorage == station) return;
            if (hoveredStorage != null) hoveredStorage.SetHovered(false);
            hoveredStorage = station;
            if (hoveredStorage != null) hoveredStorage.SetHovered(true);
        }
        private void SetHoveredSleep(FarmSleepStation station)
        {
            if (hoveredSleep == station) return;
            if (hoveredSleep != null) hoveredSleep.SetHovered(false);
            hoveredSleep = station;
            if (hoveredSleep != null) hoveredSleep.SetHovered(true);
        }

        private void SetHoveredOrderBoard(FarmOrderBoardStation station)
        {
            if (hoveredOrderBoard == station) return;
            if (hoveredOrderBoard != null) hoveredOrderBoard.SetHovered(false);
            hoveredOrderBoard = station;
            if (hoveredOrderBoard != null) hoveredOrderBoard.SetHovered(true);
        }

        private bool IsInRange(Vector3 targetPosition) =>
            player != null && Vector3.Distance(player.position, targetPosition) <= interactionDistance;

        private bool IsStationInRange() =>
            player != null && sellStation != null && Vector3.Distance(player.position, sellStation.transform.position) <= SellStationInteractionDistance;

        private bool IsStorageInRange() =>
            player != null && storageStation != null && Vector3.Distance(player.position, storageStation.transform.position) <= StorageInteractionDistance;

        private bool IsSleepInRange() =>
            player != null && sleepStation != null && Vector3.Distance(player.position, sleepStation.transform.position) <= SleepInteractionDistance;

        private bool IsOrderBoardInRange() =>
            player != null && orderBoardStation != null && Vector3.Distance(player.position, orderBoardStation.transform.position) <= OrderBoardInteractionDistance;

        private async void TryPrimaryInteraction()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (hoveredTile != null)
            {
                if (!IsInRange(hoveredTile.transform.position))
                {
                    feedback = FarmLocalization.Get("interaction.tile.too_far", "Move closer to the plot tile.");
                    return;
                }
                feedback = await UseToolOnTileAsync(hoveredTile);
                return;
            }

            if (hoveredStation != null) RequestShop();
            else if (hoveredStorage != null) RequestStorage();
            else if (hoveredSleep != null) RequestSleep();
            else if (hoveredOrderBoard != null) RequestOrderBoard();
        }

        public IReadOnlyList<FarmTestTile> GetAffectedTiles(FarmTestTile target, FarmTool tool)
        {
            var result = new List<FarmTestTile>();
            if (target == null) return result;
            var level = gameState != null && FarmGameState.IsUpgradeableTool(tool)
                ? (gameState.IsExhausted ? 1 : gameState.GetToolLevel(tool))
                : 1;
            if (level <= 1 || !FarmGameState.IsUpgradeableTool(tool))
            {
                result.Add(target);
                return result;
            }

            var center = target.Index;
            var centerRow = center / gridSize;
            var centerColumn = center % gridSize;
            var rowRadius = level >= 3 ? 1 : 0;
            for (var rowOffset = -rowRadius; rowOffset <= rowRadius; rowOffset++)
            {
                for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
                {
                    var row = centerRow + rowOffset;
                    var column = centerColumn + columnOffset;
                    if (row < 0 || row >= gridSize || column < 0 || column >= gridSize) continue;
                    var index = (row * gridSize) + column;
                    if (index >= 0 && index < tiles.Count) result.Add(tiles[index]);
                }
            }
            return result;
        }

        public async Task<string> UseToolOnTileAsync(FarmTestTile target)
        {
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                var targetIndex = target != null ? target.Index : -1;
                FarmSessionIntentBus.Raise(
                    FarmSessionIntentKind.ToolAction,
                    playerName,
                    $"tool={activeTool};tile={targetIndex};item={selectedItemId ?? string.Empty}");
                return FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
            }
            if (!useAuthoritativeCore || activeTool is not (FarmTool.Hoe or FarmTool.Seeds or FarmTool.WateringCan or FarmTool.Harvest or FarmTool.Fertilizer))
                return UseToolOnTile(target);
            if (target == null || authoritativeCore == null)
                return FarmLocalization.Get("backend.unavailable", "Test backend unavailable.");
            if (authoritativeActionPending || authoritativeCore.IsCommandInFlight)
                return FarmLocalization.Get("backend.waiting", "Waiting for server confirmation.");

            authoritativeActionPending = true;
            try
            {
                if (activeTool is FarmTool.Hoe or FarmTool.WateringCan or FarmTool.Harvest)
                    return await ConfirmAreaToolAsync(activeTool, target);

                if (activeTool == FarmTool.Fertilizer)
                {
                    var fertilizing = await authoritativeCore.FertilizeTileAsync(target.Index, selectedItemId);
                    if (!fertilizing.Succeeded) return fertilizing.Message;
                    if (!target.ApplyConfirmedFertilization())
                        return FarmLocalization.Get("backend.tile.visual_rejected", "The plot tile visual state rejected the confirmation.");
                    return fertilizing.Message + ApplyConfirmedActionEffects(FarmTool.Fertilizer, target);
                }

                var selectedCrop = FarmContentDatabase.GetCropForSeed(selectedItemId);
                if (selectedCrop == null || selectedCrop.SeedItem == null)
                    return FarmLocalization.Get("backend.plant.select_seed", "Select a valid seed in the hotbar.");
                var planting = await authoritativeCore.PlantSeedAsync(target.Index, selectedCrop.SeedItem.Id, selectedCrop.Id);
                if (!planting.Succeeded) return planting.Message;
                if (!target.ApplyConfirmedPlant(selectedCrop))
                    return FarmLocalization.Get("backend.tile.visual_rejected", "The plot tile visual state rejected the confirmation.");
                return FarmLocalization.Format("backend.plant.next_water", "{0} Water it now (3).", planting.Message) + ApplyConfirmedActionEffects(FarmTool.Seeds, target);
            }
            finally
            {
                authoritativeActionPending = false;
            }
        }

        private async Task<string> ConfirmAreaToolAsync(FarmTool tool, FarmTestTile target)
        {
            var changed = 0;
            var failureMessage = string.Empty;
            foreach (var candidate in GetAffectedTiles(target, tool))
            {
                var result = tool switch
                {
                    FarmTool.Hoe => await authoritativeCore.PrepareSoilAsync(candidate.Index),
                    FarmTool.WateringCan => await authoritativeCore.WaterTileAsync(candidate.Index),
                    FarmTool.Harvest => await authoritativeCore.HarvestTileAsync(candidate.Index),
                    _ => null
                };
                if (result == null)
                {
                    failureMessage = FarmLocalization.Get("backend.no_response", "The backend did not respond.");
                    continue;
                }
                if (!result.Succeeded)
                {
                    if (string.IsNullOrEmpty(failureMessage)) failureMessage = result.Message;
                    continue;
                }

                var harvestMessage = string.Empty;
                var visualApplied = tool switch
                {
                    FarmTool.Hoe => candidate.ApplyConfirmedPreparation(),
                    FarmTool.WateringCan => candidate.ApplyConfirmedWatering(),
                    FarmTool.Harvest => candidate.ApplyConfirmedHarvest(result.Harvest, out harvestMessage),
                    _ => false
                };
                if (tool == FarmTool.Harvest && !string.IsNullOrEmpty(harvestMessage)) failureMessage = harvestMessage;
                if (!visualApplied)
                {
                    failureMessage = FarmLocalization.Get("backend.tile.visual_rejected", "The plot tile visual state rejected the confirmation.");
                    continue;
                }
                changed++;
            }

            if (changed <= 0) return string.IsNullOrEmpty(failureMessage)
                ? FarmLocalization.Get("backend.no_response", "The backend did not respond.")
                : failureMessage;

            var message = changed > 1
                ? tool == FarmTool.Hoe
                    ? FarmLocalization.Format("tile.area.hoe", "Prepared {0} plot tiles at once.", changed)
                    : FarmLocalization.Format("tile.area.water", "Watered {0} plot tiles at once.", changed)
                : tool switch
                {
                    FarmTool.Hoe => FarmLocalization.Format("backend.prepare.next_seed", "Soil prepared. Select seeds (2).", FarmLocalization.Get("backend.soil.prepared", "Soil prepared.")),
                    FarmTool.WateringCan => FarmLocalization.Format("tile.watered", "Watered! Ready in {0:0} seconds.", target.GrowthSeconds),
                    FarmTool.Harvest => string.IsNullOrEmpty(failureMessage) ? FarmLocalization.Get("backend.harvest.confirmed", "Harvest confirmed by server.") : failureMessage,
                    _ => FarmLocalization.Get("backend.no_response", "The backend did not respond.")
                };
            return message + ApplyConfirmedActionEffects(tool, target, changed);
        }

        private string ApplyConfirmedActionEffects(FarmTool tool, FarmTestTile target, int changedTiles = 1)
        {
            if (gameState == null || target == null || changedTiles <= 0) return string.Empty;
            var masterySkill = tool == FarmTool.Harvest ? FarmMasterySkill.Harvesting : FarmMasterySkill.Cultivation;
            var masteryAmount = tool == FarmTool.Harvest ? changedTiles * 2 : changedTiles;
            var masteryLevelUp = gameState.AddMasteryExperience(masterySkill, masteryAmount);
            var wasExhausted = gameState.IsExhausted;
            gameState.SpendEnergy(tool, changedTiles);
            actionFeedback?.PlayTool(tool, target.transform.position, changedTiles);

            var suffix = string.Empty;
            if (gameState.LastEnergyActionWasFree)
                suffix += FarmLocalization.Get("tile.energy.free_action", " First wind: this action used no energy.");
            if (masteryLevelUp)
                suffix += FarmLocalization.Format("tile.mastery.level_up", " {0} reached level {1}!", FarmMasteryRules.DisplayName(masterySkill), gameState.GetMasteryLevel(masterySkill));
            if (!wasExhausted && gameState.IsExhausted)
                suffix += FarmLocalization.Get("tile.energy.exhausted", " Energy depleted; area tools now affect one plot tile until sleep.");
            else if (wasExhausted)
                suffix += FarmLocalization.Get("tile.energy.tired", " Tired, but the action was completed.");
            return suffix;
        }
        public string UseToolOnTile(FarmTestTile target)
        {
            if (!FarmSessionTime.IsSimulationAuthority)
                return FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
            if (target == null || gameState == null) return FarmLocalization.Get("tile.none_selected", "No plot tile selected.");
            var affected = GetAffectedTiles(target, activeTool);
            var changed = 0;
            var successMessage = string.Empty;
            var targetMessage = string.Empty;
            foreach (var tile in affected)
            {
                var before = tile.CaptureSaveData();
                var message = tile.Use(activeTool, gameState, selectedItemId);
                var after = tile.CaptureSaveData();
                var didChange = before.State != after.State || before.Fertilized != after.Fertilized ||
                    !string.Equals(before.CropId, after.CropId, StringComparison.OrdinalIgnoreCase);
                if (didChange)
                {
                    changed++;
                    successMessage = message;
                }
                if (tile == target) targetMessage = message;
            }

            var resultMessage = changed <= 1
                ? (!string.IsNullOrEmpty(successMessage) ? successMessage : targetMessage)
                : activeTool switch
                {
                    FarmTool.Hoe => FarmLocalization.Format("tile.area.hoe", "Prepared {0} plot tiles at once.", changed),
                    FarmTool.WateringCan => FarmLocalization.Format("tile.area.water", "Watered {0} plot tiles at once.", changed),
                    FarmTool.Harvest => FarmLocalization.Format("tile.area.harvest", "Harvested {0} plot tiles at once.", changed),
                    _ => successMessage
                };
            if (changed <= 0) return resultMessage;

            var masterySkill = activeTool == FarmTool.Harvest ? FarmMasterySkill.Harvesting : FarmMasterySkill.Cultivation;
            var masteryLevelUp = gameState.AddMasteryExperience(masterySkill, activeTool == FarmTool.Harvest ? changed * 2 : changed);
            var wasExhausted = gameState.IsExhausted;
            gameState.SpendEnergy(activeTool, changed);
            actionFeedback?.PlayTool(activeTool, target.transform.position, changed);
            if (gameState.LastEnergyActionWasFree)
                resultMessage += FarmLocalization.Get("tile.energy.free_action", " First wind: this action used no energy.");
            if (masteryLevelUp)
                resultMessage += FarmLocalization.Format("tile.mastery.level_up", " {0} reached level {1}!", FarmMasteryRules.DisplayName(masterySkill), gameState.GetMasteryLevel(masterySkill));
            if (!wasExhausted && gameState.IsExhausted)
                resultMessage += FarmLocalization.Get("tile.energy.exhausted", " Energy depleted; area tools now affect one plot tile until sleep.");
            else if (wasExhausted)
                resultMessage += FarmLocalization.Get("tile.energy.tired", " Tired, but the action was completed.");
            return resultMessage;
        }

        /// <summary>
        /// Host-side execution point for a validated peer intent. The peer supplies
        /// only its requested tool and target; this method checks the current host
        /// world and routes through the same authoritative backend as local input.
        /// </summary>
        public bool TryExecuteRemoteToolAction(FarmTool requestedTool, int tileIndex, string itemId, out Task<string> execution)
        {
            execution = Task.FromResult(FarmLocalization.Get("session.command.rejected", "The host rejected this farm action."));
            if (!FarmSessionTime.IsSimulationAuthority || gameState == null || tileIndex < 0 || tileIndex >= tiles.Count) return false;
            if (requestedTool is not (FarmTool.Hoe or FarmTool.Seeds or FarmTool.WateringCan or FarmTool.Harvest or FarmTool.Fertilizer)) return false;
            if (requestedTool == FarmTool.Seeds && (string.IsNullOrWhiteSpace(itemId) || FarmContentDatabase.GetCropForSeed(itemId) == null)) return false;
            if (requestedTool == FarmTool.Fertilizer && FarmContentDatabase.GetItem(itemId)?.Category != ItemCategory.Fertilizer) return false;
            execution = ExecuteRemoteToolActionAsync(requestedTool, tiles[tileIndex], itemId);
            return true;
        }

        public bool TrySetRemoteSleepReadiness(string participantId, bool ready)
        {
            if (!FarmSessionTime.IsSimulationAuthority || sleepSession == null) return false;
            sleepSession.EnsureParticipant(participantId);
            return sleepSession.SetParticipantReady(participantId, ready);
        }

        private async Task<string> ExecuteRemoteToolActionAsync(FarmTool requestedTool, FarmTestTile target, string itemId)
        {
            var previousTool = activeTool;
            var previousItemId = selectedItemId;
            try
            {
                activeTool = requestedTool;
                selectedItemId = requestedTool is FarmTool.Seeds or FarmTool.Fertilizer ? itemId : null;
                return await UseToolOnTileAsync(target);
            }
            finally
            {
                activeTool = previousTool;
                selectedItemId = previousItemId;
            }
        }

        private void InteractWithStation(bool buySeeds)
        {
            if (!IsStationInRange())
            {
                feedback = FarmLocalization.Get("interaction.market.too_far", "Move closer to the market crate.");
                return;
            }

            if (sessionCommerce == null)
            {
                feedback = FarmLocalization.Get("commerce.unavailable", "Commerce unavailable: session is not initialized.");
                return;
            }

            var request = buySeeds
                ? FarmCommerceRequest.BuySeedPack(playerName, ShopCrop != null ? ShopCrop.Id : null)
                : FarmCommerceRequest.SellAllCrops(playerName);
            var result = sessionCommerce.Execute(request);
            feedback = result.Message;
            if (!result.Succeeded) return;

            if (buySeeds)
            {
                var advanced = gameState.MarkMilestone(FarmMilestone.BoughtSeeds);
                if (advanced && gameState.Tutorial.IsComplete)
                    feedback += FarmLocalization.Get("tutorial.complete_bonus", " First harvest complete: +$50 bonus!");
            }
            else
            {
                var advanced = gameState.MarkMilestone(FarmMilestone.Sold);
                if (advanced && gameState.Tutorial.IsComplete)
                    feedback += FarmLocalization.Get("tutorial.complete_bonus", " First harvest complete: +$50 bonus!");
            }
        }

        public int ApplySprinklerWatering(Vector3 center, float radius)
        {
            var watered = 0;
            var safeRadius = Mathf.Max(0.5f, radius);
            var radiusSquared = safeRadius * safeRadius;
            foreach (var tile in tiles)
            {
                var delta = tile.transform.position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude <= radiusSquared && tile.ApplyAutomaticWatering()) watered++;
            }
            return watered;
        }

        public int NotifyMorningStarted()
        {
            weatherSystem?.Refresh();
            RebuildWorldPickups();
            lastMorningPestAffected = 0;
            lastMorningPestProtected = 0;
            lastMorningProcessedStored = 0;
            if (gameState == null || !gameState.TryBeginMorningAutomation(gameState.DayNumber)) return 0;
            var watered = buildingSystem != null ? buildingSystem.ApplyMorningSprinklers() : 0;
            if (FarmSessionTime.IsSimulationAuthority && gameState.HasPlacedItem(FarmGameState.FarmShedKitId))
                lastMorningProcessedStored = gameState.CollectCompletedProcessingToStorage();
            if (FarmPestRules.IsVisitDay(gameState.DayNumber))
                lastMorningPestAffected = ApplyPestVisit(out lastMorningPestProtected);
            if (lastMorningPestProtected > 0)
                feedback = FarmLocalization.Format("morning.scarecrows", "Good morning! Scarecrows protected {0} crop(s) from crows.", lastMorningPestProtected);
            else if (lastMorningPestAffected > 0)
                feedback = FarmLocalization.Get("morning.crows", "Good morning! Crows delayed one crop by 2 seconds.");
            else if (watered > 0)
                feedback = FarmLocalization.Format("morning.sprinklers", "Good morning! Sprinklers watered {0} plot tile(s).", watered);
            else if (lastMorningProcessedStored > 0)
                feedback = FarmLocalization.Format("morning.processor_storage", "Workshop storage collected {0} processed item(s).", lastMorningProcessedStored);
            QueueSave();
            return watered;
        }

        private int ApplyPestVisit(out int protectedCrops)
        {
            protectedCrops = 0;
            var exposed = new List<FarmTestTile>();
            foreach (var tile in tiles)
            {
                if (!tile.CanReceivePestDelay) continue;
                if (buildingSystem != null && buildingSystem.IsProtectedByScarecrow(tile.transform.position)) protectedCrops++;
                else exposed.Add(tile);
            }
            buildingSystem?.PulseScarecrows();
            if (exposed.Count == 0) return 0;
            var selector = (gameState.WorldSeed ^ (gameState.DayNumber * 73856093)) & int.MaxValue;
            var target = exposed[selector % exposed.Count];
            if (!target.ApplyPestDelay(FarmPestRules.GrowthDelaySeconds)) return 0;
            actionFeedback?.PlayPest(target.transform.position);
            return 1;
        }
        public int ApplyRainToTiles()
        {
            if (!FarmSessionTime.IsSimulationAuthority) return 0;
            var watered = 0;
            foreach (var tile in tiles)
                if (tile.ApplyRainWatering()) watered++;
            if (watered > 0) feedback = FarmLocalization.Format("weather.rain.watered", "Rain watered {0} plot tile(s).", watered);
            return watered;
        }

        /// <summary>Advances to the next morning and runs normal morning automation.</summary>
        public void AdvanceDayForDebug()
        {
            if (dayClock == null || gameState == null) return;
            dayClock.SetClock(gameState.DayNumber + 1, 360f);
            NotifyMorningStarted();
            feedback = FarmLocalization.Format("dev.day.skipped", "Developer: advanced to Day {0}.", gameState.DayNumber);
            QueueSave();
        }

        public void SetSeasonForDebug(FarmSeason season)
        {
            if (dayClock == null || gameState == null) return;
            var yearStart = ((gameState.DayNumber - 1) / FarmDayClock.DaysPerYear) * FarmDayClock.DaysPerYear + 1;
            var targetDay = yearStart + ((int)season * FarmDayClock.DaysPerSeason) + (dayClock.DayOfSeason - 1);
            dayClock.SetClock(targetDay, gameState.MinutesOfDay);
            weatherSystem?.Refresh();
            feedback = FarmLocalization.Format("dev.season.changed", "Developer: season set to {0}.", FarmDayClock.SeasonName(season));
            QueueSave();
        }

        public int TriggerPestVisitForDebug()
        {
            lastMorningPestAffected = ApplyPestVisit(out lastMorningPestProtected);
            feedback = FarmLocalization.Format("dev.pests.triggered", "Developer: pest visit tested. Affected {0}, protected {1}.", lastMorningPestAffected, lastMorningPestProtected);
            QueueSave();
            return lastMorningPestAffected;
        }

        public int WaterAllTilesForDebug()
        {
            var watered = 0;
            foreach (var tile in tiles)
                if (tile.ApplyRainWatering()) watered++;
            feedback = FarmLocalization.Format("dev.tiles.watered", "Developer: watered {0} tile(s).", watered);
            NotifyTileChanged();
            return watered;
        }

        public int AdvanceCropGrowthForDebug(float realSeconds)
        {
            var advanced = 0;
            foreach (var tile in tiles)
                if (tile.AdvanceGrowth(realSeconds)) advanced++;
            feedback = FarmLocalization.Format("dev.crops.advanced", "Developer: advanced {0} crop(s) by {1:0.#} seconds.", advanced, realSeconds);
            NotifyTileChanged();
            return advanced;
        }
        public void NotifyClockCheckpoint()
        {
            QueueSave();
        }
        public void NotifyTileChanged()
        {
            QueueSave();
            QueueWorldSnapshot();
        }

        private void QueueSave()
        {
            if (!FarmSessionTime.IsSimulationAuthority) return;
            saveQueued = true;
            saveAt = Time.unscaledTime + 0.35f;
        }

        private void QueueWorldSnapshot()
        {
            if (!FarmSessionTime.IsSimulationAuthority || WorldSnapshotReady == null) return;
            worldSnapshotQueued = true;
        }

        private List<FarmTileSaveData> CaptureTiles()
        {
            var result = new List<FarmTileSaveData>(tiles.Count);
            foreach (var tile in tiles) result.Add(tile.CaptureSaveData());
            return result;
        }

        /// <summary>Called by a future host transport after a confirmed world mutation.</summary>
        public FarmWorldSessionSnapshot CaptureWorldSessionSnapshot()
        {
            if (!FarmSessionTime.IsSimulationAuthority || gameState == null) return null;
            nextWorldSnapshotRevision = Mathf.Max(1, nextWorldSnapshotRevision + 1);
            return FarmWorldSessionSnapshot.Create(
                nextWorldSnapshotRevision,
                FarmSessionTime.Now,
                gameState.CreateSaveData(CaptureTiles()),
                sleepSession != null ? sleepSession.CaptureSnapshot() : null);
        }

        /// <summary>Called by a future peer transport after receiving a host snapshot.</summary>
        public bool ApplyWorldSessionSnapshot(FarmWorldSessionSnapshot snapshot)
        {
            if (FarmSessionTime.IsSimulationAuthority || snapshot == null || !snapshot.IsValid ||
                snapshot.Revision <= lastAppliedWorldSnapshotRevision) return false;
            var farmData = snapshot.CreateIndependentFarmCopy();
            if (farmData == null) return false;

            ApplyFarmState(farmData);
            sleepSession?.ApplySnapshot(snapshot.CreateIndependentSleepCopy());
            lastAppliedWorldSnapshotRevision = snapshot.Revision;
            feedback = FarmLocalization.Get("session.snapshot.applied", "Host world update applied.");
            return true;
        }

        private void ApplyFarmState(FarmSaveData data)
        {
            if (data == null || gameState == null) return;
            gameState.Restore(data);
            EnsureFarmTilesForLandLevel();
            if (hud != null) RebuildWorldPickups();
            weatherSystem?.Refresh();
            buildingSystem?.RebuildFromState();
            ApplySelectedHotbarEntry(false);
            if (data.Tiles != null)
            {
                foreach (var tileData in data.Tiles)
                    if (tileData.Index >= 0 && tileData.Index < tiles.Count) tiles[tileData.Index].Restore(tileData);
            }
            saveQueued = false;
        }

        private void SaveGame(bool showFeedback)
        {
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                saveQueued = false;
                if (showFeedback) feedback = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
                return;
            }
            saveQueued = false;
            var data = gameState.CreateSaveData(CaptureTiles());
            if (FarmSaveSystem.Save(data, out var error))
            {
                saveStatus = $"Saved at {DateTime.Now:HH:mm:ss}";
                if (showFeedback) feedback = "Game saved.";
            }
            else
            {
                saveStatus = "Save failed";
                feedback = $"Save error: {error}";
                Debug.LogError($"Farm save failed: {error}");
            }
        }

        private void LoadGame(bool showFeedback)
        {
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                if (showFeedback) feedback = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
                return;
            }
            if (!FarmSaveSystem.TryLoad(out var data, out var error))
            {
                if (!string.IsNullOrEmpty(error))
                {
                    feedback = $"Load error: {error}";
                    Debug.LogError($"Farm load failed: {error}");
                }
                else if (showFeedback)
                {
                    feedback = FarmLocalization.Get("save.none_found", "No save found.");
                }
                return;
            }

            ApplyFarmState(data);
            if (FarmSaveSystem.LastLoadUsedBackup)
            {
                saveStatus = "Save recovered from backup";
                if (showFeedback) feedback = "Primary save was damaged. Backup recovered.";
                Debug.Log("Farm save recovered from the automatic backup.");
            }
            else
            {
                saveStatus = data.Version < 19 ? "Save migrated to v19" : "Save loaded";
                if (showFeedback) feedback = "Game loaded.";
            }
        }

        private string HoverPrompt()
        {
            if (hoveredTile != null)
            {
                var range = IsInRange(hoveredTile.transform.position) ? string.Empty : FarmLocalization.Get("prompt.out_of_range", " (out of range)");
                return FarmLocalization.Format("prompt.tile", "Target: {0}{1}", hoveredTile.StatusText, range);
            }
            if (hoveredStation != null)
            {
                var range = IsStationInRange() ? string.Empty : FarmLocalization.Get("prompt.out_of_range", " (out of range)");
                return FarmLocalization.Format("prompt.market", "Market crate: click or press F to open commerce{0}", range);
            }
            if (hoveredStorage != null)
            {
                var range = IsStorageInRange() ? string.Empty : FarmLocalization.Get("prompt.out_of_range", " (out of range)");
                return FarmLocalization.Format("prompt.storage", "Storage: click or press F to manage items{0}", range);
            }
            if (hoveredSleep != null)
            {
                var range = IsSleepInRange() ? string.Empty : FarmLocalization.Get("prompt.out_of_range", " (out of range)");
                return FarmLocalization.Format("prompt.bed", "Bed: click or press F to plan the evening or end the day{0}", range);
            }
            if (hoveredOrderBoard != null)
            {
                var range = IsOrderBoardInRange() ? string.Empty : FarmLocalization.Get("prompt.out_of_range", " (out of range)");
                return FarmLocalization.Format("prompt.orders", "Order board: click or press F to view deliveries{0}", range);
            }
            if (MailboxVisible)
            {
                var unread = mailboxSystem != null ? mailboxSystem.UnreadCount : 0;
                return unread > 0
                    ? FarmLocalization.Format("prompt.mailbox.unread", "Mailbox in range: press F. {0} new letter(s).", unread)
                    : FarmLocalization.Get("prompt.mailbox", "Mailbox in range: press F to view the schedule.");
            }
            if (OrderBoardVisible) return FarmLocalization.Get("prompt.orders.nearby", "Order board in range: press F to open daily orders.");
            if (SleepVisible) return FarmLocalization.Get("prompt.bed.nearby", "Bed in range: press F to plan the evening or rest.");
            if (StorageVisible) return FarmLocalization.Get("prompt.storage.nearby", "Storage in range: press F to open.");
            return ShopInRange
                ? FarmLocalization.Get("prompt.market.nearby", "Market crate in range: press F to open commerce.")
                : FarmLocalization.Get("prompt.default", "Hover a plot tile or move closer to a station.");
        }
        private static string ToolAreaText(FarmTool tool, int level)
        {
            if (!FarmGameState.IsUpgradeableTool(tool)) return FarmLocalization.Get("tool.area.one", "1 plot tile");
            return level switch
            {
                1 => FarmLocalization.Get("tool.area.one", "1 plot tile"),
                2 => FarmLocalization.Get("tool.area.line", "3-tile line"),
                _ => FarmLocalization.Get("tool.area.square", "3x3 area")
            };
        }

        private static string ToolName(FarmTool tool) => tool switch
        {
            FarmTool.None => FarmLocalization.Get("tool.none", "Empty"),
            FarmTool.Hoe => FarmLocalization.Get("tool.hoe", "Hoe"),
            FarmTool.Seeds => FarmLocalization.Get("tool.seeds", "Seeds"),
            FarmTool.WateringCan => FarmLocalization.Get("tool.watering_can", "Watering Can"),
            FarmTool.Harvest => FarmLocalization.Get("tool.harvest", "Harvest"),
            FarmTool.Axe => FarmLocalization.Get("tool.axe", "Axe"),
            FarmTool.Pickaxe => FarmLocalization.Get("tool.pickaxe", "Pickaxe"),
            FarmTool.Fertilizer => FarmLocalization.Get("tool.compost", "Compost"),
            _ => "-"
        };
    }

    public sealed class FarmOrderBoardStation : MonoBehaviour
    {
        private Vector3 normalScale;
        public void Initialize() => normalScale = transform.localScale;
        public void SetHovered(bool hovered) => transform.localScale = normalScale * (hovered ? 1.05f : 1f);
    }

    public sealed class FarmSleepStation : MonoBehaviour
    {
        private Vector3 normalScale;
        public void Initialize() => normalScale = transform.localScale;
        public void SetHovered(bool hovered) => transform.localScale = normalScale * (hovered ? 1.05f : 1f);
    }

    public sealed class FarmStorageStation : MonoBehaviour
    {
        private Vector3 normalScale;
        public void Initialize() => normalScale = transform.localScale;
        public void SetHovered(bool hovered) => transform.localScale = normalScale * (hovered ? 1.08f : 1f);
    }
    public sealed class FarmSellStation : MonoBehaviour
    {
        private Vector3 normalScale;
        public void Initialize() => normalScale = transform.localScale;
        public void SetHovered(bool hovered) => transform.localScale = normalScale * (hovered ? 1.08f : 1f);
    }

    public sealed class FarmTestTile : MonoBehaviour
    {
        private enum State { Untilled, Tilled, Seeded, Watered, Growing, Ready }

        private static readonly Color UntilledColor = new(0.33f, 0.48f, 0.18f);
        private static readonly Color TilledColor = new(0.28f, 0.13f, 0.05f);
        private static readonly Color SeededColor = new(0.40f, 0.22f, 0.08f);
        private static readonly Color WateredColor = new(0.12f, 0.25f, 0.18f);
        private static readonly Color ReadyColor = new(0.22f, 0.42f, 0.10f);

        private FarmTestPlot plot;
        private CropDefinition cropDefinition;
        private Material tileMaterial;
        private State state;
        private int index;
        private float middleAt;
        private float readyAt;
        private GameObject cropVisual;
        private Color normalColor;
        private bool hovered;
        private bool fertilized;
        private string lastHarvestedCropId = string.Empty;
        private bool rotated;

        public int Index => index;
        public string CropId => cropDefinition != null ? cropDefinition.Id : string.Empty;
        public float GrowthSeconds => cropDefinition != null ? cropDefinition.GrowthSeconds : 0f;
        public bool CanReceivePestDelay => state is State.Watered or State.Growing;
        public bool IsFertilized => fertilized;
        public bool IsRotated => rotated;

        public string StatusText => (state switch
        {
            State.Untilled => FarmLocalization.Get("tile.status.untilled", "unprepared soil"),
            State.Tilled => FarmLocalization.Get("tile.status.tilled", "prepared soil — plant"),
            State.Seeded => FarmLocalization.Get("tile.status.seeded", "seed planted — water"),
            State.Watered => FarmLocalization.Format("tile.status.watered", "watered — {0:0}s", SecondsRemaining),
            State.Growing => FarmLocalization.Format("tile.status.growing", "growing — {0:0}s", SecondsRemaining),
            State.Ready => FarmLocalization.Format("tile.status.ready", "{0} ready — harvest", cropDefinition != null ? cropDefinition.LocalizedName : FarmLocalization.Get("tile.status.crop", "crop")),
            _ => FarmLocalization.Get("tile.status.default", "plot tile")
        }) + (rotated ? FarmLocalization.Get("tile.status.rotated_suffix", "  •  rotated soil") : string.Empty);

        private float SecondsRemaining => Mathf.Max(0f, readyAt - FarmSessionTime.Now);

        public void Initialize(FarmTestPlot owner, int tileIndex, CropDefinition definition)
        {
            plot = owner;
            index = tileIndex;
            cropDefinition = definition;
            tileMaterial = GetComponent<Renderer>().material;
            if (definition.SmallModel == null || definition.MediumModel == null || definition.LargeModel == null)
                Debug.LogError($"A cultura '{definition.Id}' possui modelos de crescimento ausentes.");
            RefreshVisual();
        }

        private void Update()
        {
            if (!FarmSessionTime.IsSimulationAuthority) return;
            if (state == State.Watered && FarmSessionTime.Now >= middleAt)
            {
                state = State.Growing;
                RefreshVisual();
                plot.NotifyTileChanged();
            }
            else if (state == State.Growing && FarmSessionTime.Now >= readyAt)
            {
                state = State.Ready;
                RefreshVisual();
                plot.NotifyTileChanged();
            }
        }

        public bool ApplyConfirmedPreparation()
        {
            if (state != State.Untilled) return false;
            state = State.Tilled;
            plot.GameState?.RecordJournal(FarmJournalMetric.Tilled);
            RefreshVisual();
            plot.NotifyTileChanged();
            plot.MarkMilestone(FarmMilestone.Tilled);
            return true;
        }
        public bool ApplyConfirmedPlant(CropDefinition confirmedCrop)
        {
            if (state != State.Tilled || confirmedCrop == null) return false;
            cropDefinition = confirmedCrop;
            rotated = FarmSoilRules.IsRotation(lastHarvestedCropId, confirmedCrop.Id);
            state = State.Seeded;
            plot.GameState?.RecordJournal(FarmJournalMetric.Planted, 1, cropDefinition.Id);
            RefreshVisual();
            plot.NotifyTileChanged();
            plot.MarkMilestone(FarmMilestone.Planted);
            return true;
        }
        public bool ApplyConfirmedWatering()
        {
            if (state != State.Seeded || cropDefinition == null) return false;
            state = State.Watered;
            plot.GameState?.RecordJournal(FarmJournalMetric.Watered);
            middleAt = FarmSessionTime.Now + (cropDefinition.GrowthSeconds * 0.5f);
            readyAt = FarmSessionTime.Now + cropDefinition.GrowthSeconds;
            RefreshVisual();
            plot.NotifyTileChanged();
            plot.MarkMilestone(FarmMilestone.Watered);
            return true;
        }
        public bool ApplyConfirmedFertilization()
        {
            if (fertilized || state is State.Untilled or State.Ready) return false;
            fertilized = true;
            RefreshVisual();
            plot.NotifyTileChanged();
            return true;
        }

        public bool ApplyConfirmedHarvest(FarmHarvestSnapshot confirmedHarvest, out string message)
        {
            message = string.Empty;
            if (state != State.Ready || cropDefinition == null || confirmedHarvest == null ||
                !string.Equals(cropDefinition.Id, confirmedHarvest.CropId, StringComparison.OrdinalIgnoreCase) ||
                confirmedHarvest.Yield <= 0) return false;

            var harvestedCrop = cropDefinition;
            var wasFertilized = fertilized;
            var wasRotated = rotated;
            var season = plot.EffectiveCropSeason(harvestedCrop, transform.position);
            plot.GameState?.RecordJournal(FarmJournalMetric.HarvestedUnits, confirmedHarvest.Yield, harvestedCrop.Id);
            if (confirmedHarvest.Replanted)
            {
                state = State.Seeded;
                plot.GameState?.RecordJournal(FarmJournalMetric.Planted, 1, harvestedCrop.Id, false);
            }
            else
            {
                state = State.Tilled;
                cropDefinition = null;
            }
            fertilized = false;
            lastHarvestedCropId = harvestedCrop.Id;
            rotated = false;

            RefreshVisual();
            plot.NotifyTileChanged();
            plot.MarkMilestone(FarmMilestone.Harvested);
            var seasonalBonus = confirmedHarvest.Yield - harvestedCrop.HarvestYield;
            message = seasonalBonus > 0
                ? FarmLocalization.Format("tile.harvest.affinity", "Harvested {0} {1}, including +{2} from {3} affinity.", confirmedHarvest.Yield, harvestedCrop.LocalizedName, seasonalBonus, FarmDayClock.SeasonName(season))
                : FarmLocalization.Format("tile.harvest.success", "Harvested {0} {1}.", confirmedHarvest.Yield, harvestedCrop.LocalizedName);
            message += FarmLocalization.Format("tile.harvest.quality", " Quality: {0}.", FarmItemQualityRules.DisplayName(confirmedHarvest.Quality));
            if (confirmedHarvest.Replanted) message += FarmLocalization.Get("tile.harvest.replanted", " Continuous cycle: one seed was replanted.");
            if (wasFertilized || confirmedHarvest.WasFertilized) message += FarmLocalization.Get("tile.harvest.compost_bonus", " Compost increased this harvest.");
            if (wasRotated || confirmedHarvest.WasRotated) message += FarmLocalization.Get("tile.harvest.rotation_bonus", " Crop rotation improved its quality.");
            return true;
        }
        public string Use(FarmTool tool, FarmGameState inventory, string selectedItemId)
        {
            if (tool == FarmTool.Hoe && state == State.Untilled)
            {
                state = State.Tilled;
                inventory.RecordJournal(FarmJournalMetric.Tilled);
                RefreshVisual();
                plot.NotifyTileChanged();
                plot.MarkMilestone(FarmMilestone.Tilled);
                return FarmLocalization.Get("tile.soil.prepared", "Soil prepared. Select seeds (2).");
            }

            if (tool == FarmTool.Seeds && state == State.Tilled)
            {
                var selectedCrop = FarmContentDatabase.GetCropForSeed(selectedItemId);
                if (selectedCrop == null)
                    return FarmLocalization.Get("tile.plant.select_seed", "Select a valid seed in the hotbar.");
                var currentSeason = plot.DayClock != null ? plot.DayClock.CurrentSeason : FarmSeason.Spring;
                if (currentSeason != selectedCrop.PreferredSeason && !plot.IsGreenhouseClimateAt(transform.position))
                    return FarmLocalization.Format("backend.plant.out_of_season", "{0} can only be planted in {1}, unless this tile is covered by a greenhouse.", selectedCrop.LocalizedName, FarmDayClock.SeasonName(selectedCrop.PreferredSeason));
                if (!inventory.TryRemoveItem(selectedCrop.SeedItem.Id, 1))
                    return FarmLocalization.Format("tile.plant.seed_missing", "You do not have {0}.", selectedCrop.SeedItem.LocalizedName);
                cropDefinition = selectedCrop;
                rotated = FarmSoilRules.IsRotation(lastHarvestedCropId, selectedCrop.Id);
                state = State.Seeded;
                inventory.RecordJournal(FarmJournalMetric.Planted, 1, cropDefinition.Id);
                RefreshVisual();
                plot.NotifyTileChanged();
                plot.MarkMilestone(FarmMilestone.Planted);
                return FarmLocalization.Get("tile.plant.confirmed", "One seed was used. Water it now (3).");
            }

            if (tool == FarmTool.WateringCan && state == State.Seeded)
            {
                state = State.Watered;
                inventory.RecordJournal(FarmJournalMetric.Watered);
                middleAt = FarmSessionTime.Now + (cropDefinition.GrowthSeconds * 0.5f);
                readyAt = FarmSessionTime.Now + cropDefinition.GrowthSeconds;
                RefreshVisual();
                plot.NotifyTileChanged();
                plot.MarkMilestone(FarmMilestone.Watered);
                return FarmLocalization.Format("tile.watered", "Watered! Ready in {0:0} seconds.", cropDefinition.GrowthSeconds);
            }

            if (tool == FarmTool.Fertilizer && state is not (State.Untilled or State.Ready))
            {
                if (fertilized) return FarmLocalization.Get("backend.fertilize.already", "This plot tile is already enriched.");
                var fertilizer = FarmContentDatabase.GetItem(selectedItemId);
                if (fertilizer == null || fertilizer.Category != ItemCategory.Fertilizer)
                    return FarmLocalization.Get("backend.fertilize.invalid", "Select compost in the hotbar.");
                if (!inventory.TryRemoveItem(fertilizer.Id, 1))
                    return FarmLocalization.Format("backend.fertilize.missing", "You do not have {0}.", fertilizer.LocalizedName);
                fertilized = true;
                RefreshVisual();
                plot.NotifyTileChanged();
                return FarmLocalization.Get("backend.fertilize.confirmed", "Compost confirmed by server. This crop will yield +1 and gain quality.");
            }

            if (tool == FarmTool.Harvest && state == State.Ready)
            {
                var season = plot.EffectiveCropSeason(cropDefinition, transform.position);
                var wasFertilized = fertilized;
                var wasRotated = rotated;
                var harvestYield = cropDefinition.HarvestYieldForSeason(season) + (wasFertilized ? 1 : 0);
                var harvestQuality = FarmItemQualityRules.EvaluateHarvest(
                    cropDefinition, season, inventory.GetMasteryLevel(FarmMasterySkill.Harvesting), wasFertilized, wasRotated);
                var sentToStorage = false;
                if (!inventory.AddItem(cropDefinition.HarvestItem.Id, harvestYield, harvestQuality))
                {
                    if (inventory.GetMasteryLevel(FarmMasterySkill.Harvesting) < 3 ||
                        !inventory.TryAddToStorage(cropDefinition.HarvestItem.Id, harvestYield, harvestQuality))
                        return FarmLocalization.Get("tile.harvest.inventory_full", "Inventory full. Make room before harvesting.");
                    sentToStorage = true;
                }
                inventory.RecordJournal(FarmJournalMetric.HarvestedUnits, harvestYield, cropDefinition.Id);
                var replanted = inventory.GetMasteryLevel(FarmMasterySkill.Cultivation) >= 3 &&
                    cropDefinition.SeedItem != null && inventory.TryRemoveItem(cropDefinition.SeedItem.Id, 1);
                state = replanted ? State.Seeded : State.Tilled;
                fertilized = false;
                lastHarvestedCropId = cropDefinition.Id;
                rotated = false;
                if (replanted) inventory.RecordJournal(FarmJournalMetric.Planted, 1, cropDefinition.Id, false);
                RefreshVisual();
                plot.NotifyTileChanged();
                plot.MarkMilestone(FarmMilestone.Harvested);
                var seasonalBonus = harvestYield - cropDefinition.HarvestYield;
                var message = seasonalBonus > 0
                    ? FarmLocalization.Format("tile.harvest.affinity", "Harvested {0} {1}, including +{2} from {3} affinity.", harvestYield, cropDefinition.LocalizedName, seasonalBonus, FarmDayClock.SeasonName(season))
                    : FarmLocalization.Format("tile.harvest.success", "Harvested {0} {1}.", harvestYield, cropDefinition.LocalizedName);
                message += FarmLocalization.Format("tile.harvest.quality", " Quality: {0}.", FarmItemQualityRules.DisplayName(harvestQuality));
                if (sentToStorage) message += FarmLocalization.Get("tile.harvest.storage", " Support basket: harvest sent to storage.");
                if (replanted) message += FarmLocalization.Get("tile.harvest.replanted", " Continuous cycle: one seed was replanted.");
                if (wasFertilized) message += FarmLocalization.Get("tile.harvest.compost_bonus", " Compost increased this harvest.");
                if (wasRotated) message += FarmLocalization.Get("tile.harvest.rotation_bonus", " Crop rotation improved its quality.");
                return message;
            }

            return tool switch
            {
                FarmTool.Hoe => FarmLocalization.Get("tile.invalid.hoe", "This plot tile is already prepared."),
                FarmTool.Seeds => FarmLocalization.Get("tile.invalid.seeds", "Seeds can only be used in prepared soil."),
                FarmTool.WateringCan => FarmLocalization.Get("tile.invalid.water", "Plant a seed before watering."),
                FarmTool.Harvest => FarmLocalization.Get("tile.invalid.harvest", "The crop is not ready yet."),
                FarmTool.Fertilizer => FarmLocalization.Get("backend.fertilize.requires_active_crop", "Compost can only enrich prepared soil or a crop before it is ready."),
                _ => FarmLocalization.Get("tile.invalid.action", "Action unavailable.")
            };
        }
        public bool ApplyPestDelay(float realSeconds)
        {
            if (!CanReceivePestDelay || realSeconds <= 0f) return false;
            readyAt += realSeconds;
            if (state == State.Watered) middleAt += realSeconds;
            RefreshVisual();
            plot.NotifyTileChanged();
            return true;
        }
        public bool ApplyAutomaticWatering() => ApplyRainWatering();

        public bool ApplyRainWatering()
        {
            if (state != State.Seeded) return false;
            state = State.Watered;
            plot.GameState?.RecordJournal(FarmJournalMetric.Watered);
            middleAt = FarmSessionTime.Now + (cropDefinition.GrowthSeconds * 0.5f);
            readyAt = FarmSessionTime.Now + cropDefinition.GrowthSeconds;
            RefreshVisual();
            plot.NotifyTileChanged();
            plot.MarkMilestone(FarmMilestone.Watered);
            return true;
        }
        public bool AdvanceGrowth(float realSeconds)
        {
            if (state is not (State.Watered or State.Growing) || realSeconds <= 0f) return false;
            var remaining = SecondsRemaining - realSeconds;
            if (remaining <= 0f)
            {
                state = State.Ready;
            }
            else
            {
                readyAt = FarmSessionTime.Now + remaining;
                var halfwayRemaining = cropDefinition.GrowthSeconds * 0.5f;
                if (remaining > halfwayRemaining)
                {
                    state = State.Watered;
                    middleAt = FarmSessionTime.Now + (remaining - halfwayRemaining);
                }
                else
                {
                    state = State.Growing;
                    middleAt = FarmSessionTime.Now;
                }
            }
            RefreshVisual();
            plot.NotifyTileChanged();
            return true;
        }

        public FarmTileSaveData CaptureSaveData() => new()
        {
            Index = index,
            State = (int)state,
            CropId = cropDefinition != null ? cropDefinition.Id : string.Empty,
            GrowthSecondsRemaining = state is State.Watered or State.Growing ? SecondsRemaining : 0f,
            Fertilized = fertilized,
            LastHarvestedCropId = lastHarvestedCropId,
            Rotated = rotated
        };

        public void Restore(FarmTileSaveData data)
        {
            var savedCrop = FarmContentDatabase.GetCrop(data.CropId);
            if (savedCrop != null) cropDefinition = savedCrop;
            var maxState = Enum.GetValues(typeof(State)).Length - 1;
            state = (State)Mathf.Clamp(data.State, 0, maxState);
            // A crop can be saved after becoming ready but before the player harvests;
            // its pending soil benefits must survive that save/load boundary.
            fertilized = data.Fertilized && state is not State.Untilled;
            lastHarvestedCropId = data.LastHarvestedCropId ?? string.Empty;
            rotated = data.Rotated && state is not State.Untilled;
            if (state is State.Watered or State.Growing)
            {
                if (data.GrowthSecondsRemaining <= 0f)
                {
                    state = State.Ready;
                }
                else
                {
                    readyAt = FarmSessionTime.Now + data.GrowthSecondsRemaining;
                    var halfwayRemaining = cropDefinition.GrowthSeconds * 0.5f;
                    if (data.GrowthSecondsRemaining > halfwayRemaining)
                    {
                        state = State.Watered;
                        middleAt = FarmSessionTime.Now + (data.GrowthSecondsRemaining - halfwayRemaining);
                    }
                    else
                    {
                        state = State.Growing;
                        middleAt = FarmSessionTime.Now;
                    }
                }
            }
            RefreshVisual();
        }

        public void SetHovered(bool value)
        {
            hovered = value;
            ApplyTileColor();
        }

        private void RefreshVisual()
        {
            normalColor = state switch
            {
                State.Untilled => UntilledColor,
                State.Tilled => TilledColor,
                State.Seeded => SeededColor,
                State.Watered => WateredColor,
                State.Growing => WateredColor,
                State.Ready => ReadyColor,
                _ => UntilledColor
            };
            if (fertilized) normalColor = Color.Lerp(normalColor, new Color(0.35f, 0.58f, 0.19f), 0.36f);
            if (rotated) normalColor = Color.Lerp(normalColor, new Color(0.52f, 0.42f, 0.18f), 0.22f);
            ApplyTileColor();

            if (cropVisual != null) Destroy(cropVisual);
            cropVisual = null;
            var model = state switch
            {
                State.Seeded or State.Watered => cropDefinition.SmallModel,
                State.Growing => cropDefinition.MediumModel,
                State.Ready => cropDefinition.LargeModel,
                _ => null
            };
            if (model == null) return;

            cropVisual = Instantiate(model);
            cropVisual.name = $"{cropDefinition.Id}_{state}_{index}";
            cropVisual.transform.position = transform.position + (Vector3.up * 0.18f);
            cropVisual.transform.rotation = Quaternion.identity;
            cropVisual.transform.localScale = Vector3.one * 0.75f;
            cropVisual.transform.SetParent(transform, true);
            foreach (var cropCollider in cropVisual.GetComponentsInChildren<Collider>()) Destroy(cropCollider);
        }

        private void ApplyTileColor()
        {
            if (tileMaterial == null) return;
            tileMaterial.color = hovered ? Color.Lerp(normalColor, Color.white, 0.32f) : normalColor;
        }
    }
}
