using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Local development console for exercising farm systems without editing a save.
    /// It is compiled into all targets but only creates UI in the Editor or a
    /// Development Build, so it cannot become a player-facing production feature.
    /// </summary>
    public sealed class FarmDeveloperPanel : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.035f, 0.055f, 0.04f, 0.98f);
        private static readonly Color SectionColor = new(0.09f, 0.13f, 0.08f, 0.96f);
        private static readonly Color ButtonColor = new(0.20f, 0.34f, 0.18f, 1f);
        private static readonly Color AccentColor = new(1f, 0.73f, 0.22f, 1f);

        private FarmTestPlot plot;
        private Font font;
        private GameObject root;
        private Text statusText;
        private Text itemText;
        private Text pestText;
        private readonly List<ItemDefinition> items = new();
        private int selectedItemIndex;
        private bool isOpen;

        public bool IsOpen => isOpen;

        public void Initialize(FarmTestPlot owner)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (root != null) return;
            plot = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RefreshItems();
            CreateInterface();
            SetOpen(false);
#else
            enabled = false;
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current == null || root == null) return;
            if (Keyboard.current.hKey.wasPressedThisFrame) SetOpen(!isOpen);
            if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame) SetOpen(false);
            if (isOpen) RefreshReadout();
#endif
        }

        private void SetOpen(bool value)
        {
            isOpen = value;
            if (root != null) root.SetActive(value);
            if (value) RefreshReadout();
        }

        private void RefreshItems()
        {
            items.Clear();
            items.AddRange(FarmContentDatabase.Items.Where(item => item != null).OrderBy(item => item.LocalizedName, StringComparer.OrdinalIgnoreCase));
            selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, Mathf.Max(0, items.Count - 1));
        }

        private void RefreshReadout()
        {
            if (plot == null) plot = GetComponent<FarmTestPlot>();
            var state = plot != null ? plot.GameState : null;
            if (state == null || plot.DayClock == null)
            {
                if (statusText != null) statusText.text = L("dev.status.waiting", "Waiting for farm state...");
                return;
            }

            var weather = plot.WeatherSystem;
            var weatherSuffix = weather != null && weather.HasDeveloperWeatherOverride
                ? L("dev.status.weather_override", " (override)")
                : string.Empty;
            statusText.text = string.Format(
                L("dev.status", "Day {0}  {1}  •  {2}  •  {3}{4}\nMoney ${5}  •  Energy {6}/{7}  •  Speed x{8:0.#}"),
                state.DayNumber,
                FarmDayClock.FormatTime(state.MinutesOfDay),
                FarmDayClock.SeasonName(plot.DayClock.CurrentSeason),
                weather != null ? FarmWeatherSystem.WeatherName(weather.CurrentWeather) : L("dev.weather.unknown", "Unknown"),
                weatherSuffix,
                state.Money,
                state.Energy,
                FarmGameState.MaxEnergy,
                plot.DayClock.SimulationSpeed);

            var pestMode = FarmPestRules.DeveloperVisitOverride switch
            {
                true => L("dev.pests.forced_on", "FORCED ON"),
                false => L("dev.pests.forced_off", "FORCED OFF"),
                _ => L("dev.pests.calendar", "CALENDAR")
            };
            pestText.text = string.Format(L("dev.pests.status", "Pests: {0}  •  Today: {1}"), pestMode, plot.PestThreatToday ? L("common.yes", "YES") : L("common.no", "NO"));

            if (items.Count == 0) RefreshItems();
            if (items.Count == 0)
            {
                itemText.text = L("dev.items.none", "No item definitions were found.");
                return;
            }

            var item = items[selectedItemIndex];
            itemText.text = string.Format(
                L("dev.items.selected", "Item {0}/{1}: {2}\nID: {3}  •  {4}  •  Owned: {5}"),
                selectedItemIndex + 1,
                items.Count,
                item.LocalizedName,
                item.Id,
                item.Category.ToString().ToUpperInvariant(),
                state.GetQuantity(item.Id));
        }

        private void CreateInterface()
        {
            var canvasObject = new GameObject("Farm_DeveloperConsole", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 900;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root = canvasObject;

            var shade = CreatePanel("Shade", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.62f));
            var shadeRect = shade.GetComponent<RectTransform>();
            shadeRect.offsetMin = Vector2.zero;
            shadeRect.offsetMax = Vector2.zero;

            var panel = CreatePanel("Panel", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1160f, 790f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("Title", panel.transform, L("dev.title", "DEVELOPER CONSOLE"), 30, FontStyle.Bold, AccentColor, new Vector2(30f, -22f), new Vector2(560f, 40f));
            CreateText("Hint", panel.transform, L("dev.hint", "H: toggle  •  ESC: close  •  Changes apply to the local host save."), 15, FontStyle.Normal, new Color(0.78f, 0.88f, 0.72f), new Vector2(30f, -62f), new Vector2(820f, 28f));
            var close = CreateButton("Close", panel.transform, L("common.close", "CLOSE"), new Vector2(1000f, -22f), new Vector2(130f, 42f));
            close.onClick.AddListener(() => SetOpen(false));
            statusText = CreateText("Status", panel.transform, string.Empty, 17, FontStyle.Bold, Color.white, new Vector2(30f, -96f), new Vector2(1090f, 58f));

            var time = CreateSection(panel.transform, "Time", L("dev.time", "TIME & DAY"), new Vector2(28f, -170f), new Vector2(530f, 190f));
            AddButton(time.transform, "PlusHour", L("dev.time.plus_hour", "+ 1 HOUR"), new Vector2(18f, -46f), () => plot?.DayClock?.AdvanceMinutes(60f));
            AddButton(time.transform, "Morning", L("dev.time.morning", "06:00"), new Vector2(184f, -46f), () => SetTime(360f));
            AddButton(time.transform, "Noon", L("dev.time.noon", "12:00"), new Vector2(350f, -46f), () => SetTime(720f));
            AddButton(time.transform, "Evening", L("dev.time.evening", "18:00"), new Vector2(18f, -98f), () => SetTime(1080f));
            AddButton(time.transform, "Night", L("dev.time.night", "00:00"), new Vector2(184f, -98f), () => SetTime(0f));
            AddButton(time.transform, "SkipDay", L("dev.time.skip_day", "NEXT DAY + MORNING"), new Vector2(350f, -98f), () => plot?.AdvanceDayForDebug(), 162f);
            AddButton(time.transform, "SpeedOne", L("dev.time.speed_one", "SPEED x1"), new Vector2(18f, -150f), () => plot?.DayClock?.SetSimulationSpeed(1f));
            AddButton(time.transform, "SpeedTen", L("dev.time.speed_ten", "SPEED x10"), new Vector2(184f, -150f), () => plot?.DayClock?.SetSimulationSpeed(10f));
            AddButton(time.transform, "SpeedStop", L("dev.time.speed_stop", "STOP CLOCK"), new Vector2(350f, -150f), () => plot?.DayClock?.SetSimulationSpeed(0f), 162f);

            var world = CreateSection(panel.transform, "World", L("dev.world", "WORLD CONDITIONS"), new Vector2(574f, -170f), new Vector2(558f, 190f));
            AddButton(world.transform, "Spring", L("season.spring", "Spring"), new Vector2(18f, -46f), () => plot?.SetSeasonForDebug(FarmSeason.Spring), 122f);
            AddButton(world.transform, "Summer", L("season.summer", "Summer"), new Vector2(148f, -46f), () => plot?.SetSeasonForDebug(FarmSeason.Summer), 122f);
            AddButton(world.transform, "Autumn", L("season.autumn", "Autumn"), new Vector2(278f, -46f), () => plot?.SetSeasonForDebug(FarmSeason.Autumn), 122f);
            AddButton(world.transform, "Winter", L("season.winter", "Winter"), new Vector2(408f, -46f), () => plot?.SetSeasonForDebug(FarmSeason.Winter), 122f);
            AddButton(world.transform, "Sunny", L("weather.clear", "Sunny"), new Vector2(18f, -98f), () => SetWeather(FarmWeather.Clear), 122f);
            AddButton(world.transform, "Cloudy", L("weather.cloudy", "Cloudy"), new Vector2(148f, -98f), () => SetWeather(FarmWeather.Cloudy), 122f);
            AddButton(world.transform, "Rain", L("weather.rain", "Rain"), new Vector2(278f, -98f), () => SetWeather(FarmWeather.Rain), 122f);
            AddButton(world.transform, "AutoWeather", L("dev.weather.auto", "AUTO WEATHER"), new Vector2(408f, -98f), () => plot?.WeatherSystem?.SetDeveloperWeatherOverride(null), 122f);
            pestText = CreateText("PestStatus", world.transform, string.Empty, 14, FontStyle.Bold, new Color(1f, 0.73f, 0.32f), new Vector2(18f, -148f), new Vector2(520f, 22f));
            AddButton(world.transform, "PestsOn", L("dev.pests.on", "PESTS ON"), new Vector2(18f, -174f), () => FarmPestRules.SetDeveloperVisitOverride(true), 122f);
            AddButton(world.transform, "PestsOff", L("dev.pests.off", "PESTS OFF"), new Vector2(148f, -174f), () => FarmPestRules.SetDeveloperVisitOverride(false), 122f);
            AddButton(world.transform, "PestsAuto", L("dev.pests.auto", "AUTO PESTS"), new Vector2(278f, -174f), () => FarmPestRules.SetDeveloperVisitOverride(null), 122f);
            AddButton(world.transform, "TriggerPests", L("dev.pests.trigger", "TRIGGER NOW"), new Vector2(408f, -174f), () => plot?.TriggerPestVisitForDebug(), 122f);

            var test = CreateSection(panel.transform, "Test", L("dev.test", "PLAYER & CROP TESTS"), new Vector2(28f, -376f), new Vector2(530f, 148f));
            AddButton(test.transform, "Money100", L("dev.money.100", "+ $100"), new Vector2(18f, -46f), () => plot?.GameState?.AddMoneyForDebug(100));
            AddButton(test.transform, "Money1000", L("dev.money.1000", "+ $1000"), new Vector2(184f, -46f), () => plot?.GameState?.AddMoneyForDebug(1000));
            AddButton(test.transform, "Energy", L("dev.energy.restore", "FULL ENERGY"), new Vector2(350f, -46f), () => plot?.GameState?.RestoreEnergyForDebug(), 162f);
            AddButton(test.transform, "WaterAll", L("dev.tiles.water_all", "WATER ALL"), new Vector2(18f, -98f), () => plot?.WaterAllTilesForDebug());
            AddButton(test.transform, "Grow30", L("dev.growth.30", "GROW +30s"), new Vector2(184f, -98f), () => plot?.AdvanceCropGrowthForDebug(30f));
            AddButton(test.transform, "Grow300", L("dev.growth.300", "GROW +5m"), new Vector2(350f, -98f), () => plot?.AdvanceCropGrowthForDebug(300f), 162f);

            var itemSection = CreateSection(panel.transform, "Items", L("dev.items", "ITEM SPAWNER — EVERY REGISTERED ITEM"), new Vector2(574f, -376f), new Vector2(558f, 148f));
            itemText = CreateText("Item", itemSection.transform, string.Empty, 15, FontStyle.Bold, Color.white, new Vector2(18f, -44f), new Vector2(520f, 54f));
            AddButton(itemSection.transform, "PreviousItem", L("dev.items.previous", "◀ PREVIOUS"), new Vector2(18f, -108f), () => SelectItem(-1), 122f);
            AddButton(itemSection.transform, "NextItem", L("dev.items.next", "NEXT ▶"), new Vector2(148f, -108f), () => SelectItem(1), 122f);
            AddButton(itemSection.transform, "AddOne", L("dev.items.add_one", "ADD 1"), new Vector2(278f, -108f), () => AddSelectedItem(1), 122f);
            AddButton(itemSection.transform, "AddTen", L("dev.items.add_ten", "ADD 10"), new Vector2(408f, -108f), () => AddSelectedItem(10), 122f);

            CreateText("Warning", panel.transform, L("dev.warning", "Development changes are saved locally. This console is unavailable in release builds."), 14, FontStyle.Italic, new Color(0.80f, 0.86f, 0.70f), new Vector2(30f, -748f), new Vector2(1090f, 24f), TextAnchor.MiddleCenter);
        }

        private void SetTime(float minute)
        {
            if (plot?.DayClock == null || plot.GameState == null) return;
            plot.DayClock.SetClock(plot.GameState.DayNumber, minute);
            plot.NotifyClockCheckpoint();
        }

        private void SetWeather(FarmWeather weather) => plot?.WeatherSystem?.SetDeveloperWeatherOverride(weather);

        private void SelectItem(int direction)
        {
            if (items.Count == 0) RefreshItems();
            if (items.Count == 0) return;
            selectedItemIndex = (selectedItemIndex + direction) % items.Count;
            if (selectedItemIndex < 0) selectedItemIndex += items.Count;
            RefreshReadout();
        }

        private void AddSelectedItem(int amount)
        {
            if (plot?.GameState == null || items.Count == 0) return;
            var item = items[selectedItemIndex];
            var added = plot.GameState.AddItem(item.Id, amount);
            var message = added
                ? string.Format(L("dev.items.added", "Developer: added {0} x{1}."), item.LocalizedName, amount)
                : string.Format(L("dev.items.failed", "Developer: could not add {0}; inventory may be full."), item.LocalizedName);
            plot.SetExternalPrompt(this, message);
            Invoke(nameof(ClearPrompt), 2.5f);
            RefreshReadout();
        }

        private void ClearPrompt() => plot?.SetExternalPrompt(this, null);

        private GameObject CreateSection(Transform parent, string name, string title, Vector2 position, Vector2 size)
        {
            var section = CreatePanel(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size, new Vector2(0f, 1f), SectionColor);
            CreateText("Title", section.transform, title, 16, FontStyle.Bold, AccentColor, new Vector2(18f, -14f), new Vector2(size.x - 36f, 24f));
            return section;
        }

        private void AddButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action, float width = 154f)
        {
            var button = CreateButton(name, parent, label, position, new Vector2(width, 40f));
            button.onClick.AddListener(action);
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
            text.text = value;
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
            buttonObject.GetComponent<Image>().color = ButtonColor;
            var button = buttonObject.GetComponent<Button>();
            var labelText = CreateText("Label", buttonObject.transform, label, 13, FontStyle.Bold, Color.white, Vector2.zero, size, TextAnchor.MiddleCenter);
            labelText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            labelText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            labelText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return button;
        }

        private static string L(string key, string fallback) => FarmLocalization.Get(key, fallback);
    }
}
