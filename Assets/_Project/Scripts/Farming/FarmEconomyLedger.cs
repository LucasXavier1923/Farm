using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmPrototype.Farming
{
    /// <summary>Small runtime report used to tune the first-week economy.</summary>
    public static class FarmEconomyDiagnostics
    {
        public static string BuildFirstWeekReport(FarmGameState state)
        {
            var builder = new StringBuilder();
            builder.AppendLine(FarmLocalization.Get("economy.ledger.crop_header", "CROP ECONOMY - one full seed pack"));
            foreach (var crop in FarmContentDatabase.Crops)
            {
                if (crop == null || crop.HarvestItem == null) continue;
                var packAmount = FarmEconomyRules.SeedPackAmount(crop);
                var packCost = FarmEconomyRules.SeedPackPrice(crop);
                var normalYield = crop.HarvestYield;
                var preferredYield = crop.HarvestYieldForSeason(crop.PreferredSeason);
                var unitValue = FarmEconomyRules.BaseSellPrice(crop.HarvestItem);
                var normalRevenue = packAmount * normalYield * unitValue;
                var preferredRevenue = packAmount * preferredYield * unitValue;
                var quote = state != null ? state.GetMarketQuote(crop.HarvestItem.Id) : FarmMarketRules.Quote(FarmGameState.DefaultWorldSeed, 1, crop.HarvestItem.Id);
                builder.AppendLine(FarmLocalization.Format(
                    "economy.ledger.crop_row",
                    "{0}: pack ${1} / {2} seeds | yield {3}-{4} | value ${5} | expected ${6}-${7} | profit ${8}-${9} | {10}",
                    crop.LocalizedName,
                    packCost,
                    packAmount,
                    normalYield,
                    preferredYield,
                    unitValue,
                    normalRevenue,
                    preferredRevenue,
                    normalRevenue - packCost,
                    preferredRevenue - packCost,
                    quote.CompactText));
            }

            if (state != null)
            {
                var toolCost = state.GetToolUpgradeCost(FarmTool.Hoe);
                var landCost = state.GetLandUpgradeCost();
                builder.AppendLine();
                builder.Append(FarmLocalization.Format(
                    "economy.ledger.targets",
                    "NEXT INVESTMENTS - Hoe: ${0} | Land: ${1} | Board bonus: ${2}",
                    toolCost,
                    landCost,
                    FarmEconomyRules.BoardCompletionBonus));
            }
            return builder.ToString().TrimEnd();
        }
    }

    public sealed class FarmEconomyLedgerMenu : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.055f, 0.075f, 0.05f, 0.97f);
        private static readonly Color AccentColor = new(0.95f, 0.67f, 0.18f, 1f);

        private FarmHudController hud;
        private FarmGameState state;
        private Font font;
        private GameObject window;
        private CanvasGroup group;
        private Text reportText;
        private bool open;

        public bool IsOpen => open;
        public string ReportText => reportText != null ? reportText.text : string.Empty;

        public void Initialize(FarmHudController owner, FarmGameState gameState)
        {
            if (window != null) return;
            hud = owner;
            state = gameState;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = hud != null ? hud.GetComponentInChildren<Canvas>() : null;
            if (canvas == null) throw new InvalidOperationException("Farm canvas is missing for the economy ledger.");
            CreateLauncher(canvas.transform);
            CreateWindow(canvas.transform);
            SetOpen(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f9Key.wasPressedThisFrame) SetOpen(!open);
            else if (open && keyboard.escapeKey.wasPressedThisFrame) SetOpen(false);
            if (!open) return;
            if (hud.IsInventoryOpen || hud.IsStorageOpen || hud.IsJournalOpen || hud.IsSleepConfirmationOpen || hud.IsDailyOrdersOpen || hud.IsSettingsOpen || hud.IsMasteryOpen || hud.IsCraftingOpen)
            {
                SetOpen(false);
                return;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (hud != null) hud.SetEconomyLedgerOpen(false);
        }

        public void SetOpen(bool value)
        {
            if (hud == null || group == null) return;
            if (value)
            {
                hud.SetInventoryOpen(false);
                hud.SetStorageOpen(false);
                hud.SetJournalOpen(false);
                hud.SetSleepConfirmationOpen(false);
                hud.SetDailyOrdersOpen(false);
            }
            open = value;
            group.alpha = value ? 1f : 0f;
            group.interactable = value;
            group.blocksRaycasts = value;
            hud.SetEconomyLedgerOpen(value);
            if (value)
            {
                Refresh();
                window.transform.SetAsLastSibling();
            }
        }

        private void Refresh()
        {
            if (reportText != null) reportText.text = FarmEconomyDiagnostics.BuildFirstWeekReport(state);
        }

        private void CreateLauncher(Transform root)
        {
            var button = CreateButton("OpenEconomyLedger", root, "economy.ledger.launch", new Vector2(1f, 1f), new Vector2(-18f, -382f), new Vector2(210f, 44f), new Vector2(1f, 1f));
            button.onClick.AddListener(() => SetOpen(true));
        }

        private void CreateWindow(Transform root)
        {
            window = CreatePanel("EconomyLedgerWindow", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.015f, 0.80f));
            var backdrop = window.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            group = window.AddComponent<CanvasGroup>();
            var panel = CreatePanel("EconomyLedgerPanel", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 610f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("Title", panel.transform, "economy.ledger.title", 30, FontStyle.Bold, AccentColor, new Vector2(30f, -22f), new Vector2(700f, 42f));
            CreateText("Subtitle", panel.transform, "economy.ledger.subtitle", 16, FontStyle.Normal, new Color(0.78f, 0.84f, 0.74f), new Vector2(30f, -66f), new Vector2(900f, 28f));
            reportText = CreateText("Report", panel.transform, string.Empty, 16, FontStyle.Normal, Color.white, new Vector2(30f, -112f), new Vector2(1060f, 430f));
            var close = CreateButton("CloseEconomyLedger", panel.transform, "economy.ledger.close", new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(160f, 44f), new Vector2(1f, 1f));
            close.onClick.AddListener(() => SetOpen(false));
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot, Color color)
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

        private Text CreateText(string name, Transform parent, string content, int size, FontStyle style, Color color, Vector2 position, Vector2 dimensions)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Text));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = item.GetComponent<Text>();
            text.font = font;
            text.text = FarmLocalization.Get(content, content);
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            var item = CreatePanel(name, parent, anchor, anchor, position, size, pivot, new Color(0.18f, 0.23f, 0.14f, 1f));
            var button = item.AddComponent<Button>();
            var text = CreateText("Label", item.transform, label, 16, FontStyle.Bold, Color.white, Vector2.zero, size);
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
