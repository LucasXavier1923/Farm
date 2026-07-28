using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FarmPrototype.Farming
{
    public sealed class FarmHudController : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.06f, 0.08f, 0.055f, 0.92f);
        private static readonly Color AccentColor = new(0.95f, 0.67f, 0.18f, 1f);
        private static readonly Color SlotColor = new(0.12f, 0.15f, 0.10f, 0.96f);
        private static readonly Color SelectedSlotColor = new(0.42f, 0.27f, 0.06f, 1f);
        private static readonly Color EmptySlotColor = new(0.08f, 0.10f, 0.075f, 0.82f);

        private FarmTestPlot plot;
        private Font font;
        private Canvas canvas;
        private Text resourcesText;
        private Text toolText;
        private Text promptText;
        private Text feedbackText;
        private Text saveText;
        private Text tutorialText;
        private Text homesteadText;
        private Text clockText;
        private Text calendarText;
        private Text weatherText;
        private Text pestText;
        private Image energyFill;
        private Text energyText;
        private Text inventorySummaryText;
        private Text shopInfoText;
        private GameObject shopPanel;
        private bool shopOpen;
        private Button sellButton;
        private Button buyButton;
        private Button previousCropButton;
        private Button nextCropButton;
        private Button upgradeToolButton;
        private Text upgradeToolButtonText;
        private Button landUpgradeButton;
        private Text landUpgradeButtonText;
        private GameObject inventoryWindow;
        private CanvasGroup inventoryGroup;
        private Text inventoryCapacityText;
        private readonly GameObject[] inventorySlots = new GameObject[20];
        private readonly Image[] inventorySlotBackgrounds = new Image[20];
        private readonly Text[] inventoryIcons = new Text[20];
        private readonly Text[] inventoryNames = new Text[20];
        private readonly Text[] inventoryCounts = new Text[20];
        private readonly Button[] inventoryFilterButtons = new Button[5];
        private readonly Image[] inventoryFilterBackgrounds = new Image[5];
        private FarmCollectionCategory inventoryFilter = FarmCollectionCategory.All;
        private int inventoryVisibleItemCount;
        private readonly Image[] hotbarSlots = new Image[FarmGameState.HotbarSlotCount];
        private readonly Text[] hotbarIcons = new Text[FarmGameState.HotbarSlotCount];
        private readonly Text[] hotbarLabels = new Text[FarmGameState.HotbarSlotCount];
        private readonly Text[] hotbarCounts = new Text[FarmGameState.HotbarSlotCount];
        private GameObject storageWindow;
        private CanvasGroup storageGroup;
        private Text storageBackpackCapacityText;
        private Text storageChestCapacityText;
        private Text storageFeedbackText;
        private readonly Image[] storageBackpackBackgrounds = new Image[20];
        private readonly Text[] storageBackpackIcons = new Text[20];
        private readonly Text[] storageBackpackNames = new Text[20];
        private readonly Text[] storageBackpackCounts = new Text[20];
        private readonly FarmStorageSlotView[] storageBackpackViews = new FarmStorageSlotView[20];
        private readonly Image[] storageChestBackgrounds = new Image[30];
        private readonly Text[] storageChestIcons = new Text[30];
        private readonly Text[] storageChestNames = new Text[30];
        private readonly Text[] storageChestCounts = new Text[30];
        private readonly FarmStorageSlotView[] storageChestViews = new FarmStorageSlotView[30];
        private GameObject dragGhost;
        private Text dragGhostIcon;
        private Text dragGhostLabel;
        private string draggedItemId;
        private FarmItemQuality draggedItemQuality;
        private bool draggedFromStorage;
        private int draggedHotbarIndex = -1;
        private bool hotbarCommandPending;
        private int hoveredHotbarDropIndex = -1;
        private GameObject itemTooltip;
        private CanvasGroup itemTooltipGroup;
        private Text itemTooltipTitle;
        private Text itemTooltipBody;
        private string tooltipItemId;
        private string tooltipContext;
        private bool inventoryOpen;
        private bool storageOpen;
        private GameObject journalWindow;
        private CanvasGroup journalGroup;
        private bool journalOpen;
        private readonly Text[] journalQuestTexts = new Text[5];
        private readonly Button[] journalClaimButtons = new Button[5];
        private readonly Text[] journalClaimButtonTexts = new Text[5];
        private GameObject pickupToast;
        private CanvasGroup pickupToastGroup;
        private Text pickupToastText;
        private float pickupToastUntil;
        private GameObject sleepConfirmationWindow;
        private CanvasGroup sleepConfirmationGroup;
        private Text sleepDescriptionText;
        private bool sleepConfirmationOpen;
        private CanvasGroup dayTransitionGroup;
        private Text dayTransitionText;
        private Coroutine dayTransitionRoutine;
        private GameObject dailyOrdersWindow;
        private CanvasGroup dailyOrdersGroup;
        private bool dailyOrdersOpen;
        private bool settingsOpen;
        private bool masteryOpen;
        private bool economyLedgerOpen;
        private bool craftingOpen;
        private bool buildingCatalogOpen;
        private bool mailboxOpen;
        private bool collectionOpen;
        private bool seasonalPlannerOpen;
        private Text dailyOrdersSummaryText;
        private readonly Text[] dailyOrderTexts = new Text[FarmDailyOrderGenerator.OrderCount];
        private readonly Button[] dailyOrderButtons = new Button[FarmDailyOrderGenerator.OrderCount];
        private readonly Text[] dailyOrderButtonTexts = new Text[FarmDailyOrderGenerator.OrderCount];

        public static bool IsModalOpen { get; private set; }
        public bool IsInventoryOpen => inventoryOpen;
        public bool IsStorageOpen => storageOpen;
        public bool IsJournalOpen => journalOpen;
        public bool IsSleepConfirmationOpen => sleepConfirmationOpen;
        public bool IsDailyOrdersOpen => dailyOrdersOpen;
        public bool IsSettingsOpen => settingsOpen;
        public bool IsMasteryOpen => masteryOpen;
        public bool IsEconomyLedgerOpen => economyLedgerOpen;
        public bool IsCraftingOpen => craftingOpen;
        public bool IsBuildingCatalogOpen => buildingCatalogOpen;
        public bool IsMailboxOpen => mailboxOpen;
        public bool IsCollectionOpen => collectionOpen;
        public bool IsSeasonalPlannerOpen => seasonalPlannerOpen;
        public bool IsShopOpen => shopOpen;
        public float DayTransitionAlpha => dayTransitionGroup != null ? dayTransitionGroup.alpha : 0f;
        public string DayTransitionText => dayTransitionText != null ? dayTransitionText.text : string.Empty;
        public string DraggedItemId => draggedItemId;
        public string CurrentToastText => pickupToastText != null ? pickupToastText.text : string.Empty;
        public bool IsItemTooltipVisible => itemTooltipGroup != null && itemTooltipGroup.alpha > 0.5f;
        public string TooltipItemId => tooltipItemId ?? string.Empty;
        public string TooltipText => itemTooltipTitle == null || itemTooltipBody == null
            ? string.Empty
            : itemTooltipTitle.text + "\n" + itemTooltipBody.text;
        public FarmCollectionCategory InventoryFilter => inventoryFilter;
        public int InventoryVisibleItemCount => inventoryVisibleItemCount;

        public void Initialize(FarmTestPlot owner)
        {
            plot = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateInterface();
        }

        private void OnDisable()
        {
            IsModalOpen = false;
        }

        private void OnDestroy()
        {
            IsModalOpen = false;
        }

        private static string SessionRoleText() => FarmSessionTime.Role switch
        {
            FarmSessionRole.Host => FarmLocalization.Get("session.role.host", "HOST"),
            FarmSessionRole.Peer => FarmLocalization.Get("session.role.peer", "PEER"),
            _ => FarmLocalization.Get("session.role.solo", "SOLO")
        };

        private void Update()
        {
            HandleInventoryInput();
            UpdatePickupToast();
            if (sleepConfirmationOpen && sleepDescriptionText != null && plot != null)
                sleepDescriptionText.text = $"{plot.SleepSessionStatus}\n{plot.EveningPreparationStatus}";
            if (plot == null) plot = GetComponent<FarmTestPlot>();
            if (plot == null || plot.GameState == null || plot.ActiveCrop == null || !InterfaceReady()) return;

            var state = plot.GameState;
            var crop = plot.ActiveCrop;
            var shopCrop = plot.ShopCrop ?? crop;
            resourcesText.text = FarmLocalization.Format("hud.resources", "${0}    Seeds: {1}    {2}: {3}", state.Money, state.GetQuantity(crop.SeedItem.Id), crop.LocalizedName, state.GetQuantity(crop.HarvestItem.Id));
            energyFill.fillAmount = state.EnergyRatio;
            energyFill.color = state.IsExhausted ? new Color(0.86f, 0.25f, 0.20f)
                : state.Energy <= 25 ? new Color(0.96f, 0.56f, 0.16f)
                : new Color(0.30f, 0.78f, 0.38f);
            var comfortSuffix = state.ComfortCharges > 0
                ? FarmLocalization.Format("hud.energy.comfort", "  COMFORT {0}", state.ComfortCharges)
                : string.Empty;
            energyText.text = (state.IsExhausted
                ? FarmLocalization.Format("hud.energy.exhausted", "ENERGY  {0}/{1}  •  TIRED: reduced area", state.Energy, FarmGameState.MaxEnergy)
                : FarmLocalization.Format("hud.energy.normal", "ENERGY  {0}/{1}", state.Energy, FarmGameState.MaxEnergy)) + comfortSuffix;
            toolText.text = FarmLocalization.Format("hud.tool.selected", "Selected: {0}", plot.ActiveToolDisplayName);
            promptText.text = inventoryOpen
                ? FarmLocalization.Get("hud.inventory.open_hint", "Inventory open — drag items to the hotbar")
                : plot.CurrentPrompt;
            feedbackText.text = plot.Feedback;
            saveText.text = FarmLocalization.Format(
                "hud.controls",
                "F interact  •  I/Tab inventory  •  J journal  •  K mastery  •  F5 save  •  F9 load  •  SESSION: {0}    {1}",
                SessionRoleText(),
                plot.SaveStatus);
            tutorialText.text = FarmFirstWeekProgress.BuildHudText(state);
            homesteadText.text = BuildHomesteadGuide(state);
            if (plot.DayClock != null)
            {
                clockText.text = plot.DayClock.DisplayText;
                clockText.color = plot.DayClock.Phase is FarmDayPhase.Night or FarmDayPhase.Dawn ? new Color(0.62f, 0.76f, 1f) : AccentColor;
                calendarText.text = plot.DayClock.CalendarText;
                calendarText.color = plot.DayClock.CurrentSeason switch
                {
                    FarmSeason.Spring => new Color(0.58f, 0.88f, 0.48f),
                    FarmSeason.Summer => new Color(1f, 0.78f, 0.28f),
                    FarmSeason.Autumn => new Color(0.94f, 0.48f, 0.20f),
                    FarmSeason.Winter => new Color(0.58f, 0.78f, 1f),
                    _ => Color.white
                };
            }
            if (plot.WeatherSystem != null)
            {
                weatherText.text = plot.WeatherSystem.DisplayText;
                weatherText.color = plot.WeatherSystem.CurrentWeather switch
                {
                    FarmWeather.Rain => new Color(0.50f, 0.72f, 1f),
                    FarmWeather.Cloudy => new Color(0.78f, 0.82f, 0.84f),
                    _ => new Color(1f, 0.82f, 0.34f)
                };

                if (pestText != null)
                {
                    pestText.text = plot.PestForecastText;
                    pestText.color = plot.PestThreatToday ? new Color(1f, 0.46f, 0.24f)
                        : FarmPestRules.IsVisitDay(plot.GameState.DayNumber + 1) ? new Color(1f, 0.72f, 0.22f)
                        : new Color(0.64f, 0.84f, 0.58f);
                }            }
            inventorySummaryText.text = BuildInventorySummary(state);
            RefreshInventory(state);
            RefreshHotbar(state);
            RefreshStorage(state);
            RefreshJournal(state);
            RefreshDailyOrders(state);

            shopPanel.SetActive(shopOpen);
            if (shopOpen)
            {
                var todayQuote = state.GetMarketQuote(shopCrop.HarvestItem.Id);
                var tomorrowQuote = state.GetMarketQuote(shopCrop.HarvestItem.Id, 1);
                var normalPrice = state.GetMarketUnitPrice(shopCrop.HarvestItem.Id, FarmItemQuality.Normal);
                var silverPrice = state.GetMarketUnitPrice(shopCrop.HarvestItem.Id, FarmItemQuality.Silver);
                var goldPrice = state.GetMarketUnitPrice(shopCrop.HarvestItem.Id, FarmItemQuality.Gold);
                shopInfoText.text = FarmLocalization.Format(
                    "hud.shop.details",
                    "{0}\n{1}\nMarket today: {2}  ?  Tomorrow: {3}\nSell N/S/G: ${4} / ${5} / ${6}  ?  {7} seeds: ${8}",
                    $"{shopCrop.LocalizedName.ToUpperInvariant()}  ({plot.ShopCropIndex + 1}/{plot.ShopCropCount})",
                    shopCrop.AffinityText,
                    todayQuote.CompactText,
                    tomorrowQuote.CompactText,
                    normalPrice,
                    silverPrice,
                    goldPrice,
                    FarmEconomyRules.SeedPackAmount(shopCrop),
                    FarmEconomyRules.SeedPackPrice(shopCrop));
                sellButton.interactable = plot.ShopInRange;
                buyButton.interactable = plot.ShopInRange;
                previousCropButton.interactable = plot.ShopCropCount > 1;
                nextCropButton.interactable = plot.ShopCropCount > 1;
                var upgradeable = FarmGameState.IsUpgradeableTool(plot.ActiveTool);
                var maxed = upgradeable && plot.ActiveToolLevel >= FarmGameState.MaxToolLevel;
                var upgradeCost = plot.ActiveToolUpgradeCost;
                var requiredSkill = FarmGameState.MasteryForTool(plot.ActiveTool);
                var requiredLevel = state.RequiredMasteryLevelForNextToolUpgrade(plot.ActiveTool);
                var progressionLocked = upgradeable && !maxed && state.GetMasteryLevel(requiredSkill) < requiredLevel;
                upgradeToolButton.interactable = plot.CanUpgradeActiveTool;
                upgradeToolButtonText.text = !upgradeable
                    ? FarmLocalization.Get("hud.upgrade.select_tool", "SELECT A TOOL TO UPGRADE")
                    : maxed
                        ? FarmLocalization.Format("hud.upgrade.maxed", "{0} • MAX LEVEL", plot.ActiveToolDisplayName.ToUpperInvariant())
                        : progressionLocked
                            ? FarmLocalization.Format("hud.upgrade.mastery_locked", "REQUIRES {0} MASTERY L{1}", FarmMasteryRules.DisplayName(requiredSkill), requiredLevel)
                            : FarmLocalization.Format("hud.upgrade.action", "UPGRADE {0} • ${1}", plot.ActiveToolDisplayName.ToUpperInvariant(), upgradeCost);
                landUpgradeButton.interactable = plot.CanUpgradeLand;
                landUpgradeButtonText.text = state.IsLandMaxed
                    ? FarmLocalization.Format("hud.land.maxed", "MAX LAND • {0} PLOT TILES", plot.LandTileCount)
                    : FarmLocalization.Format("hud.land.buy", "BUY LAND L{0} • {1} PLOT TILES • ${2}", state.LandLevel + 1, FarmGameState.GetLandTileCount(state.LandLevel + 1), plot.LandUpgradeCost);
            }
        }

        public void ShowPickupToast(string itemId, int amount)
        {
            var definition = FarmContentDatabase.GetItem(itemId);
            var displayName = definition != null ? definition.LocalizedName : itemId;
            ShowToast($"+{amount}  {displayName}", false);
        }

        public void ShowInventoryFullToast()
        {
            ShowToast(FarmLocalization.Get("hud.inventory.full", "Inventory full ? use storage to make room."), true);
        }

        public void ShowSystemToast(string message, bool warning = false) => ShowToast(message, warning);

        private void ShowToast(string message, bool warning)
        {
            if (pickupToastText == null || pickupToastGroup == null) return;
            pickupToastText.text = message;
            pickupToastText.color = warning ? new Color(1f, 0.45f, 0.28f) : AccentColor;
            pickupToastGroup.alpha = 1f;
            pickupToastUntil = Time.unscaledTime + 2.4f;
            pickupToast.transform.SetAsLastSibling();
        }

        private void UpdatePickupToast()
        {
            if (pickupToastGroup == null || pickupToastGroup.alpha <= 0f) return;
            var remaining = pickupToastUntil - Time.unscaledTime;
            pickupToastGroup.alpha = remaining > 0.35f ? 1f : Mathf.Clamp01(remaining / 0.35f);
        }
        public void ToggleInventory() => SetInventoryOpen(!inventoryOpen);

        public void SetInventoryOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            if (value && storageOpen) SetStorageOpen(false);
            if (value && journalOpen) SetJournalOpen(false);
            if (value && sleepConfirmationOpen) SetSleepConfirmationOpen(false);
            if (value && dailyOrdersOpen) SetDailyOrdersOpen(false);
            inventoryOpen = value;
            SetCanvasGroup(inventoryGroup, value);
            if (value) inventoryWindow.transform.SetAsLastSibling();
            if (!value)
            {
                EndItemDrag();
                HideItemTooltip();
            }
            UpdateModalState();
        }

        public void SetStorageOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            if (value && inventoryOpen) SetInventoryOpen(false);
            if (value && journalOpen) SetJournalOpen(false);
            if (value && sleepConfirmationOpen) SetSleepConfirmationOpen(false);
            if (value && dailyOrdersOpen) SetDailyOrdersOpen(false);
            storageOpen = value;
            SetCanvasGroup(storageGroup, value);
            if (value)
            {
                storageWindow.transform.SetAsLastSibling();
                storageFeedbackText.text = FarmLocalization.Get("hud.storage.instructions", "Left click: full stack  ?  Shift + left click: half  ?  Right click: one item.  Drag an item to the hotbar to equip it.");
            }
            else HideItemTooltip();
            UpdateModalState();
        }

        public void TransferToStorage(string itemId, int requestedAmount) =>
            TransferToStorage(itemId, FarmItemQuality.Normal, requestedAmount);

        public void TransferToStorage(string itemId, FarmItemQuality quality, int requestedAmount)
        {
            if (plot == null || plot.GameState == null) return;
            if (!EnsureStorageAuthority($"direction=to_storage;item={itemId};quality={quality};amount={requestedAmount}")) return;
            var amount = Mathf.Min(requestedAmount, plot.GameState.GetQuantity(itemId, quality));
            if (amount > 0 && plot.GameState.TransferToStorage(itemId, quality, amount))
                storageFeedbackText.text = FarmLocalization.Format("hud.storage.stored", "Stored: {0}{1} x{2}.", DisplayItemName(itemId), QualityInline(quality), amount);
            else storageFeedbackText.text = FarmLocalization.Get("hud.storage.store_failed", "Could not store the item. Check storage space.");
        }

        public void TransferFromStorage(string itemId, int requestedAmount) =>
            TransferFromStorage(itemId, FarmItemQuality.Normal, requestedAmount);

        public void TransferFromStorage(string itemId, FarmItemQuality quality, int requestedAmount)
        {
            if (plot == null || plot.GameState == null) return;
            if (!EnsureStorageAuthority($"direction=from_storage;item={itemId};quality={quality};amount={requestedAmount}")) return;
            var amount = Mathf.Min(requestedAmount, plot.GameState.GetStorageQuantity(itemId, quality));
            if (amount > 0 && plot.GameState.TransferFromStorage(itemId, quality, amount))
                storageFeedbackText.text = FarmLocalization.Format("hud.storage.retrieved", "Retrieved: {0}{1} x{2}.", DisplayItemName(itemId), QualityInline(quality), amount);
            else storageFeedbackText.text = FarmLocalization.Get("hud.storage.retrieve_failed", "Could not retrieve the item. Check inventory space.");
        }

        public int TransferHalf(string itemId, bool fromBackpack) =>
            TransferHalf(itemId, FarmItemQuality.Normal, fromBackpack);

        public int TransferHalf(string itemId, FarmItemQuality quality, bool fromBackpack)
        {
            if (plot == null || plot.GameState == null || string.IsNullOrWhiteSpace(itemId)) return 0;
            var available = fromBackpack
                ? plot.GameState.GetQuantity(itemId, quality)
                : plot.GameState.GetStorageQuantity(itemId, quality);
            if (available <= 0) return 0;
            var amount = Mathf.Max(1, Mathf.CeilToInt(available * 0.5f));
            var before = available;
            if (fromBackpack) TransferToStorage(itemId, quality, amount);
            else TransferFromStorage(itemId, quality, amount);
            var after = fromBackpack
                ? plot.GameState.GetQuantity(itemId, quality)
                : plot.GameState.GetStorageQuantity(itemId, quality);
            return Mathf.Max(0, before - after);
        }

        private bool EnsureStorageAuthority(string payload)
        {
            if (FarmSessionTime.IsSimulationAuthority) return true;
            FarmSessionIntentBus.Raise(FarmSessionIntentKind.StorageTransfer, "local", payload);
            if (storageFeedbackText != null)
                storageFeedbackText.text = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
            return false;
        }
        public void SetInventoryFilter(FarmCollectionCategory category)
        {
            inventoryFilter = category;
            HideItemTooltip();
            EndItemDrag();
            if (plot != null && plot.GameState != null) RefreshInventory(plot.GameState);
        }

        public bool OrganizeInventory()
        {
            if (plot == null || plot.GameState == null) return false;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.InventoryOrganize, "local", "target=inventory");
                ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return false;
            }
            var changed = plot.GameState.SortInventory();
            ShowSystemToast(changed ? FarmLocalization.Get("hud.inventory.organized", "Inventory organized.") : FarmLocalization.Get("hud.inventory.already_organized", "Inventory is already organized."), false);
            return changed;
        }

        public bool OrganizeStorage()
        {
            if (plot == null || plot.GameState == null) return false;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.InventoryOrganize, "local", "target=storage");
                if (storageFeedbackText != null)
                    storageFeedbackText.text = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
                return false;
            }
            var changed = plot.GameState.SortStorage();
            if (storageFeedbackText != null)
                storageFeedbackText.text = changed ? FarmLocalization.Get("hud.storage.organized", "Storage organized.") : FarmLocalization.Get("hud.storage.already_organized", "Storage is already organized.");
            return changed;
        }

        public void ToggleJournal() => SetJournalOpen(!journalOpen);

        public void SetJournalOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            if (value && inventoryOpen) SetInventoryOpen(false);
            if (value && storageOpen) SetStorageOpen(false);
            if (value && sleepConfirmationOpen) SetSleepConfirmationOpen(false);
            if (value && dailyOrdersOpen) SetDailyOrdersOpen(false);
            journalOpen = value;
            SetCanvasGroup(journalGroup, value);
            if (value) journalWindow.transform.SetAsLastSibling();
            UpdateModalState();
        }

        public void SetSleepConfirmationOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            if (value && inventoryOpen) SetInventoryOpen(false);
            if (value && storageOpen) SetStorageOpen(false);
            if (value && journalOpen) SetJournalOpen(false);
            if (value && dailyOrdersOpen) SetDailyOrdersOpen(false);
            sleepConfirmationOpen = value;
            SetCanvasGroup(sleepConfirmationGroup, value);
            if (value) sleepConfirmationWindow.transform.SetAsLastSibling();
            UpdateModalState();
        }

        public void SetDailyOrdersOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            if (value && inventoryOpen) SetInventoryOpen(false);
            if (value && storageOpen) SetStorageOpen(false);
            if (value && journalOpen) SetJournalOpen(false);
            if (value && sleepConfirmationOpen) SetSleepConfirmationOpen(false);
            dailyOrdersOpen = value;
            SetCanvasGroup(dailyOrdersGroup, value);
            if (value) dailyOrdersWindow.transform.SetAsLastSibling();
            UpdateModalState();
        }

        public void SetSettingsOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            settingsOpen = value;
            UpdateModalState();
        }

        public void SetMasteryOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            masteryOpen = value;
            UpdateModalState();
        }

        public void SetEconomyLedgerOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            economyLedgerOpen = value;
            UpdateModalState();
        }

        public void SetCraftingOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            craftingOpen = value;
            UpdateModalState();
        }

        public void SetBuildingCatalogOpen(bool value)
        {
            buildingCatalogOpen = value;
            UpdateModalState();
        }

        public void SetMailboxOpen(bool value)
        {
            mailboxOpen = value;
            UpdateModalState();
        }

        public void SetCollectionOpen(bool value)
        {
            collectionOpen = value;
            UpdateModalState();
        }

        public void SetSeasonalPlannerOpen(bool value)
        {
            seasonalPlannerOpen = value;
            UpdateModalState();
        }

        public void SetShopOpen(bool value)
        {
            if (value)
            {
                SetInventoryOpen(false);
                SetStorageOpen(false);
                SetJournalOpen(false);
                SetSleepConfirmationOpen(false);
                SetDailyOrdersOpen(false);
            }
            shopOpen = value;
            if (shopPanel != null)
            {
                shopPanel.SetActive(value);
                if (value) shopPanel.transform.SetAsLastSibling();
            }
            UpdateModalState();
        }

        public void CompleteDailyOrder(int index) => plot?.TryCompleteDailyOrder(index);

        public void ConfirmSleep()
        {
            plot?.ConfirmSleep();
        }

        public void PrepareEveningTea()
        {
            plot?.PrepareEveningTea();
        }

        public void ShowDayTransition(int day, string weather)
        {
            if (dayTransitionGroup == null || dayTransitionText == null) return;
            if (dayTransitionRoutine != null) StopCoroutine(dayTransitionRoutine);
            dayTransitionText.text = string.IsNullOrEmpty(weather)
                ? $"DIA {day}  \u2022  06:00"
                : $"DIA {day}  \u2022  06:00\n{weather}";
            dayTransitionRoutine = StartCoroutine(FadeDayTransition());
        }

        private IEnumerator FadeDayTransition()
        {
            dayTransitionGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(1.15f);
            var elapsed = 0f;
            const float duration = 0.75f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                dayTransitionGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            dayTransitionGroup.alpha = 0f;
            dayTransitionRoutine = null;
        }

        public void ClaimJournalQuest(int index)
        {
            if (plot == null || plot.GameState == null || index < 0 || index >= FarmJournalDatabase.Definitions.Count) return;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                ShowToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            var definition = FarmJournalDatabase.Definitions[index];
            if (plot.GameState.TryClaimJournalReward(definition.Id, out var reward))
                ShowToast(FarmLocalization.Format("hud.journal.claimed", "Journal entry completed: +${0}", reward), false);
        }

        private static void SetCanvasGroup(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen || masteryOpen || economyLedgerOpen || craftingOpen || buildingCatalogOpen || mailboxOpen || collectionOpen || seasonalPlannerOpen || shopOpen;

        public void ShowItemTooltip(string itemId, Vector2 screenPosition, string context) =>
            ShowItemTooltip(itemId, FarmItemQuality.Normal, screenPosition, context);

        public void ShowItemTooltip(string itemId, FarmItemQuality quality, Vector2 screenPosition, string context)
        {
            if (itemTooltipGroup == null || plot == null || plot.GameState == null ||
                string.IsNullOrWhiteSpace(itemId)) return;
            var definition = FarmContentDatabase.GetItem(itemId);
            if (definition == null) return;
            quality = FarmItemQualityRules.Clamp(quality);
            tooltipItemId = itemId;
            tooltipContext = context ?? string.Empty;
            var quantity = string.Equals(tooltipContext, FarmLocalization.Get("storage.chest", "STORAGE"), StringComparison.OrdinalIgnoreCase)
                ? plot.GameState.GetStorageQuantity(itemId, quality)
                : plot.GameState.GetQuantity(itemId, quality);
            itemTooltipTitle.text = definition.LocalizedName.ToUpperInvariant() + QualityInline(quality).ToUpperInvariant();
            itemTooltipTitle.color = QualityColor(quality);
            var category = definition.Category switch
            {
                ItemCategory.Seed => FarmLocalization.Get("tooltip.category.seed", "SEED"),
                ItemCategory.Crop => FarmLocalization.Get("tooltip.category.crop", "CROP"),
                ItemCategory.Tool => FarmLocalization.Get("tooltip.category.tool", "TOOL"),
                ItemCategory.Material => FarmLocalization.Get("tooltip.category.material", "MATERIAL"),
                ItemCategory.Fertilizer => FarmLocalization.Get("tooltip.category.fertilizer", "FERTILIZER"),
                ItemCategory.Consumable => FarmLocalization.Get("tooltip.category.meal", "MEAL"),
                _ => FarmLocalization.Get("tooltip.category.item", "ITEM")
            };
            var use = definition.Category switch
            {
                ItemCategory.Seed => FarmLocalization.Get("tooltip.use.seed", "Plant in prepared soil."),
                ItemCategory.Crop => FarmLocalization.Get("tooltip.use.crop", "Sell it, fulfill orders, or store it."),
                ItemCategory.Tool => FarmLocalization.Get("tooltip.use.tool", "Equip it in the hotbar."),
                ItemCategory.Material => FarmLocalization.Get("tooltip.use.material", "Use it in crafting recipes and buildings."),
                ItemCategory.Fertilizer => FarmLocalization.Get("tooltip.use.fertilizer", "Use on prepared soil or a crop before it is ready."),
                ItemCategory.Consumable => FarmLocalization.Get("tooltip.use.meal", "Select in the hotbar and press R to restore Energy."),
                _ => FarmLocalization.Get("tooltip.use.item", "A farm item.")
            };
            var qualityBaseValue = definition.Category == ItemCategory.Crop
                ? FarmItemQualityRules.UnitSellPrice(definition, quality)
                : FarmEconomyRules.BaseSellPrice(definition);
            var unitValue = definition.Category == ItemCategory.Crop
                ? plot.GameState.GetMarketUnitPrice(itemId, quality)
                : qualityBaseValue;
            var value = unitValue > 0
                ? definition.Category == ItemCategory.Crop
                    ? FarmLocalization.Format("tooltip.value.market", "Today: ${0}  ?  Quality base: ${1}", unitValue, qualityBaseValue)
                    : FarmLocalization.Format("tooltip.value.unit", "Unit value: ${0}", unitValue)
                : FarmLocalization.Get("tooltip.value.none", "Not sold directly");
            itemTooltipBody.text = FarmLocalization.Format(
                "tooltip.body",
                "{0}  ?  {1}  ?  QUALITY {2}\nQuantity: {3}  ?  Max stack: {4}\n{5}\n{6}",
                category,
                tooltipContext,
                FarmItemQualityRules.DisplayName(quality).ToUpperInvariant(),
                quantity,
                definition.MaxStack,
                value,
                use);
            SetCanvasGroup(itemTooltipGroup, true);
            itemTooltip.transform.SetAsLastSibling();
            MoveItemTooltip(screenPosition);
        }
        public void MoveItemTooltip(Vector2 screenPosition)
        {
            if (itemTooltip == null || itemTooltipGroup == null || itemTooltipGroup.alpha <= 0f) return;
            var rect = itemTooltip.GetComponent<RectTransform>();
            var x = Mathf.Clamp(screenPosition.x + 20f, 12f, Screen.width - rect.sizeDelta.x - 12f);
            var y = Mathf.Clamp(screenPosition.y - 18f, rect.sizeDelta.y + 12f, Screen.height - 12f);
            rect.position = new Vector2(x, y);
        }

        public void HideItemTooltip()
        {
            tooltipItemId = null;
            tooltipContext = null;
            SetCanvasGroup(itemTooltipGroup, false);
        }

        public void BeginItemDrag(string itemId, Vector2 screenPosition)
        {
            BeginItemDrag(itemId, FarmItemQuality.Normal, false, screenPosition);
        }

        public void BeginStorageItemDrag(string itemId, FarmItemQuality quality, bool sourceIsStorage, Vector2 screenPosition)
        {
            BeginItemDrag(itemId, quality, sourceIsStorage, screenPosition);
        }

        private void BeginItemDrag(string itemId, FarmItemQuality quality, bool sourceIsStorage, Vector2 screenPosition)
        {
            if (hotbarCommandPending || (!inventoryOpen && !storageOpen) || plot == null || plot.GameState == null) return;
            var quantity = sourceIsStorage
                ? plot.GameState.GetStorageQuantity(itemId, quality)
                : plot.GameState.GetQuantity(itemId, quality);
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0) return;
            hoveredHotbarDropIndex = -1;
            draggedItemId = itemId;
            draggedItemQuality = FarmItemQualityRules.Clamp(quality);
            draggedFromStorage = sourceIsStorage;
            draggedHotbarIndex = -1;
            var definition = FarmContentDatabase.GetItem(itemId);
            dragGhostIcon.text = IconLetter(definition);
            dragGhostIcon.color = IconColor(definition);
            dragGhostLabel.text = definition != null ? definition.LocalizedName : itemId;
            dragGhost.transform.SetAsLastSibling();
            dragGhost.SetActive(true);
            UpdateItemDrag(screenPosition);
        }

        public void BeginHotbarDrag(int slotIndex, Vector2 screenPosition)
        {
            if (hotbarCommandPending || plot == null || plot.GameState == null) return;
            var entry = plot.GameState.GetHotbarEntry(slotIndex);
            if (string.IsNullOrWhiteSpace(entry)) return;
            hoveredHotbarDropIndex = -1;
            draggedItemId = null;
            draggedFromStorage = false;
            draggedHotbarIndex = slotIndex;
            ResolveEntry(entry, plot.GameState, out var icon, out var label, out _, out var color);
            dragGhostIcon.text = icon;
            dragGhostIcon.color = color;
            dragGhostLabel.text = label;
            dragGhost.transform.SetAsLastSibling();
            dragGhost.SetActive(true);
            UpdateItemDrag(screenPosition);
        }

        public void UpdateItemDrag(Vector2 screenPosition)
        {
            if (dragGhost == null || !dragGhost.activeSelf) return;
            dragGhost.GetComponent<RectTransform>().position = screenPosition + new Vector2(22f, -22f);
        }

        public async void CompleteItemDrag(int hotbarIndex)
        {
            if (hotbarCommandPending || plot == null ||
                (string.IsNullOrEmpty(draggedItemId) && draggedHotbarIndex < 0)) return;
            var sourceHotbarIndex = draggedHotbarIndex;
            var itemId = draggedItemId;
            hotbarCommandPending = true;
            if (dragGhost != null && dragGhost.activeSelf)
                dragGhostLabel.text = FarmLocalization.Get("hud.hotbar.confirming", "Confirming...");
            try
            {
                if (sourceHotbarIndex >= 0)
                    await plot.SwapHotbarSlotsAsync(sourceHotbarIndex, hotbarIndex);
                else
                {
                    if (draggedFromStorage)
                    {
                        var amount = plot.GameState.GetStorageQuantity(itemId, draggedItemQuality);
                        if (amount <= 0 || !plot.GameState.TransferFromStorage(itemId, draggedItemQuality, amount))
                        {
                            storageFeedbackText.text = FarmLocalization.Get("hud.storage.retrieve_failed", "Could not retrieve the item. Check inventory space.");
                            return;
                        }
                    }
                    await plot.AssignItemToHotbarAsync(hotbarIndex, itemId);
                }
            }
            finally
            {
                hotbarCommandPending = false;
                ResetItemDrag();
            }
        }

        public void SetHotbarDropTarget(int index, bool isHovering)
        {
            if (isHovering)
            {
                hoveredHotbarDropIndex = index;
                return;
            }

            if (hoveredHotbarDropIndex == index) hoveredHotbarDropIndex = -1;
        }

        public void EndItemDrag()
        {
            if (hotbarCommandPending) return;
            if ((!string.IsNullOrEmpty(draggedItemId) || draggedHotbarIndex >= 0) && hoveredHotbarDropIndex >= 0)
            {
                CompleteItemDrag(hoveredHotbarDropIndex);
                return;
            }

            ResetItemDrag();
        }

        private void ResetItemDrag()
        {
            hoveredHotbarDropIndex = -1;
            draggedItemId = null;
            draggedFromStorage = false;
            draggedHotbarIndex = -1;
            if (dragGhost != null) dragGhost.SetActive(false);
        }
        public void SelectHotbarSlot(int index) => plot?.SelectHotbarSlot(index);
        public async void ClearHotbarSlot(int index)
        {
            if (hotbarCommandPending || plot == null) return;
            var state = plot.GameState;
            if (state == null) return;
            var entry = state.GetHotbarEntry(index);
            if (FarmGameState.IsCoreToolEntry(entry))
            {
                ShowSystemToast(FarmLocalization.Get("hud.hotbar.core_tool_locked", "Core tools stay in the hotbar. Drag them to rearrange."));
                return;
            }
            hotbarCommandPending = true;
            try
            {
                if (string.IsNullOrEmpty(entry) && FarmGameState.TryGetDefaultCoreTool(index, out var defaultTool) && !state.ContainsHotbarEntry(defaultTool))
                {
                    var restored = await plot.RestoreCoreToolToHotbarAsync(index, defaultTool);
                    ShowSystemToast(restored
                        ? FarmLocalization.Get("hud.hotbar.core_tool_restored", "Core tool restored to the hotbar.")
                        : FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), !restored);
                }
                else await plot.ClearHotbarSlotAsync(index);
            }
            finally
            {
                hotbarCommandPending = false;
            }
        }
        public bool AssignItemToHotbar(int index, string itemId) => plot != null && plot.AssignItemToHotbar(index, itemId);

        private void HandleInventoryInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (buildingCatalogOpen || mailboxOpen || collectionOpen) return;
            if (keyboard.jKey.wasPressedThisFrame)
            {
                ToggleJournal();
            }
            else if (keyboard.iKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)
            {
                if (storageOpen) SetStorageOpen(false);
                else ToggleInventory();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (inventoryOpen) SetInventoryOpen(false);
                if (storageOpen) SetStorageOpen(false);
                if (journalOpen) SetJournalOpen(false);
                if (sleepConfirmationOpen) plot?.CancelSleepReady();
                if (dailyOrdersOpen) SetDailyOrdersOpen(false);
                if (shopOpen) SetShopOpen(false);
            }
        }

        private void RefreshDailyOrders(FarmGameState state)
        {
            if (dailyOrdersSummaryText == null || plot == null) return;
            var orders = plot.DailyOrders;
            var completed = 0;
            for (var index = 0; index < dailyOrderTexts.Length; index++)
            {
                if (index >= orders.Count)
                {
                    dailyOrderTexts[index].text = FarmLocalization.Get("orders.unavailable", "Order unavailable");
                    dailyOrderButtons[index].interactable = false;
                    dailyOrderButtonTexts[index].text = FarmLocalization.Get("orders.unavailable_short", "UNAVAILABLE");
                    continue;
                }
                var order = orders[index];
                var item = order.Item;
                var storageConnected = state.GetMasteryLevel(FarmMasterySkill.Commerce) >= 3 || state.CommunityProjects.MarketRouteComplete;
                var have = item != null ? state.GetQuantity(item.Id) + (storageConnected ? state.GetStorageQuantity(item.Id) : 0) : 0;
                var done = state.DailyOrders.IsCompleted(index);
                if (done) completed++;
                var contact = FarmCommunityCatalog.GetContact(order.RequesterId);
                var favor = state.Community.GetFavor(order.RequesterId);
                var remainingFavor = FarmCommunityCatalog.FavorToNextBond(favor);
                var communityLine = remainingFavor > 0
                    ? FarmLocalization.Format("orders.community.next", "Requested by {0}  |  {1} Favor  |  {2} to next bond", contact.LocalizedName, favor, remainingFavor)
                    : FarmLocalization.Format("orders.community.max", "Requested by {0}  |  {1} Favor  |  Bond complete", contact.LocalizedName, favor);
                var recommendedRole = FarmCoopRoleRules.DisplayName(FarmCoopRoleRules.RecommendedRole(order.Type));
                var roleLine = FarmLocalization.Format("roles.order_recommended", "Best co-op role: {0}", recommendedRole);
                dailyOrderTexts[index].text = FarmLocalization.Format("orders.card_with_role", "{0}\nYou have: {1}/{2}\nReward: ${3}\n{4}\n{5}", order.DisplayText, have, order.Quantity, order.Reward, communityLine, roleLine);
                dailyOrderButtons[index].interactable = !done && have >= order.Quantity && plot.OrderBoardVisible;
                dailyOrderButtonTexts[index].text = done
                    ? FarmLocalization.Get("orders.delivered", "DELIVERED")
                    : have >= order.Quantity ? FarmLocalization.Get("orders.deliver", "DELIVER")
                    : FarmLocalization.Format("orders.remaining", "NEED {0}", order.Quantity - have);
            }
            dailyOrdersSummaryText.text = FarmLocalization.Format("orders.summary", "DAY {0}  ?  {1}/{2} delivered  ?  Complete all 3: +${3}", state.DayNumber, completed, orders.Count, FarmDailyOrderGenerator.BoardCompletionBonus);
            var neighborhoodPerks = new StringBuilder();
            foreach (var contact in FarmCommunityCatalog.AllContacts)
            {
                if (!state.HasNeighborhoodUnlock(contact.Id)) continue;
                if (neighborhoodPerks.Length > 0) neighborhoodPerks.Append("  •  ");
                neighborhoodPerks.Append(FarmCommunityCatalog.NeighborhoodUnlockDescription(contact.Id));
            }
            if (neighborhoodPerks.Length > 0)
                dailyOrdersSummaryText.text += FarmLocalization.Format("community.perks.active", "\nNEIGHBOR PERKS: {0}", neighborhoodPerks);
            if (state.GetMasteryLevel(FarmMasterySkill.Commerce) >= 2)
            {
                var tomorrow = FarmDailyOrderGenerator.Generate(state.WorldSeed, state.DayNumber + 1);
                var preview = new StringBuilder();
                for (var index = 0; index < tomorrow.Count; index++)
                {
                    if (index > 0) preview.Append(", ");
                    preview.Append(tomorrow[index].Item != null ? tomorrow[index].Item.LocalizedName : tomorrow[index].ItemId);
                }
                dailyOrdersSummaryText.text += FarmLocalization.Format("orders.tomorrow", "\nTomorrow: {0}", preview);
            }
        }
        private void RefreshInventory(FarmGameState state)
        {
            var filtered = new System.Collections.Generic.List<InventoryStack>();
            foreach (var candidate in state.Inventory)
            {
                var definition = candidate != null ? FarmContentDatabase.GetItem(candidate.ItemId) : null;
                if (MatchesInventoryFilter(definition, inventoryFilter)) filtered.Add(candidate);
            }
            inventoryVisibleItemCount = filtered.Count;
            var filterSuffix = inventoryFilter == FarmCollectionCategory.All
                ? string.Empty
                : FarmLocalization.Format("hud.inventory.showing", "  •  SHOWING {0}", filtered.Count);
            inventoryCapacityText.text = FarmLocalization.Format("hud.inventory.capacity", "INVENTORY  {0}/{1} slots{2}", state.UsedSlots, state.SlotCapacity, filterSuffix);
            for (var index = 0; index < inventorySlots.Length; index++)
            {
                var occupied = index < filtered.Count;
                var stack = occupied ? filtered[index] : null;
                var definition = occupied ? FarmContentDatabase.GetItem(stack.ItemId) : null;
                inventorySlotBackgrounds[index].color = occupied ? SlotColor : EmptySlotColor;
                inventoryIcons[index].text = occupied ? IconLetter(definition) : string.Empty;
                inventoryIcons[index].color = IconColor(definition);
                inventoryNames[index].text = occupied ? (definition != null ? definition.LocalizedName : stack.ItemId) + QualityInline(stack.Quality) : FarmLocalization.Get("hud.inventory.empty_slot", "Empty");
                inventoryNames[index].color = occupied ? QualityColor(stack.Quality) : new Color(0.48f, 0.52f, 0.45f);
                inventoryCounts[index].text = occupied ? $"x{stack.Quantity}" : string.Empty;
                inventorySlots[index].GetComponent<FarmInventorySlotView>().Initialize(this, occupied ? stack.ItemId : null, occupied ? stack.Quality : FarmItemQuality.Normal);
            }
            for (var index = 0; index < inventoryFilterBackgrounds.Length; index++)
                if (inventoryFilterBackgrounds[index] != null)
                    inventoryFilterBackgrounds[index].color = index == (int)inventoryFilter
                        ? new Color(0.42f, 0.56f, 0.20f, 1f)
                        : new Color(0.20f, 0.28f, 0.13f, 1f);
        }

        private static bool MatchesInventoryFilter(ItemDefinition definition, FarmCollectionCategory filter)
        {
            if (filter == FarmCollectionCategory.All) return true;
            if (definition == null) return false;
            if (filter == FarmCollectionCategory.Seeds) return definition.Category == ItemCategory.Seed;
            if (filter == FarmCollectionCategory.Crops) return definition.Category == ItemCategory.Crop;
            if (filter == FarmCollectionCategory.Projects) return FarmCollectionDatabase.IsProject(definition);
            if (filter == FarmCollectionCategory.Materials)
                return definition.Category == ItemCategory.Material && !FarmCollectionDatabase.IsProject(definition);
            return true;
        }

        private void RefreshHotbar(FarmGameState state)
        {
            for (var index = 0; index < FarmGameState.HotbarSlotCount; index++)
            {
                var entry = state.GetHotbarEntry(index);
                ResolveEntry(entry, state, out var icon, out var label, out var count, out var color);
                hotbarSlots[index].color = index == state.SelectedHotbarIndex ? SelectedSlotColor : (string.IsNullOrEmpty(entry) ? EmptySlotColor : SlotColor);
                hotbarIcons[index].text = icon;
                hotbarIcons[index].color = color;
                hotbarLabels[index].text = $"{index + 1}  {label}";
                hotbarCounts[index].text = count;
            }
        }

        private void RefreshStorage(FarmGameState state)
        {
            storageBackpackCapacityText.text = FarmLocalization.Format("hud.storage.backpack_capacity", "INVENTORY  {0}/{1}", state.UsedSlots, state.SlotCapacity);
            storageChestCapacityText.text = FarmLocalization.Format("hud.storage.chest_capacity", "STORAGE  {0}/{1}", state.StorageUsedSlots, state.StorageSlotCapacity);
            for (var index = 0; index < storageBackpackViews.Length; index++)
            {
                var stack = index < state.Inventory.Count ? state.Inventory[index] : null;
                RefreshStorageSlot(storageBackpackBackgrounds[index], storageBackpackIcons[index], storageBackpackNames[index], storageBackpackCounts[index], storageBackpackViews[index], true, stack);
            }
            for (var index = 0; index < storageChestViews.Length; index++)
            {
                var stack = index < state.Storage.Count ? state.Storage[index] : null;
                RefreshStorageSlot(storageChestBackgrounds[index], storageChestIcons[index], storageChestNames[index], storageChestCounts[index], storageChestViews[index], false, stack);
            }
        }

        private void RefreshStorageSlot(Image background, Text icon, Text label, Text count, FarmStorageSlotView view, bool fromBackpack, InventoryStack stack)
        {
            var occupied = stack != null;
            var definition = occupied ? FarmContentDatabase.GetItem(stack.ItemId) : null;
            background.color = occupied ? SlotColor : EmptySlotColor;
            icon.text = occupied ? IconLetter(definition) : string.Empty;
            icon.color = IconColor(definition);
            label.text = occupied ? (definition != null ? ShortName(definition.LocalizedName) : stack.ItemId.ToUpperInvariant()) + QualityShort(stack.Quality) : FarmLocalization.Get("hotbar.empty_short", "EMPTY");
            label.color = occupied ? QualityColor(stack.Quality) : new Color(0.48f, 0.52f, 0.45f);
            count.text = occupied ? $"x{stack.Quantity}" : string.Empty;
            view.Initialize(this, fromBackpack, occupied ? stack.ItemId : null, occupied ? stack.Quality : FarmItemQuality.Normal);
        }

        private void RefreshJournal(FarmGameState state)
        {
            var definitions = FarmJournalDatabase.Definitions;
            for (var index = 0; index < journalQuestTexts.Length; index++)
            {
                var definition = definitions[index];
                var current = Mathf.Min(definition.Current(state.Journal), definition.Target);
                var claimed = state.Journal != null && state.Journal.HasClaimed(definition.Id);
                var complete = definition.IsComplete(state.Journal);
                journalQuestTexts[index].text = FarmLocalization.Format("journal.card", "{0}  •  {1}\n{2}\n{3}/{4}  •  Reward: ${5}", definition.Category, definition.Title, definition.Description, current, definition.Target, definition.RewardMoney);
                journalClaimButtons[index].interactable = complete && !claimed;
                journalClaimButtonTexts[index].text = claimed
                    ? FarmLocalization.Get("journal.claimed", "CLAIMED")
                    : complete ? FarmLocalization.Get("journal.claim", "CLAIM")
                    : FarmLocalization.Get("journal.in_progress", "IN PROGRESS");
            }
        }

        private static string DisplayItemName(string itemId)
        {
            var definition = FarmContentDatabase.GetItem(itemId);
            return definition != null ? definition.LocalizedName : itemId;
        }

        private static string QualityInline(FarmItemQuality quality) =>
            FarmItemQualityRules.Clamp(quality) == FarmItemQuality.Normal
                ? string.Empty
                : $"  {FarmItemQualityRules.ShortMark(quality)} {FarmItemQualityRules.DisplayName(quality)}";

        private static string QualityShort(FarmItemQuality quality) =>
            FarmItemQualityRules.Clamp(quality) == FarmItemQuality.Normal
                ? string.Empty
                : $" {FarmItemQualityRules.ShortMark(quality)}";

        private static Color QualityColor(FarmItemQuality quality) => FarmItemQualityRules.Clamp(quality) switch
        {
            FarmItemQuality.Silver => new Color(0.72f, 0.86f, 0.95f),
            FarmItemQuality.Gold => new Color(1f, 0.76f, 0.22f),
            _ => Color.white
        };        private static void ResolveEntry(string entry, FarmGameState state, out string icon, out string label, out string count, out Color color)
        {
            icon = "+";
            label = FarmLocalization.Get("hotbar.empty_short", "EMPTY");
            count = string.Empty;
            color = new Color(0.45f, 0.48f, 0.42f);
            if (string.IsNullOrEmpty(entry)) return;
            if (entry.StartsWith(FarmGameState.ItemPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var itemId = entry[FarmGameState.ItemPrefix.Length..];
                var definition = FarmContentDatabase.GetItem(itemId);
                icon = IconLetter(definition);
                label = definition != null ? ShortName(definition.LocalizedName) : itemId.ToUpperInvariant();
                count = $"x{state.GetQuantity(itemId)}";
                color = IconColor(definition);
                return;
            }

            var toolId = entry.StartsWith(FarmGameState.ToolPrefix, StringComparison.OrdinalIgnoreCase) ? entry[FarmGameState.ToolPrefix.Length..] : entry;
            if (toolId == "hoe") { icon = "H"; label = FarmLocalization.Format("hotbar.hoe", "HOE L{0}", state.GetToolLevel(FarmTool.Hoe)); color = new Color(0.88f, 0.62f, 0.27f); }
            else if (toolId == "watering_can") { icon = "W"; label = FarmLocalization.Format("hotbar.watering_can", "WATER L{0}", state.GetToolLevel(FarmTool.WateringCan)); color = new Color(0.35f, 0.72f, 0.95f); }
            else if (toolId == "harvest") { icon = "H"; label = FarmLocalization.Format("hotbar.harvest", "HARVEST L{0}", state.GetToolLevel(FarmTool.Harvest)); color = new Color(0.55f, 0.88f, 0.42f); }
            else if (toolId == "pickaxe") { icon = "P"; label = FarmLocalization.Get("hotbar.pickaxe", "PICKAXE"); color = new Color(0.62f, 0.72f, 0.82f); }
            else if (toolId == "axe") { icon = "A"; label = FarmLocalization.Get("hotbar.axe", "AXE"); color = new Color(0.76f, 0.46f, 0.24f); }
        }
        private static string ShortName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "ITEM";
            var firstSpace = value.IndexOf(' ');
            return (firstSpace > 0 ? value[..firstSpace] : value).ToUpperInvariant();
        }

        private static string IconLetter(ItemDefinition definition) => definition == null ? "?" : definition.Id == "pond_fish" ? "F" : definition.Category switch
        {
            ItemCategory.Seed => "S",
            ItemCategory.Crop => "P",
            ItemCategory.Tool => "F",
            ItemCategory.Material => "M",
            ItemCategory.Fertilizer => "C",
            _ => "?"
        };

        private static Color IconColor(ItemDefinition definition) => definition == null ? Color.white : definition.Id == "pond_fish" ? new Color(0.36f, 0.80f, 0.94f) : definition.Category switch
        {
            ItemCategory.Seed => new Color(0.95f, 0.76f, 0.25f),
            ItemCategory.Crop => new Color(0.96f, 0.48f, 0.18f),
            ItemCategory.Tool => new Color(0.48f, 0.72f, 0.92f),
            ItemCategory.Material => new Color(0.70f, 0.62f, 0.48f),
            ItemCategory.Fertilizer => new Color(0.42f, 0.74f, 0.27f),
            _ => Color.white
        };

        private static string BuildInventorySummary(FarmGameState state)
        {
            var builder = new StringBuilder(FarmLocalization.Get("inventory.summary.title", "INVENTORY") + "\n");
            if (state.Inventory.Count == 0) return builder.Append(FarmLocalization.Get("inventory.summary.empty", "Empty\n\nPress I to open")).ToString();
            foreach (var stack in state.Inventory)
            {
                var definition = FarmContentDatabase.GetItem(stack.ItemId);
                builder.Append("? ").Append(definition != null ? definition.LocalizedName : stack.ItemId).Append(QualityInline(stack.Quality)).Append("  x").Append(stack.Quantity).Append('\n');
            }
            builder.Append("\n").Append(state.UsedSlots).Append('/').Append(state.SlotCapacity).Append(FarmLocalization.Get("inventory.summary.slots", " slots  ?  Press I to open"));
            return builder.ToString();
        }
        private bool InterfaceReady()
        {
            if (resourcesText == null || toolText == null || promptText == null || feedbackText == null || saveText == null || tutorialText == null || homesteadText == null || clockText == null || calendarText == null || weatherText == null || energyFill == null || energyText == null || inventorySummaryText == null || shopInfoText == null || shopPanel == null || upgradeToolButton == null || upgradeToolButtonText == null || landUpgradeButton == null || landUpgradeButtonText == null || inventoryWindow == null || inventoryGroup == null || storageWindow == null || storageGroup == null || journalWindow == null || journalGroup == null || sleepConfirmationWindow == null || sleepConfirmationGroup == null || dayTransitionGroup == null || dayTransitionText == null || dailyOrdersWindow == null || dailyOrdersGroup == null || dailyOrdersSummaryText == null) return false;
            foreach (var slot in hotbarSlots) if (slot == null) return false;
            return true;
        }

        private string BuildHomesteadGuide(FarmGameState state)
        {
            if (state == null) return string.Empty;
            var animals = plot != null ? plot.AnimalSystem : null;
            if (animals != null && animals.IsEggAvailable)
                return FarmLocalization.Get("homestead.hud.egg", "HOMESTEAD  •  Chicken Yard: collect today's egg.");
            if (animals != null && !animals.IsFedToday)
                return FarmLocalization.Get("homestead.hud.feed", "HOMESTEAD  •  Chicken Yard: feed the chickens for today's egg.");
            if (state.StorageSlotCapacity > 30)
                return FarmLocalization.Format("homestead.hud.shed", "HOMESTEAD  •  Farm Shed active  •  Shared storage {0}/{1}", state.StorageUsedSlots, state.StorageSlotCapacity);
            if (plot != null && plot.BuildingSystem != null && plot.BuildingSystem.PlacedCount > 0)
                return FarmLocalization.Get("homestead.hud.decorate", "HOMESTEAD  •  Keep shaping the shared space with paths and decorations.");
            return FarmLocalization.Get("homestead.hud.routine", "HOMESTEAD  •  Farm, craft, care for chickens, and organize the day together.");
        }

        private void CreateInterface()
        {
            EnsureEventSystem();
            var root = new GameObject("Farm_UI");
            root.transform.SetParent(transform, false);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var clockPanel = CreatePanel("FarmClock", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(440f, 166f), new Vector2(0.5f, 1f), PanelColor);
            CreateText("ClockTitle", clockPanel.transform, "hud.clock.title", 13, FontStyle.Bold, new Color(0.72f, 0.76f, 0.68f), new Vector2(15f, -7f), new Vector2(410f, 20f), TextAnchor.MiddleCenter);
            clockText = CreateText("ClockValue", clockPanel.transform, "DAY 1   08:00   Morning", 21, FontStyle.Bold, AccentColor, new Vector2(15f, -28f), new Vector2(410f, 32f), TextAnchor.MiddleCenter);
            calendarText = CreateText("CalendarValue", clockPanel.transform, "YEAR 1  \u2022  Spring 1/7", 14, FontStyle.Bold, new Color(0.58f, 0.88f, 0.48f), new Vector2(15f, -63f), new Vector2(410f, 24f), TextAnchor.MiddleCenter);
            weatherText = CreateText("WeatherValue", clockPanel.transform, "Sunny   \u2022   Tomorrow: Cloudy", 14, FontStyle.Bold, new Color(1f, 0.82f, 0.34f), new Vector2(15f, -98f), new Vector2(410f, 24f), TextAnchor.MiddleCenter);
            pestText = CreateText("PestValue", clockPanel.transform, "PESTS: QUIET", 12, FontStyle.Bold, new Color(0.64f, 0.84f, 0.58f), new Vector2(15f, -123f), new Vector2(410f, 22f), TextAnchor.MiddleCenter);

            var energyPanel = CreatePanel("EnergyPanel", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(440f, 48f), new Vector2(0.5f, 1f), PanelColor);
            var energyTrack = CreatePanel("EnergyTrack", energyPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -9f), new Vector2(416f, 30f), new Vector2(0f, 1f), new Color(0.08f, 0.10f, 0.075f, 1f));
            var energyFillObject = CreatePanel("EnergyFill", energyTrack.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(416f, 30f), new Vector2(0f, 1f), new Color(0.30f, 0.78f, 0.38f));
            energyFill = energyFillObject.GetComponent<Image>();
            energyFill.type = Image.Type.Filled;
            energyFill.fillMethod = Image.FillMethod.Horizontal;
            energyFill.fillOrigin = 0;
            energyFill.fillAmount = 1f;
            energyText = CreateText("EnergyText", energyPanel.transform, "ENERGY  100/100", 15, FontStyle.Bold, Color.white, new Vector2(12f, -9f), new Vector2(416f, 30f), TextAnchor.MiddleCenter);
            var statusPanel = CreatePanel("Status", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(630f, 194f), new Vector2(0f, 1f), PanelColor);
            CreateText("Title", statusPanel.transform, "FARM", 24, FontStyle.Bold, AccentColor, new Vector2(18f, -12f), new Vector2(590f, 30f));
            resourcesText = CreateText("Resources", statusPanel.transform, "", 21, FontStyle.Bold, Color.white, new Vector2(18f, -48f), new Vector2(590f, 28f));
            toolText = CreateText("Tool", statusPanel.transform, "", 18, FontStyle.Normal, Color.white, new Vector2(18f, -78f), new Vector2(590f, 25f));
            promptText = CreateText("Prompt", statusPanel.transform, "", 17, FontStyle.Normal, new Color(0.85f, 0.92f, 0.75f), new Vector2(18f, -108f), new Vector2(590f, 25f));
            feedbackText = CreateText("Feedback", statusPanel.transform, "", 17, FontStyle.Bold, AccentColor, new Vector2(18f, -137f), new Vector2(590f, 25f));
            saveText = CreateText("Save", statusPanel.transform, "", 14, FontStyle.Normal, new Color(0.7f, 0.74f, 0.68f), new Vector2(18f, -166f), new Vector2(590f, 22f));

            var tutorialPanel = CreatePanel("FirstHarvest", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -226f), new Vector2(630f, 96f), new Vector2(0f, 1f), PanelColor);
            CreateText("TutorialTitle", tutorialPanel.transform, "week.title", 18, FontStyle.Bold, AccentColor, new Vector2(18f, -12f), new Vector2(590f, 25f));
            tutorialText = CreateText("TutorialProgress", tutorialPanel.transform, "", 16, FontStyle.Normal, Color.white, new Vector2(18f, -45f), new Vector2(590f, 38f));
            var homesteadPanel = CreatePanel("HomesteadRoutine", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -330f), new Vector2(630f, 48f), new Vector2(0f, 1f), new Color(0.075f, 0.11f, 0.07f, 0.92f));
            homesteadText = CreateText("HomesteadRoutineText", homesteadPanel.transform, "", 15, FontStyle.Normal, new Color(0.78f, 0.92f, 0.66f), new Vector2(15f, -9f), new Vector2(600f, 30f), TextAnchor.MiddleCenter);
            var summaryPanel = CreatePanel("InventorySummary", root.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(325f, 245f), new Vector2(1f, 1f), PanelColor);
            inventorySummaryText = CreateText("InventorySummaryText", summaryPanel.transform, "", 17, FontStyle.Normal, Color.white, new Vector2(18f, -16f), new Vector2(290f, 215f));

            CreateHotbar(root.transform);
            CreateInventoryWindow(root.transform);
            CreateStorageWindow(root.transform);
            CreateJournalWindow(root.transform);
            CreateSleepConfirmation(root.transform);
            CreateDailyOrdersWindow(root.transform);
            CreateDayTransition(root.transform);
            CreatePickupToast(root.transform);

            shopPanel = CreatePanel("Shop", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(620f, 520f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("ShopTitle", shopPanel.transform, "MARKET CRATE", 23, FontStyle.Bold, AccentColor, new Vector2(20f, -16f), new Vector2(580f, 30f), TextAnchor.MiddleCenter);
            previousCropButton = CreateButton("PreviousCrop", shopPanel.transform, "<", new Vector2(20f, -86f), new Vector2(52f, 54f));
            shopInfoText = CreateText("ShopInfo", shopPanel.transform, "", 16, FontStyle.Normal, Color.white, new Vector2(80f, -55f), new Vector2(460f, 122f), TextAnchor.MiddleCenter);
            nextCropButton = CreateButton("NextCrop", shopPanel.transform, ">", new Vector2(548f, -86f), new Vector2(52f, 54f));
            sellButton = CreateButton("Sell", shopPanel.transform, "SELL ALL", new Vector2(20f, -205f), new Vector2(280f, 52f));
            buyButton = CreateButton("Buy", shopPanel.transform, "BUY SEEDS", new Vector2(320f, -205f), new Vector2(280f, 52f));
            previousCropButton.onClick.AddListener(() => plot.CycleShopCrop(-1));
            nextCropButton.onClick.AddListener(() => plot.CycleShopCrop(1));
            sellButton.onClick.AddListener(plot.RequestSell);
            buyButton.onClick.AddListener(plot.RequestBuySeeds);
            upgradeToolButton = CreateButton("UpgradeTool", shopPanel.transform, "UPGRADE TOOL", new Vector2(20f, -273f), new Vector2(580f, 52f));
            upgradeToolButtonText = upgradeToolButton.GetComponentInChildren<Text>();
            upgradeToolButton.onClick.AddListener(plot.RequestUpgradeActiveTool);
            landUpgradeButton = CreateButton("UpgradeLand", shopPanel.transform, "BUY LAND", new Vector2(20f, -341f), new Vector2(580f, 52f));
            landUpgradeButtonText = landUpgradeButton.GetComponentInChildren<Text>();
            landUpgradeButton.onClick.AddListener(plot.RequestUpgradeLand);
            var closeShopButton = CreateButton("CloseShop", shopPanel.transform, "CLOSE  [ESC]", new Vector2(200f, -419f), new Vector2(220f, 46f));
            closeShopButton.onClick.AddListener(() => SetShopOpen(false));
            shopPanel.SetActive(false);
        }

        private void CreateDailyOrdersWindow(Transform root)
        {
            dailyOrdersWindow = CreatePanel("DailyOrdersWindow", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.018f, 0.76f));
            var backdrop = dailyOrdersWindow.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            dailyOrdersGroup = dailyOrdersWindow.AddComponent<CanvasGroup>();
            var panel = CreatePanel("DailyOrdersPanel", dailyOrdersWindow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 620f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("Title", panel.transform, "hud.orders.title", 30, FontStyle.Bold, new Color(0.30f, 0.92f, 0.82f), new Vector2(30f, -20f), new Vector2(700f, 42f));
            dailyOrdersSummaryText = CreateText("Summary", panel.transform, "", 16, FontStyle.Bold, Color.white, new Vector2(30f, -66f), new Vector2(820f, 62f));
            var close = CreateButton("CloseOrders", panel.transform, "CLOSE  [ESC]", new Vector2(850f, -22f), new Vector2(160f, 44f));
            close.onClick.AddListener(() => SetDailyOrdersOpen(false));
            for (var index = 0; index < dailyOrderTexts.Length; index++)
            {
                var card = CreatePanel($"DailyOrder_{index + 1}", panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(35f, -158f - (index * 140f)), new Vector2(970f, 132f), new Vector2(0f, 1f), SlotColor);
                dailyOrderTexts[index] = CreateText("OrderText", card.transform, "", 17, FontStyle.Bold, Color.white, new Vector2(22f, -12f), new Vector2(690f, 112f));
                var captured = index;
                dailyOrderButtons[index] = CreateButton("Deliver", card.transform, "DELIVER", new Vector2(735f, -35f), new Vector2(205f, 56f));
                dailyOrderButtonTexts[index] = dailyOrderButtons[index].GetComponentInChildren<Text>();
                dailyOrderButtons[index].onClick.AddListener(() => CompleteDailyOrder(captured));
            }
            CreateText("Hint", panel.transform, "hud.orders.hint", 15, FontStyle.Normal, new Color(0.76f, 0.84f, 0.78f), new Vector2(35f, -570f), new Vector2(850f, 26f));
            SetCanvasGroup(dailyOrdersGroup, false);
        }

        private void CreateSleepConfirmation(Transform root)
        {
            sleepConfirmationWindow = CreatePanel("SleepConfirmation", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.01f, 0.015f, 0.025f, 0.78f));
            var backdrop = sleepConfirmationWindow.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            sleepConfirmationGroup = sleepConfirmationWindow.AddComponent<CanvasGroup>();
            var panel = CreatePanel("SleepPanel", sleepConfirmationWindow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 360f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("Title", panel.transform, "hud.sleep.title", 28, FontStyle.Bold, new Color(0.65f, 0.58f, 1f), new Vector2(30f, -24f), new Vector2(500f, 40f), TextAnchor.MiddleCenter);
            sleepDescriptionText = CreateText("Description", panel.transform, "hud.sleep.description", 18, FontStyle.Normal, Color.white, new Vector2(35f, -82f), new Vector2(490f, 102f), TextAnchor.MiddleCenter);
            var tea = CreateButton("PrepareEveningTea", panel.transform, "rest.button", new Vector2(35f, -202f), new Vector2(490f, 46f));
            var cancel = CreateButton("CancelSleep", panel.transform, "hud.sleep.cancel", new Vector2(35f, -266f), new Vector2(230f, 52f));
            var confirm = CreateButton("ConfirmSleep", panel.transform, "READY UP", new Vector2(295f, -266f), new Vector2(230f, 52f));
            tea.onClick.AddListener(PrepareEveningTea);
            cancel.onClick.AddListener(() => plot?.CancelSleepReady());
            confirm.onClick.AddListener(ConfirmSleep);
            SetCanvasGroup(sleepConfirmationGroup, false);
        }

        private void CreateDayTransition(Transform root)
        {
            var transition = CreatePanel("DayTransition", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.015f, 0.025f, 0.06f, 0.88f));
            var rect = transition.GetComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            dayTransitionText = CreateText("DayTransitionText", transition.transform, "", 42, FontStyle.Bold, new Color(0.86f, 0.82f, 1f), Vector2.zero, new Vector2(760f, 150f), TextAnchor.MiddleCenter);
            dayTransitionText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            dayTransitionText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            dayTransitionText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            dayTransitionGroup = transition.AddComponent<CanvasGroup>();
            dayTransitionGroup.alpha = 0f;
            dayTransitionGroup.interactable = false;
            dayTransitionGroup.blocksRaycasts = false;
        }

        private void CreateJournalWindow(Transform root)
        {
            journalWindow = CreatePanel("JournalWindow", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.015f, 0.02f, 0.012f, 0.72f));
            var backdrop = journalWindow.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            journalGroup = journalWindow.AddComponent<CanvasGroup>();
            var panel = CreatePanel("JournalPanel", journalWindow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 760f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("JournalTitle", panel.transform, "hud.journal.title", 28, FontStyle.Bold, AccentColor, new Vector2(30f, -22f), new Vector2(800f, 38f));
            CreateText("JournalSubtitle", panel.transform, "hud.journal.subtitle", 16, FontStyle.Normal, new Color(0.78f, 0.82f, 0.74f), new Vector2(30f, -62f), new Vector2(900f, 28f));
            var close = CreateButton("CloseJournal", panel.transform, "CLOSE", new Vector2(850f, -20f), new Vector2(120f, 44f));
            close.onClick.AddListener(() => SetJournalOpen(false));
            for (var index = 0; index < journalQuestTexts.Length; index++)
            {
                var card = CreatePanel($"Quest_{index + 1}", panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -112f - (index * 122f)), new Vector2(940f, 108f), new Vector2(0f, 1f), SlotColor);
                journalQuestTexts[index] = CreateText("QuestText", card.transform, "", 16, FontStyle.Normal, Color.white, new Vector2(18f, -10f), new Vector2(700f, 88f));
                var capturedIndex = index;
                journalClaimButtons[index] = CreateButton("Claim", card.transform, "IN PROGRESS", new Vector2(740f, -28f), new Vector2(180f, 52f));
                journalClaimButtonTexts[index] = journalClaimButtons[index].GetComponentInChildren<Text>();
                journalClaimButtons[index].onClick.AddListener(() => ClaimJournalQuest(capturedIndex));
            }
            SetCanvasGroup(journalGroup, false);
        }

        private void CreateHotbar(Transform root)
        {
            var hotbar = CreatePanel("Hotbar", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(850f, 104f), new Vector2(0.5f, 0f), PanelColor);
            for (var index = 0; index < FarmGameState.HotbarSlotCount; index++)
            {
                var slot = CreatePanel($"Slot_{index + 1}", hotbar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f + index * 104f, 0f), new Vector2(96f, 82f), new Vector2(0f, 0.5f), SlotColor);
                hotbarSlots[index] = slot.GetComponent<Image>();
                hotbarIcons[index] = CreateText("Icon", slot.transform, "", 25, FontStyle.Bold, Color.white, new Vector2(6f, -8f), new Vector2(34f, 38f), TextAnchor.MiddleCenter);
                hotbarLabels[index] = CreateText("Label", slot.transform, "", 12, FontStyle.Bold, Color.white, new Vector2(4f, -48f), new Vector2(88f, 25f), TextAnchor.MiddleCenter);
                hotbarCounts[index] = CreateText("Count", slot.transform, "", 13, FontStyle.Bold, Color.white, new Vector2(56f, -10f), new Vector2(34f, 24f), TextAnchor.MiddleRight);
                slot.AddComponent<FarmHotbarSlotView>().Initialize(this, index);
            }
        }

        private void CreateInventoryWindow(Transform root)
        {
            inventoryWindow = CreatePanel("InventoryWindow", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.015f, 0.02f, 0.012f, 0.58f));
            var backdrop = inventoryWindow.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            inventoryGroup = inventoryWindow.AddComponent<CanvasGroup>();
            inventoryGroup.alpha = 0f;
            inventoryGroup.interactable = false;
            inventoryGroup.blocksRaycasts = false;
            var window = CreatePanel("Backpack", inventoryWindow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(790f, 650f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("Title", window.transform, "hud.inventory.title", 28, FontStyle.Bold, AccentColor, new Vector2(28f, -20f), new Vector2(350f, 36f));
            inventoryCapacityText = CreateText("Capacity", window.transform, "", 17, FontStyle.Bold, Color.white, new Vector2(28f, -60f), new Vector2(400f, 28f));
            CreateText("Hint", window.transform, "hud.inventory.hint", 14, FontStyle.Normal, new Color(0.78f, 0.84f, 0.72f), new Vector2(28f, -92f), new Vector2(570f, 26f));
            var close = CreateButton("Close", window.transform, "CLOSE  [I]", new Vector2(620f, -24f), new Vector2(140f, 44f));
            close.onClick.AddListener(() => SetInventoryOpen(false));

            var filterLabels = new[] { "hud.filter.all", "hud.filter.seeds", "hud.filter.crops", "hud.filter.materials", "hud.filter.projects" };
            for (var index = 0; index < filterLabels.Length; index++)
            {
                var captured = (FarmCollectionCategory)index;
                inventoryFilterButtons[index] = CreateButton($"InventoryFilter_{filterLabels[index]}", window.transform, filterLabels[index], new Vector2(28f + index * 112f, -127f), new Vector2(106f, 34f));
                inventoryFilterBackgrounds[index] = inventoryFilterButtons[index].GetComponent<Image>();
                inventoryFilterButtons[index].onClick.AddListener(() => SetInventoryFilter(captured));
            }
            var organize = CreateButton("OrganizeInventory", window.transform, "SORT", new Vector2(620f, -127f), new Vector2(140f, 34f));
            organize.onClick.AddListener(() => OrganizeInventory());

            for (var index = 0; index < inventorySlots.Length; index++)
            {
                var column = index % 5;
                var row = index / 5;
                var slot = CreatePanel($"InventorySlot_{index + 1}", window.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f + column * 150f, -182f - row * 100f), new Vector2(138f, 88f), new Vector2(0f, 1f), EmptySlotColor);
                inventorySlots[index] = slot;
                inventorySlotBackgrounds[index] = slot.GetComponent<Image>();
                inventoryIcons[index] = CreateText("Icon", slot.transform, "", 28, FontStyle.Bold, Color.white, new Vector2(8f, -10f), new Vector2(38f, 38f), TextAnchor.MiddleCenter);
                inventoryNames[index] = CreateText("Name", slot.transform, "", 13, FontStyle.Bold, Color.white, new Vector2(48f, -10f), new Vector2(82f, 42f));
                inventoryCounts[index] = CreateText("Count", slot.transform, "", 15, FontStyle.Bold, AccentColor, new Vector2(88f, -58f), new Vector2(40f, 22f), TextAnchor.MiddleRight);
                slot.AddComponent<FarmInventorySlotView>().Initialize(this, null);
            }

            dragGhost = CreatePanel("DragGhost", root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(170f, 58f), Vector2.zero, new Color(0.08f, 0.10f, 0.06f, 0.96f));
            dragGhostIcon = CreateText("Icon", dragGhost.transform, "", 26, FontStyle.Bold, Color.white, new Vector2(8f, -8f), new Vector2(40f, 40f), TextAnchor.MiddleCenter);
            dragGhostLabel = CreateText("Label", dragGhost.transform, "", 14, FontStyle.Bold, Color.white, new Vector2(52f, -10f), new Vector2(108f, 36f), TextAnchor.MiddleLeft);
            var group = dragGhost.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            dragGhost.SetActive(false);

            itemTooltip = CreatePanel("ItemTooltip", root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(340f, 160f), new Vector2(0f, 1f), new Color(0.035f, 0.055f, 0.03f, 0.98f));
            itemTooltipTitle = CreateText("Title", itemTooltip.transform, "", 18, FontStyle.Bold, AccentColor, new Vector2(16f, -12f), new Vector2(308f, 26f));
            itemTooltipBody = CreateText("Body", itemTooltip.transform, "", 14, FontStyle.Normal, Color.white, new Vector2(16f, -45f), new Vector2(308f, 102f));
            itemTooltipGroup = itemTooltip.AddComponent<CanvasGroup>();
            itemTooltipGroup.blocksRaycasts = false;
            SetCanvasGroup(itemTooltipGroup, false);
        }

        private void CreatePickupToast(Transform root)
        {
            pickupToast = CreatePanel("PickupToast", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 142f), new Vector2(470f, 54f), new Vector2(0.5f, 0f), PanelColor);
            pickupToastText = CreateText("PickupToastText", pickupToast.transform, "", 19, FontStyle.Bold, AccentColor, Vector2.zero, new Vector2(470f, 54f), TextAnchor.MiddleCenter);
            pickupToastText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            pickupToastText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pickupToastText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pickupToastGroup = pickupToast.AddComponent<CanvasGroup>();
            pickupToastGroup.blocksRaycasts = false;
            pickupToastGroup.alpha = 0f;
        }
        private void CreateStorageWindow(Transform root)
        {
            storageWindow = CreatePanel("StorageWindow", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.015f, 0.02f, 0.012f, 0.68f));
            var backdrop = storageWindow.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            storageGroup = storageWindow.AddComponent<CanvasGroup>();
            SetCanvasGroup(storageGroup, false);

            var window = CreatePanel("Storage", storageWindow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(1470f, 760f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("Title", window.transform, "hud.storage.title", 28, FontStyle.Bold, AccentColor, new Vector2(28f, -18f), new Vector2(600f, 38f));
            CreateText("Hint", window.transform, "hud.storage.hint", 15, FontStyle.Normal, new Color(0.78f, 0.84f, 0.72f), new Vector2(28f, -58f), new Vector2(1130f, 26f));
            var close = CreateButton("Close", window.transform, "CLOSE  [ESC]", new Vector2(1275f, -22f), new Vector2(165f, 44f));
            close.onClick.AddListener(() => SetStorageOpen(false));
            storageBackpackCapacityText = CreateText("BackpackCapacity", window.transform, "", 18, FontStyle.Bold, Color.white, new Vector2(28f, -100f), new Vector2(390f, 28f));
            var organizeBackpack = CreateButton("OrganizeBackpack", window.transform, "SORT", new Vector2(445f, -94f), new Vector2(145f, 34f));
            organizeBackpack.onClick.AddListener(() => OrganizeInventory());
            storageChestCapacityText = CreateText("ChestCapacity", window.transform, "", 18, FontStyle.Bold, Color.white, new Vector2(690f, -100f), new Vector2(400f, 28f));
            var organizeChest = CreateButton("OrganizeChest", window.transform, "SORT", new Vector2(1275f, -94f), new Vector2(145f, 34f));
            organizeChest.onClick.AddListener(() => OrganizeStorage());
            CreateText("Direction", window.transform, "STORE  \u2192", 17, FontStyle.Bold, AccentColor, new Vector2(590f, -340f), new Vector2(100f, 28f), TextAnchor.MiddleCenter);

            for (var index = 0; index < storageBackpackViews.Length; index++)
            {
                var column = index % 5;
                var row = index / 5;
                CreateStorageSlot(window.transform, $"BackpackSlot_{index + 1}", new Vector2(28f + column * 112f, -145f - row * 112f), new Vector2(104f, 100f), true, index);
            }
            for (var index = 0; index < storageChestViews.Length; index++)
            {
                var column = index % 6;
                var row = index / 6;
                CreateStorageSlot(window.transform, $"ChestSlot_{index + 1}", new Vector2(690f + column * 120f, -145f - row * 108f), new Vector2(112f, 96f), false, index);
            }
            storageFeedbackText = CreateText("Feedback", window.transform, "", 16, FontStyle.Bold, AccentColor, new Vector2(28f, -690f), new Vector2(1380f, 30f), TextAnchor.MiddleCenter);
        }

        private void CreateStorageSlot(Transform parent, string name, Vector2 position, Vector2 size, bool backpack, int index)
        {
            var slot = CreatePanel(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size, new Vector2(0f, 1f), EmptySlotColor);
            var icon = CreateText("Icon", slot.transform, "", 24, FontStyle.Bold, Color.white, new Vector2(6f, -8f), new Vector2(32f, 34f), TextAnchor.MiddleCenter);
            var label = CreateText("Name", slot.transform, "", 11, FontStyle.Bold, Color.white, new Vector2(4f, -50f), new Vector2(size.x - 8f, 24f), TextAnchor.MiddleCenter);
            var count = CreateText("Count", slot.transform, "", 13, FontStyle.Bold, AccentColor, new Vector2(size.x - 46f, -10f), new Vector2(38f, 22f), TextAnchor.MiddleRight);
            var view = slot.AddComponent<FarmStorageSlotView>();
            view.Initialize(this, backpack, null);
            if (backpack)
            {
                storageBackpackBackgrounds[index] = slot.GetComponent<Image>();
                storageBackpackIcons[index] = icon;
                storageBackpackNames[index] = label;
                storageBackpackCounts[index] = count;
                storageBackpackViews[index] = view;
            }
            else
            {
                storageChestBackgrounds[index] = slot.GetComponent<Image>();
                storageChestIcons[index] = icon;
                storageChestNames[index] = label;
                storageChestCounts[index] = count;
                storageChestViews[index] = view;
            }
        }
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var eventObject = new GameObject("Farm_EventSystem");
            eventObject.transform.SetParent(transform, false);
            eventObject.AddComponent<EventSystem>();
            eventObject.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color, Vector2 position, Vector2 dimensions, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = FarmLocalization.Get(value, value);
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            buttonObject.GetComponent<Image>().color = new Color(0.28f, 0.38f, 0.16f, 1f);
            var button = buttonObject.GetComponent<Button>();
            var labelText = CreateText("Label", buttonObject.transform, label, 15, FontStyle.Bold, Color.white, Vector2.zero, size, TextAnchor.MiddleCenter);
            labelText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            labelText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            labelText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return button;
        }
    }
}
