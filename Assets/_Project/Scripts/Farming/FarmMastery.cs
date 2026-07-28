using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmPrototype.Farming
{
    public enum FarmMasterySkill
    {
        Cultivation,
        Harvesting,
        Commerce
    }

    /// <summary>
    /// A committed co-op role. Every skill can reach level 2, but only one
    /// specialization can progress to level 3 in this prototype profile.
    /// </summary>
    public enum FarmSpecialization
    {
        None,
        Cultivation,
        Harvesting,
        Commerce
    }

    [Serializable]
    public sealed class FarmMasteryProgress
    {
        public int CultivationExperience;
        public int HarvestingExperience;
        public int CommerceExperience;
        public int FreeCultivationDay;
        public FarmSpecialization Specialization;

        public int GetExperience(FarmMasterySkill skill) => skill switch
        {
            FarmMasterySkill.Cultivation => CultivationExperience,
            FarmMasterySkill.Harvesting => HarvestingExperience,
            FarmMasterySkill.Commerce => CommerceExperience,
            _ => 0
        };

        public void AddExperience(FarmMasterySkill skill, int amount)
        {
            amount = Mathf.Max(0, amount);
            if (skill == FarmMasterySkill.Cultivation) CultivationExperience += amount;
            else if (skill == FarmMasterySkill.Harvesting) HarvestingExperience += amount;
            else if (skill == FarmMasterySkill.Commerce) CommerceExperience += amount;
        }

        public void SetExperience(FarmMasterySkill skill, int value)
        {
            value = Mathf.Max(0, value);
            if (skill == FarmMasterySkill.Cultivation) CultivationExperience = value;
            else if (skill == FarmMasterySkill.Harvesting) HarvestingExperience = value;
            else if (skill == FarmMasterySkill.Commerce) CommerceExperience = value;
        }

        public FarmMasteryProgress Clone() => new()
        {
            CultivationExperience = CultivationExperience,
            HarvestingExperience = HarvestingExperience,
            CommerceExperience = CommerceExperience,
            FreeCultivationDay = FreeCultivationDay,
            Specialization = Specialization
        };
    }

    public static class FarmMasteryRules
    {
        public const int LevelTwoExperience = 12;
        public const int LevelThreeExperience = 36;
        public const float SkilledMagnetDistance = 4f;

        public static int LevelForExperience(int experience) =>
            experience >= LevelThreeExperience ? 3 : experience >= LevelTwoExperience ? 2 : 1;

        public static int NextThreshold(int level) => level switch
        {
            1 => LevelTwoExperience,
            2 => LevelThreeExperience,
            _ => LevelThreeExperience
        };

        public static string DisplayName(FarmMasterySkill skill) => skill switch
        {
            FarmMasterySkill.Cultivation => FarmLocalization.Get("mastery.cultivation.name", "CULTIVATION"),
            FarmMasterySkill.Harvesting => FarmLocalization.Get("mastery.harvesting.name", "HARVESTING"),
            FarmMasterySkill.Commerce => FarmLocalization.Get("mastery.commerce.name", "COMMERCE"),
            _ => skill.ToString().ToUpperInvariant()
        };

        public static FarmSpecialization SpecializationFor(FarmMasterySkill skill) => skill switch
        {
            FarmMasterySkill.Cultivation => FarmSpecialization.Cultivation,
            FarmMasterySkill.Harvesting => FarmSpecialization.Harvesting,
            FarmMasterySkill.Commerce => FarmSpecialization.Commerce,
            _ => FarmSpecialization.None
        };

        public static bool TryGetSkill(FarmSpecialization specialization, out FarmMasterySkill skill)
        {
            skill = specialization switch
            {
                FarmSpecialization.Cultivation => FarmMasterySkill.Cultivation,
                FarmSpecialization.Harvesting => FarmMasterySkill.Harvesting,
                FarmSpecialization.Commerce => FarmMasterySkill.Commerce,
                _ => FarmMasterySkill.Cultivation
            };
            return specialization != FarmSpecialization.None;
        }

        public static string Description(FarmMasterySkill skill) => skill switch
        {
            FarmMasterySkill.Cultivation => FarmLocalization.Get("mastery.cultivation.description", "Prepare, plant, and water plot tiles."),
            FarmMasterySkill.Harvesting => FarmLocalization.Get("mastery.harvesting.description", "Harvest crops and find items in the world."),
            FarmMasterySkill.Commerce => FarmLocalization.Get("mastery.commerce.description", "Sell, buy seeds, and deliver orders."),
            _ => string.Empty
        };

        public static string LevelTwoPerk(FarmMasterySkill skill) => skill switch
        {
            FarmMasterySkill.Cultivation => FarmLocalization.Get("mastery.cultivation.perk2", "FIRST WIND: the first cultivation action of the day costs no energy."),
            FarmMasterySkill.Harvesting => FarmLocalization.Get("mastery.harvesting.perk2", "KEEN EYE: world items are drawn from 4 units away."),
            FarmMasterySkill.Commerce => FarmLocalization.Get("mastery.commerce.perk2", "LOCAL SCHEDULE: the board reveals tomorrow's requested crops."),
            _ => string.Empty
        };

        public static string LevelThreePerk(FarmMasterySkill skill) => skill switch
        {
            FarmMasterySkill.Cultivation => FarmLocalization.Get("mastery.cultivation.perk3", "CONTINUOUS CYCLE: harvesting replants the same crop when a seed is available."),
            FarmMasterySkill.Harvesting => FarmLocalization.Get("mastery.harvesting.perk3", "SUPPORT BASKET: harvests without room go straight to storage."),
            FarmMasterySkill.Commerce => FarmLocalization.Get("mastery.commerce.perk3", "CONNECTED STOCK: orders also use products stored in farm storage."),
            _ => string.Empty
        };
    }

    public sealed class FarmMasteryMenu : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.055f, 0.075f, 0.05f, 0.97f);
        private static readonly Color AccentColor = new(0.95f, 0.67f, 0.18f, 1f);
        private static readonly FarmMasterySkill[] Skills =
        {
            FarmMasterySkill.Cultivation,
            FarmMasterySkill.Harvesting,
            FarmMasterySkill.Commerce
        };

        private FarmHudController hud;
        private FarmGameState state;
        private Font font;
        private GameObject window;
        private CanvasGroup group;
        private readonly Text[] cardTexts = new Text[3];
        private Text focusText;
        private Text roleText;
        private bool open;

        public bool IsOpen => open;
        public string CardText(int index) => index >= 0 && index < cardTexts.Length && cardTexts[index] != null ? cardTexts[index].text : string.Empty;

        public void Initialize(FarmHudController owner, FarmGameState gameState)
        {
            if (window != null) return;
            hud = owner;
            state = gameState;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = hud != null ? hud.GetComponentInChildren<Canvas>() : null;
            if (canvas == null) throw new InvalidOperationException("Canvas da fazenda ausente para o dom\u00EDnio.");
            CreateLauncher(canvas.transform);
            CreateWindow(canvas.transform);
            SetOpen(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.kKey.wasPressedThisFrame) SetOpen(!open);
            else if (open && keyboard.escapeKey.wasPressedThisFrame) SetOpen(false);
            if (!open) return;
            if (hud.IsInventoryOpen || hud.IsStorageOpen || hud.IsJournalOpen || hud.IsSleepConfirmationOpen || hud.IsDailyOrdersOpen || hud.IsSettingsOpen || hud.IsEconomyLedgerOpen || hud.IsCraftingOpen)
            {
                SetOpen(false);
                return;
            }
            var roleModifier = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            if (roleModifier && keyboard.digit1Key.wasPressedThisFrame) TrySelectCoopRole(FarmSpecialization.Cultivation);
            else if (roleModifier && keyboard.digit2Key.wasPressedThisFrame) TrySelectCoopRole(FarmSpecialization.Harvesting);
            else if (roleModifier && keyboard.digit3Key.wasPressedThisFrame) TrySelectCoopRole(FarmSpecialization.Commerce);
            else if (keyboard.digit1Key.wasPressedThisFrame) TrySelectSpecialization(FarmSpecialization.Cultivation);
            else if (keyboard.digit2Key.wasPressedThisFrame) TrySelectSpecialization(FarmSpecialization.Harvesting);
            else if (keyboard.digit3Key.wasPressedThisFrame) TrySelectSpecialization(FarmSpecialization.Commerce);
            Refresh();
        }

        private void OnDisable()
        {
            if (hud != null) hud.SetMasteryOpen(false);
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
            hud.SetMasteryOpen(value);
            if (value)
            {
                Refresh();
                window.transform.SetAsLastSibling();
            }
        }

        private void Refresh()
        {
            if (state == null) return;
            if (focusText != null)
            {
                focusText.text = state.Specialization == FarmSpecialization.None
                    ? FarmLocalization.Get("mastery.focus.none", "NO FOCUS SELECTED - reach level 2 in a track, then press 1, 2, or 3 to focus it.")
                    : FarmLocalization.Format("mastery.focus.current", "CURRENT FOCUS: {0}  (+25% XP)", FarmMasteryRules.DisplayName(FarmMasteryRules.TryGetSkill(state.Specialization, out var focusedSkill) ? focusedSkill : FarmMasterySkill.Cultivation));
            }
            if (roleText != null)
            {
                var profile = state.GetCoopRoleProfile(LocalPlayerId);
                roleText.text = profile == null || profile.Role == FarmSpecialization.None
                    ? FarmLocalization.Get("roles.current.none", "YOUR CO-OP ROLE: UNASSIGNED — hold Shift and press 1, 2, or 3 to choose.")
                    : FarmLocalization.Format("roles.current", "YOUR CO-OP ROLE: {0} — {1} matching order contribution(s).", FarmCoopRoleRules.DisplayName(profile.Role), profile.MatchingOrderContributions);
            }
            for (var index = 0; index < Skills.Length; index++)
            {
                var skill = Skills[index];
                var experience = state.GetMasteryExperience(skill);
                var level = state.GetMasteryLevel(skill);
                var progress = level >= 3
                    ? FarmLocalization.Get("mastery.maxed", "MAX MASTERY")
                    : FarmLocalization.Format("mastery.progress", "{0}/{1} XP", experience, FarmMasteryRules.NextThreshold(level));
                var levelTwo = level >= 2 ? "\u2713" : "\u2022";
                var levelThree = level >= 3 ? "\u2713" : "\u2022";
                var specialization = FarmMasteryRules.SpecializationFor(skill);
                var focusStatus = state.Specialization == specialization
                    ? FarmLocalization.Get("mastery.focus.selected", "FOCUS ACTIVE  +25% XP")
                    : level >= 2
                        ? FarmLocalization.Format("mastery.focus.select", "[{0}] SET AS FOCUS  +25% XP", index + 1)
                        : FarmLocalization.Format("mastery.focus.locked", "[{0}] FOCUS UNLOCKS AT LEVEL 2", index + 1);
                if (cardTexts[index] == null && window != null)
                {
                    var cardText = window.transform.Find($"MasteryPanel/MasteryCard_{index + 1}/Text");
                    if (cardText != null) cardTexts[index] = cardText.GetComponent<Text>();
                }
                if (cardTexts[index] == null) continue;
                cardTexts[index].text = FarmLocalization.Format("mastery.card", "{0}  -  LEVEL {1}\n{2}  {3}\n\n{4} L2  {5}\n{6} L3  {7}\n\n{8}",
                    FarmMasteryRules.DisplayName(skill), level, FarmMasteryRules.Description(skill), progress,
                    levelTwo, FarmMasteryRules.LevelTwoPerk(skill), levelThree, FarmMasteryRules.LevelThreePerk(skill), focusStatus);
            }
        }

        private void TrySelectSpecialization(FarmSpecialization specialization)
        {
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.Progression, "local", $"specialization={specialization}");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            if (state == null) return;
            if (!state.TrySetSpecialization(specialization, out var error))
            {
                hud?.ShowSystemToast(error, true);
                return;
            }
            hud?.ShowSystemToast(FarmLocalization.Format("mastery.focus.changed", "Focus set to {0}. Focused work earns 25% more XP.", FarmMasteryRules.DisplayName(FarmMasteryRules.TryGetSkill(specialization, out var skill) ? skill : FarmMasterySkill.Cultivation)), false);
            Refresh();
        }

        private string LocalPlayerId => GetComponent<FarmTestPlot>() != null ? GetComponent<FarmTestPlot>().PlayerName : "Player";

        private void TrySelectCoopRole(FarmSpecialization role)
        {
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.PlayerRole, LocalPlayerId, $"role={role}");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            if (state == null) return;
            if (!state.TrySetCoopRole(LocalPlayerId, role, out var error))
            {
                hud?.ShowSystemToast(error, true);
                return;
            }
            hud?.ShowSystemToast(FarmLocalization.Format("roles.set", "Co-op role set: {0}.", FarmCoopRoleRules.DisplayName(role)));
            Refresh();
        }

        private void CreateLauncher(Transform root)
        {
            var button = CreateButton("OpenMastery", root, "mastery.launch", new Vector2(1f, 1f), new Vector2(-18f, -334f), new Vector2(210f, 44f), new Vector2(1f, 1f));
            button.onClick.AddListener(() => SetOpen(true));
        }

        private void CreateWindow(Transform root)
        {
            window = CreatePanel("MasteryWindow", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.015f, 0.80f));
            var backdrop = window.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            group = window.AddComponent<CanvasGroup>();
            var panel = CreatePanel("MasteryPanel", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 720f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("Title", panel.transform, "mastery.title", 30, FontStyle.Bold, AccentColor, new Vector2(30f, -22f), new Vector2(700f, 42f));
            CreateText("Subtitle", panel.transform, "mastery.subtitle", 16, FontStyle.Normal, new Color(0.78f, 0.84f, 0.74f), new Vector2(30f, -66f), new Vector2(850f, 28f));
            focusText = CreateText("Focus", panel.transform, string.Empty, 14, FontStyle.Bold, new Color(0.54f, 0.86f, 0.54f), new Vector2(30f, -94f), new Vector2(1020f, 28f));
            roleText = CreateText("Role", panel.transform, string.Empty, 14, FontStyle.Bold, new Color(0.52f, 0.78f, 0.96f), new Vector2(30f, -121f), new Vector2(1020f, 28f));
            var close = CreateButton("CloseMastery", panel.transform, "mastery.close", new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(160f, 44f), new Vector2(1f, 1f));
            close.onClick.AddListener(() => SetOpen(false));
            for (var index = 0; index < Skills.Length; index++)
            {
                var card = CreatePanel($"MasteryCard_{index + 1}", panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -154f - (index * 184f)), new Vector2(1060f, 174f), new Vector2(0f, 1f), new Color(0.11f, 0.14f, 0.09f, 1f));
                cardTexts[index] = CreateText("Text", card.transform, string.Empty, 15, FontStyle.Normal, Color.white, new Vector2(22f, -12f), new Vector2(1016f, 156f));
            }
            SetOpen(false);
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
