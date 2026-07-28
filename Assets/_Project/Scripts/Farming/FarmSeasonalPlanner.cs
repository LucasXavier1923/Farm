using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// A read-only planning screen. It contains no local simulation state, so the
    /// host calendar and crop data remain the single source of truth in co-op.
    /// </summary>
    public sealed class FarmSeasonalPlanner : MonoBehaviour
    {
        private FarmTestPlot plot;
        private FarmHudController hud;
        private FarmDayClock clock;
        private CanvasGroup group;
        private GameObject window;
        private Text seasonText;
        private Text cropText;
        private Text greenhouseText;
        private Text forecastText;
        private Text planText;
        private Button prepareForecastButton;
        private Font font;

        public bool IsOpen { get; private set; }
        public string CropPlanText => cropText != null ? cropText.text : string.Empty;

        public void Initialize(FarmTestPlot owner, FarmHudController ownerHud)
        {
            if (window != null) return;
            plot = owner;
            hud = ownerHud;
            clock = owner != null ? owner.DayClock : null;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateInterface();
            Close();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.pKey.wasPressedThisFrame)
            {
                if (IsOpen) Close(); else Open();
                return;
            }
            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) Close();
            if (IsOpen) Refresh();
        }

        public bool Open()
        {
            if (IsOpen) return true;
            if (FarmHudController.IsModalOpen) return false;
            IsOpen = true;
            hud?.SetSeasonalPlannerOpen(true);
            SetVisible(true);
            window.transform.SetAsLastSibling();
            Refresh();
            return true;
        }

        public void Close()
        {
            IsOpen = false;
            SetVisible(false);
            hud?.SetSeasonalPlannerOpen(false);
        }

        public void Refresh()
        {
            if (clock == null && plot != null) clock = plot.DayClock;
            var current = clock != null ? clock.CurrentSeason : FarmSeason.Spring;
            var day = clock != null ? clock.DayOfSeason : 1;
            seasonText.text = FarmLocalization.Format("planner.season", "YEAR {0}  -  {1}  DAY {2}/{3}", clock != null ? clock.YearNumber : 1, FarmDayClock.SeasonName(current), day, FarmDayClock.DaysPerSeason);
            var plan = new StringBuilder();
            foreach (var crop in FarmContentDatabase.Crops)
            {
                if (crop == null) continue;
                var inSeason = crop.PreferredSeason == current;
                var status = inSeason
                    ? FarmLocalization.Get("planner.crop.in_season", "PLANT OUTDOORS")
                    : FarmLocalization.Get("planner.crop.greenhouse", "GREENHOUSE REQUIRED");
                plan.Append(FarmLocalization.Format("planner.crop.row", "{0}  -  {1}  -  {2}\n", crop.LocalizedName, FarmDayClock.SeasonName(crop.PreferredSeason), status));
            }
            cropText.text = plan.ToString().TrimEnd();
            greenhouseText.text = FarmLocalization.Get("planner.greenhouse", "Greenhouse coverage allows off-season planting and applies the crop's preferred-season harvest result while it remains placed.");
            var state = plot != null ? plot.GameState : null;
            var tomorrow = state != null ? FarmWeatherSystem.WeatherForDay(state.WorldSeed, state.DayNumber + 1) : FarmWeather.Clear;
            var routeKey = FarmForecastPlanRules.RouteKeyForWeather(tomorrow);
            forecastText.text = FarmLocalization.Format("forecast.tomorrow", "TOMORROW: {0}", FarmWeatherSystem.WeatherName(tomorrow));
            var planned = state != null && state.HasForecastPlanForRoute(state.DayNumber + 1, routeKey);
            planText.text = planned
                ? FarmLocalization.Format("forecast.plan.active", "PREPARED: {0}", FarmForecastPlanRules.Description(routeKey))
                : FarmForecastPlanRules.Description(routeKey);
            if (prepareForecastButton != null) prepareForecastButton.interactable = !planned;
        }

        private void TryPrepareForecastPlan()
        {
            var state = plot != null ? plot.GameState : null;
            if (state == null) return;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.ForecastPlan, plot.PlayerName, "action=prepare_tomorrow_route");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            if (state.TryPrepareTomorrowForecastPlan(out var routeKey, out var error))
                hud?.ShowSystemToast(FarmLocalization.Format("forecast.plan.confirmed", "Forecast plan prepared: {0}.", FarmForecastPlanRules.Description(routeKey)));
            else hud?.ShowSystemToast(error, true);
            Refresh();
        }

        private void CreateInterface()
        {
            var root = new GameObject("Farm_Seasonal_Planner_UI");
            root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 128;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            root.AddComponent<GraphicRaycaster>();

            var open = CreateButton(root.transform, FarmLocalization.Get("planner.launch", "PLANNER  [P]"), new Vector2(-18f, -438f), new Vector2(210f, 48f));
            var openRect = open.GetComponent<RectTransform>();
            openRect.anchorMin = Vector2.one; openRect.anchorMax = Vector2.one; openRect.pivot = Vector2.one;
            open.onClick.AddListener(() => { if (IsOpen) Close(); else Open(); });

            window = CreatePanel(root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.01f, 0.78f));
            group = window.AddComponent<CanvasGroup>();
            var panel = CreatePanel(window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(800f, 600f), new Color(0.055f, 0.085f, 0.045f, 0.99f));
            CreateText(panel.transform, FarmLocalization.Get("planner.title", "SEASONAL PLANNER"), new Vector2(34f, -28f), new Vector2(520f, 42f), 30, FontStyle.Bold, new Color(0.96f, 0.75f, 0.24f));
            seasonText = CreateText(panel.transform, string.Empty, new Vector2(36f, -78f), new Vector2(650f, 28f), 17, FontStyle.Bold, new Color(0.50f, 0.88f, 0.48f));
            CreateText(panel.transform, FarmLocalization.Get("planner.headers", "CROP / PREFERRED SEASON / ACTION"), new Vector2(36f, -120f), new Vector2(690f, 25f), 14, FontStyle.Bold, new Color(0.76f, 0.84f, 0.70f));
            cropText = CreateText(panel.transform, string.Empty, new Vector2(36f, -154f), new Vector2(720f, 145f), 18, FontStyle.Normal, Color.white);
            greenhouseText = CreateText(panel.transform, string.Empty, new Vector2(36f, -306f), new Vector2(720f, 48f), 13, FontStyle.Normal, new Color(0.72f, 0.83f, 0.68f));
            forecastText = CreateText(panel.transform, string.Empty, new Vector2(36f, -365f), new Vector2(720f, 26f), 16, FontStyle.Bold, new Color(0.50f, 0.82f, 0.96f));
            planText = CreateText(panel.transform, string.Empty, new Vector2(36f, -398f), new Vector2(720f, 48f), 14, FontStyle.Normal, new Color(0.84f, 0.90f, 0.72f));
            prepareForecastButton = CreateButton(panel.transform, FarmLocalization.Get("forecast.plan.button", "PREPARE TOMORROW'S ROUTE"), new Vector2(36f, -462f), new Vector2(330f, 46f));
            prepareForecastButton.onClick.AddListener(TryPrepareForecastPlan);
            var close = CreateButton(panel.transform, FarmLocalization.Get("ui.close.esc", "CLOSE  [ESC]"), new Vector2(592f, -522f), new Vector2(170f, 46f));
            close.onClick.AddListener(Close);
        }

        private GameObject CreatePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private Text CreateText(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = font; text.text = value; text.fontSize = fontSize; text.fontStyle = style; text.color = color; text.alignment = TextAnchor.UpperLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button CreateButton(Transform parent, string value, Vector2 position, Vector2 size)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = position; rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.22f, 0.38f, 0.14f, 1f);
            var text = CreateText(go.transform, value, Vector2.zero, size, 15, FontStyle.Bold, Color.white);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.pivot = new Vector2(0.5f, 0.5f); text.rectTransform.anchoredPosition = Vector2.zero; text.rectTransform.sizeDelta = Vector2.zero; text.alignment = TextAnchor.MiddleCenter;
            return go.GetComponent<Button>();
        }

        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
