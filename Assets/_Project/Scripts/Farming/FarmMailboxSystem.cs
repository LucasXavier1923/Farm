using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmPrototype.Farming
{
    public enum FarmMailKind { Welcome, SeedFair, Community }

    public sealed class FarmMailDefinition
    {
        public string Id;
        public FarmMailKind Kind;
        public int DeliveredDay;
        public int EventDay;
        public string Sender;
        public string Title;
        public string Body;
        public int RewardMoney;
        public string RewardItemId;
        public int RewardQuantity;

        public bool HasReward => RewardMoney > 0 ||
            (!string.IsNullOrWhiteSpace(RewardItemId) && RewardQuantity > 0);

        public string RewardText
        {
            get
            {
                var parts = new List<string>();
                if (RewardMoney > 0) parts.Add($"+${RewardMoney}");
                if (!string.IsNullOrWhiteSpace(RewardItemId) && RewardQuantity > 0)
                {
                    var item = FarmContentDatabase.GetItem(RewardItemId);
                    parts.Add($"{(item != null ? item.LocalizedName : RewardItemId)} x{RewardQuantity}");
                }
                return parts.Count > 0 ? string.Join("  •  ", parts) : FarmLocalization.Get("mail.reward.none", "No attachment");
            }
        }
    }

    public static class FarmMailDatabase
    {
        public static List<FarmMailDefinition> GetInbox(int currentDay)
        {
            currentDay = Mathf.Max(1, currentDay);
            var result = new List<FarmMailDefinition>();
            if (currentDay >= 1) result.Add(CreateWelcome());
            var lastCycle = (currentDay - 1) / FarmDayClock.DaysPerSeason;
            for (var cycle = 0; cycle <= lastCycle; cycle++)
            {
                var seedFair = CreateSeedFair(cycle);
                var community = CreateCommunity(cycle);
                if (seedFair.DeliveredDay <= currentDay) result.Add(seedFair);
                if (community.DeliveredDay <= currentDay) result.Add(community);
            }
            result.Sort((left, right) =>
            {
                var day = right.DeliveredDay.CompareTo(left.DeliveredDay);
                return day != 0 ? day : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
            });
            return result;
        }

        public static FarmMailDefinition Get(string id, int currentDay)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var mail in GetInbox(currentDay))
                if (string.Equals(mail.Id, id, StringComparison.OrdinalIgnoreCase)) return mail;
            return null;
        }

        public static FarmMailDefinition NextEvent(int currentDay)
        {
            currentDay = Mathf.Max(1, currentDay);
            for (var day = currentDay; day <= currentDay + 14; day++)
            {
                var dayInSeason = FarmDayClock.DayInSeason(day);
                var cycle = (day - 1) / FarmDayClock.DaysPerSeason;
                if (dayInSeason == 2) return CreateSeedFair(cycle);
                if (dayInSeason == 6) return CreateCommunity(cycle);
            }
            return null;
        }

        private static FarmMailDefinition CreateWelcome() => new()
        {
            Id = "welcome_1",
            Kind = FarmMailKind.Welcome,
            DeliveredDay = 1,
            EventDay = 1,
            Sender = FarmLocalization.Get("mail.welcome.sender", "Farm Association"),
            Title = FarmLocalization.Get("mail.welcome.title", "Welcome to your new farm"),
            Body = FarmLocalization.Get("mail.welcome.body", "We prepared this mailbox for notices, gifts, and local events. Letters never expire: read at your own pace and claim each attachment once."),
            RewardMoney = 25
        };

        private static FarmMailDefinition CreateSeedFair(int cycle)
        {
            var eventDay = cycle * FarmDayClock.DaysPerSeason + 2;
            var season = FarmDayClock.SeasonForDay(eventDay);
            return new FarmMailDefinition
            {
                Id = $"seed_fair_{cycle}",
                Kind = FarmMailKind.SeedFair,
                DeliveredDay = eventDay,
                EventDay = eventDay,
                Sender = FarmLocalization.Get("mail.seed_fair.sender", "Growers' Fair"),
                Title = FarmLocalization.Format("mail.seed_fair.title", "{0} Seed Fair", FarmDayClock.SeasonName(season)),
                Body = FarmLocalization.Format("mail.seed_fair.body", "The {0} fair has arrived. We sent seeds for the crop with the best affinity for this season.", FarmDayClock.SeasonName(season)),
                RewardItemId = AffinitySeedId(season),
                RewardQuantity = 3
            };
        }

        private static FarmMailDefinition CreateCommunity(int cycle)
        {
            var eventDay = cycle * FarmDayClock.DaysPerSeason + 6;
            return new FarmMailDefinition
            {
                Id = $"community_{cycle}",
                Kind = FarmMailKind.Community,
                DeliveredDay = eventDay,
                EventDay = eventDay,
                Sender = FarmLocalization.Get("mail.community.sender", "Valley Neighbors"),
                Title = FarmLocalization.Get("mail.community.title", "Community work day"),
                Body = FarmLocalization.Get("mail.community.body", "Today we care for paths and fences. Here is a little help as you continue improving your farm."),
                RewardMoney = 15,
                RewardItemId = "wood",
                RewardQuantity = 4
            };
        }

        private static string AffinitySeedId(FarmSeason season) => season switch
        {
            FarmSeason.Spring => "strawberry_seed",
            FarmSeason.Summer => "corn_seed",
            FarmSeason.Autumn => "pumpkin_seed",
            FarmSeason.Winter => "carrot_seed",
            _ => "pumpkin_seed"
        };
    }

    public sealed class FarmMailboxSystem : MonoBehaviour
    {
        public const float InteractionDistance = 2.2f;
        private const int LettersPerPage = 5;
        private static readonly Color OverlayColor = new(0.012f, 0.018f, 0.01f, 0.76f);
        private static readonly Color PanelColor = new(0.06f, 0.082f, 0.05f, 0.98f);
        private static readonly Color CardColor = new(0.105f, 0.14f, 0.085f, 1f);
        private static readonly Color SelectedColor = new(0.31f, 0.43f, 0.16f, 1f);
        private static readonly Color AccentColor = new(0.96f, 0.70f, 0.22f, 1f);

        private readonly List<FarmMailDefinition> inbox = new();
        private readonly FarmMailDefinition[] visibleMail = new FarmMailDefinition[LettersPerPage];
        private readonly GameObject[] cardRoots = new GameObject[LettersPerPage];
        private readonly Image[] cardBackgrounds = new Image[LettersPerPage];
        private readonly Text[] cardTitles = new Text[LettersPerPage];
        private readonly Text[] cardStates = new Text[LettersPerPage];

        private FarmTestPlot plot;
        private FarmGameState state;
        private FarmHudController hud;
        private Transform player;
        private Font font;
        private GameObject mailboxObject;
        private GameObject unreadMarker;
        private Material markerMaterial;
        private GameObject window;
        private CanvasGroup windowGroup;
        private Text unreadSummary;
        private Text nextEventText;
        private Text detailSender;
        private Text detailTitle;
        private Text detailDate;
        private Text detailBody;
        private Text detailReward;
        private Text detailStatus;
        private Button claimButton;
        private Text claimButtonLabel;
        private Button previousPageButton;
        private Button nextPageButton;
        private Text pageText;
        private readonly Button[] giftButtons = new Button[3];
        private readonly Text[] giftButtonLabels = new Text[3];
        private Text giftHint;
        private string selectedMailId;
        private int currentPage;
        private float nextWorldRefresh;

        public bool IsOpen { get; private set; }
        public bool IsInRange => player != null && mailboxObject != null &&
            Vector3.Distance(player.position, mailboxObject.transform.position) <= InteractionDistance;
        public Vector3 WorldPosition => mailboxObject != null ? mailboxObject.transform.position : Vector3.zero;
        public int UnreadCount => state != null ? state.CountUnreadMail() : 0;
        public int ClaimableCount => state != null ? state.CountClaimableMail() : 0;
        public bool IsUnreadMarkerVisible => unreadMarker != null && unreadMarker.activeSelf;
        public string SelectedMailId => selectedMailId ?? string.Empty;
        public int InboxCount => inbox.Count;
        public int CurrentPage => currentPage;
        public string NextEventText => nextEventText != null ? nextEventText.text : string.Empty;

        public void Initialize(
            FarmTestPlot owner,
            FarmGameState gameState,
            FarmHudController ownerHud,
            Transform playerTransform,
            Vector3 position,
            Vector3 facing)
        {
            if (window != null) return;
            plot = owner;
            state = gameState;
            hud = ownerHud;
            player = playerTransform;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateWorld(position, facing);
            var canvas = hud != null ? hud.GetComponentInChildren<Canvas>() : null;
            if (canvas == null) throw new InvalidOperationException("Canvas is missing for the mailbox.");
            CreateInterface(canvas.transform);
            if (state != null) state.Changed += HandleStateChanged;
            RefreshAll();
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }
            if (Time.unscaledTime < nextWorldRefresh) return;
            nextWorldRefresh = Time.unscaledTime + 0.25f;
            RefreshWorldIndicator();
            RefreshNextEvent();
        }

        private void OnDisable() => Close();

        private void OnDestroy()
        {
            if (state != null) state.Changed -= HandleStateChanged;
            if (markerMaterial != null) Destroy(markerMaterial);
            if (IsOpen && hud != null) hud.SetMailboxOpen(false);
        }

        public bool Open()
        {
            if (!IsInRange)
            {
                hud?.ShowSystemToast(FarmLocalization.Get("mail.too_far", "Move closer to the mailbox."), true);
                return false;
            }
            return OpenInternal();
        }

        public bool OpenForTesting() => OpenInternal();

        private bool OpenInternal()
        {
            if (IsOpen || state == null || FarmHudController.IsModalOpen) return false;
            IsOpen = true;
            hud?.SetMailboxOpen(true);
            SetCanvasGroup(windowGroup, true);
            window.transform.SetAsLastSibling();
            currentPage = 0;
            RefreshInbox();
            var initial = FindFirstUnread() ?? (inbox.Count > 0 ? inbox[0] : null);
            if (initial != null) SelectMail(initial.Id);
            else RefreshDetail();
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            SetCanvasGroup(windowGroup, false);
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.transform.IsChildOf(window.transform))
                EventSystem.current.SetSelectedGameObject(null);
            hud?.SetMailboxOpen(false);
        }

        public bool SelectMailForTesting(string mailId)
        {
            if (!IsOpen || FarmMailDatabase.Get(mailId, state.DayNumber) == null) return false;
            SelectMail(mailId);
            return true;
        }

        public bool ClaimSelectedForTesting(out string error) => TryClaimSelected(out error);

        public void RefreshAll()
        {
            RefreshInbox();
            RefreshWorldIndicator();
            RefreshNextEvent();
            RefreshGifts();
        }

        private void SelectVisibleMail(int index)
        {
            if (index < 0 || index >= visibleMail.Length || visibleMail[index] == null) return;
            SelectMail(visibleMail[index].Id);
        }

        private void SelectMail(string mailId)
        {
            var mail = FarmMailDatabase.Get(mailId, state.DayNumber);
            if (mail == null) return;
            selectedMailId = mail.Id;
            if (FarmSessionTime.IsSimulationAuthority) state.MarkMailRead(mail.Id);
            RefreshInbox();
            RefreshDetail();
        }

        private void ClaimSelected()
        {
            if (TryClaimSelected(out var error))
                hud?.ShowSystemToast(FarmLocalization.Get("mail.claimed.feedback", "Letter attachment claimed."), false);
            else if (!string.IsNullOrWhiteSpace(error))
                hud?.ShowSystemToast(error, true);
        }

        private bool TryClaimSelected(out string error)
        {
            error = string.Empty;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                error = FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
                return false;
            }
            var mail = FarmMailDatabase.Get(selectedMailId, state.DayNumber);
            if (mail == null)
            {
                error = FarmLocalization.Get("mail.select", "Select a letter.");
                return false;
            }
            if (!state.TryClaimMail(mail, out error)) return false;
            RefreshAll();
            RefreshDetail();
            return true;
        }

        private void GiftToContact(int index)
        {
            if (index < 0 || index >= FarmCommunityCatalog.AllContacts.Count || state == null) return;
            var contact = FarmCommunityCatalog.AllContacts[index];
            var itemId = FarmCommunityCatalog.PreferredGiftId(contact.Id);
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.CommunityGift, "Player", $"contact={contact.Id};item={itemId}");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            if (state.TryGiveCommunityGift(contact.Id, itemId, out var gift, out var error))
            {
                var message = gift.ReachedMilestone
                    ? FarmLocalization.Format("gift.sent_milestone", "Gift delivered to {0}: +{1} Favor. Community bond {2} reached: +${3}.", contact.LocalizedName, gift.FavorGained, gift.NewBondLevel, gift.MilestoneReward)
                    : FarmLocalization.Format("gift.sent", "Gift delivered to {0}: +{1} Favor.", contact.LocalizedName, gift.FavorGained);
                hud?.ShowSystemToast(message, false);
                RefreshAll();
            }
            else hud?.ShowSystemToast(error, true);
        }

        private void ChangePage(int direction)
        {
            currentPage += direction;
            RefreshInbox();
        }

        private void RefreshInbox()
        {
            if (state == null || cardRoots[0] == null) return;
            inbox.Clear();
            inbox.AddRange(FarmMailDatabase.GetInbox(state.DayNumber));
            var pageCount = Mathf.Max(1, Mathf.CeilToInt(inbox.Count / (float)LettersPerPage));
            currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);
            for (var index = 0; index < LettersPerPage; index++)
            {
                var sourceIndex = currentPage * LettersPerPage + index;
                var visible = sourceIndex < inbox.Count;
                cardRoots[index].SetActive(visible);
                visibleMail[index] = visible ? inbox[sourceIndex] : null;
                if (!visible) continue;
                var mail = inbox[sourceIndex];
                var read = state.IsMailRead(mail.Id);
                var claimed = state.IsMailClaimed(mail.Id);
                cardTitles[index].text = mail.Title;
                cardStates[index].text = claimed
                    ? FarmLocalization.Get("mail.state.claimed", "CLAIMED")
                    : read ? FarmLocalization.Get("mail.state.read", "READ")
                    : FarmLocalization.Get("mail.state.new", "NEW");
                cardStates[index].color = claimed ? new Color(0.55f, 0.72f, 0.50f)
                    : read ? new Color(0.68f, 0.74f, 0.64f)
                    : AccentColor;
                cardBackgrounds[index].color = string.Equals(mail.Id, selectedMailId, StringComparison.OrdinalIgnoreCase)
                    ? SelectedColor : CardColor;
            }
            unreadSummary.text = FarmLocalization.Format("mail.summary", "UNREAD  {0}   •   ATTACHMENTS  {1}", state.CountUnreadMail(), state.CountClaimableMail());
            pageText.text = FarmLocalization.Format("ui.page", "PAGE {0}/{1}", currentPage + 1, pageCount);
            previousPageButton.interactable = currentPage > 0;
            nextPageButton.interactable = currentPage + 1 < pageCount;
        }

        private void RefreshDetail()
        {
            var mail = state != null ? FarmMailDatabase.Get(selectedMailId, state.DayNumber) : null;
            var hasMail = mail != null;
            detailSender.gameObject.SetActive(hasMail);
            detailTitle.gameObject.SetActive(hasMail);
            detailDate.gameObject.SetActive(hasMail);
            detailBody.gameObject.SetActive(hasMail);
            detailReward.gameObject.SetActive(hasMail);
            detailStatus.gameObject.SetActive(hasMail);
            claimButton.gameObject.SetActive(hasMail);
            if (!hasMail) return;

            var claimed = state.IsMailClaimed(mail.Id);
            detailSender.text = FarmLocalization.Format("mail.from", "FROM  {0}", mail.Sender.ToUpperInvariant());
            detailTitle.text = mail.Title.ToUpperInvariant();
            detailDate.text = CalendarLabel(mail.EventDay);
            detailBody.text = mail.Body;
            detailReward.text = FarmLocalization.Format("mail.attachment", "ATTACHMENT  •  {0}", mail.RewardText);
            detailStatus.text = claimed
                ? FarmLocalization.Get("mail.status.claimed", "Attachment already claimed.")
                : mail.HasReward ? FarmLocalization.Get("mail.status.available", "Available to claim.")
                : FarmLocalization.Get("mail.status.informational", "Informational letter.");
            claimButton.interactable = mail.HasReward && !claimed;
            claimButtonLabel.text = claimed
                ? FarmLocalization.Get("mail.state.claimed", "CLAIMED")
                : mail.HasReward ? FarmLocalization.Get("mail.claim_attachment", "CLAIM ATTACHMENT")
                : FarmLocalization.Get("mail.reward.none_short", "NO ATTACHMENT");
        }

        private void RefreshGifts()
        {
            if (state == null || giftHint == null) return;
            giftHint.text = FarmLocalization.Get("gift.hint", "COMMUNITY GIFTS  -  one favorite gift per neighbor each day");
            for (var index = 0; index < giftButtons.Length; index++)
            {
                if (giftButtons[index] == null || index >= FarmCommunityCatalog.AllContacts.Count) continue;
                var contact = FarmCommunityCatalog.AllContacts[index];
                var itemId = FarmCommunityCatalog.PreferredGiftId(contact.Id);
                var item = FarmContentDatabase.GetItem(itemId);
                var sentToday = state.Community.HasGiftedOnDay(contact.Id, state.DayNumber);
                var available = state.GetQuantity(itemId) > 0;
                giftButtons[index].interactable = available && !sentToday;
                giftButtonLabels[index].text = sentToday
                    ? FarmLocalization.Format("gift.button_sent", "{0}\nSENT TODAY", contact.LocalizedName)
                    : FarmLocalization.Format("gift.button", "{0}\n{1}  +3 Favor", contact.LocalizedName, item != null ? item.LocalizedName : itemId);
                giftButtonLabels[index].color = available && !sentToday ? Color.white : new Color(0.70f, 0.72f, 0.66f);
            }
        }

        private void RefreshWorldIndicator()
        {
            if (unreadMarker == null || state == null) return;
            unreadMarker.SetActive(state.CountUnreadMail() > 0);
            if (unreadMarker.activeSelf)
            {
                unreadMarker.transform.localRotation = Quaternion.Euler(
                    0f, Time.unscaledTime * 45f, 45f);
                var pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.12f;
                unreadMarker.transform.localScale = Vector3.one * (0.22f * pulse);
            }
        }

        private void RefreshNextEvent()
        {
            if (nextEventText == null || state == null) return;
            var next = FarmMailDatabase.NextEvent(state.DayNumber);
            nextEventText.text = next != null
                ? FarmLocalization.Format("mail.next_event", "NEXT EVENT  •  {0}  •  {1}", CalendarLabel(next.EventDay), next.Title)
                : FarmLocalization.Get("mail.no_event", "LOCAL SCHEDULE IS QUIET");
        }

        private void HandleStateChanged()
        {
            RefreshAll();
            if (IsOpen) RefreshDetail();
        }

        private FarmMailDefinition FindFirstUnread()
        {
            foreach (var mail in inbox)
                if (!state.IsMailRead(mail.Id)) return mail;
            return null;
        }

        private void CreateWorld(Vector3 position, Vector3 facing)
        {
            var prefab = Resources.Load<GameObject>("FarmProps/Mailbox");
            mailboxObject = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            mailboxObject.name = "Farm_Mailbox";
            mailboxObject.transform.SetParent(plot.transform, true);
            mailboxObject.transform.position = position;
            var direction = facing;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            mailboxObject.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            mailboxObject.transform.localScale *= 1.05f;
            if (mailboxObject.GetComponentInChildren<Collider>() == null)
            {
                var collider = mailboxObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.9f, 0f);
                collider.size = new Vector3(0.8f, 1.8f, 0.8f);
            }

            unreadMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            unreadMarker.name = "MailboxUnreadMarker";
            unreadMarker.transform.SetParent(mailboxObject.transform, false);
            unreadMarker.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            unreadMarker.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            unreadMarker.transform.localScale = Vector3.one * 0.22f;
            var markerCollider = unreadMarker.GetComponent<Collider>();
            if (markerCollider != null) Destroy(markerCollider);
            var renderer = unreadMarker.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (renderer != null && shader != null)
            {
                markerMaterial = new Material(shader) { name = "MailboxUnread_Runtime" };
                var color = new Color(1f, 0.72f, 0.14f, 1f);
                if (markerMaterial.HasProperty("_BaseColor")) markerMaterial.SetColor("_BaseColor", color);
                if (markerMaterial.HasProperty("_Color")) markerMaterial.color = color;
                renderer.sharedMaterial = markerMaterial;
            }
        }

        private void CreateInterface(Transform canvas)
        {
            window = CreatePanel(
                "MailboxWindow", canvas, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Vector2.zero, OverlayColor);
            windowGroup = window.AddComponent<CanvasGroup>();
            var panel = CreatePanel(
                "MailboxPanel", window.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1120f, 700f), new Vector2(0.5f, 0.5f), PanelColor);

            CreateText(
                "Title", panel.transform, "mail.title", 27, FontStyle.Bold,
                AccentColor, new Vector2(30f, -20f), new Vector2(500f, 38f), TextAnchor.MiddleLeft);
            unreadSummary = CreateText(
                "UnreadSummary", panel.transform, string.Empty, 14, FontStyle.Bold,
                new Color(0.72f, 0.84f, 0.66f), new Vector2(31f, -59f),
                new Vector2(470f, 24f), TextAnchor.MiddleLeft);
            nextEventText = CreateText(
                "NextEvent", panel.transform, string.Empty, 14, FontStyle.Bold,
                new Color(0.38f, 0.86f, 0.92f), new Vector2(390f, -22f),
                new Vector2(500f, 55f), TextAnchor.MiddleCenter);
            var close = CreateButton(
                "CloseMailbox", panel.transform, "ui.close.esc",
                new Vector2(925f, -22f), new Vector2(165f, 44f));
            close.onClick.AddListener(Close);

            var listPanel = CreatePanel(
                "LetterList", panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -105f), new Vector2(390f, 520f), new Vector2(0f, 1f),
                new Color(0.045f, 0.062f, 0.04f, 1f));
            for (var index = 0; index < LettersPerPage; index++)
            {
                var captured = index;
                var card = CreatePanel(
                    $"Letter_{index + 1}", listPanel.transform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(12f, -12f - index * 91f),
                    new Vector2(366f, 80f), new Vector2(0f, 1f), CardColor);
                cardRoots[index] = card;
                cardBackgrounds[index] = card.GetComponent<Image>();
                var button = card.AddComponent<Button>();
                button.targetGraphic = cardBackgrounds[index];
                button.onClick.AddListener(() => SelectVisibleMail(captured));
                cardTitles[index] = CreateText(
                    "Title", card.transform, string.Empty, 15, FontStyle.Bold,
                    Color.white, new Vector2(14f, -10f), new Vector2(265f, 58f), TextAnchor.MiddleLeft);
                cardStates[index] = CreateText(
                    "State", card.transform, string.Empty, 12, FontStyle.Bold,
                    AccentColor, new Vector2(278f, -10f), new Vector2(75f, 58f), TextAnchor.MiddleRight);
            }
            previousPageButton = CreateButton(
                "PreviousPage", listPanel.transform, "<",
                new Vector2(70f, -470f), new Vector2(54f, 36f));
            previousPageButton.onClick.AddListener(() => ChangePage(-1));
            pageText = CreateText(
                "Page", listPanel.transform, "mail.default_page", 13, FontStyle.Bold,
                new Color(0.70f, 0.80f, 0.65f), new Vector2(135f, -470f),
                new Vector2(120f, 36f), TextAnchor.MiddleCenter);
            nextPageButton = CreateButton(
                "NextPage", listPanel.transform, ">",
                new Vector2(266f, -470f), new Vector2(54f, 36f));
            nextPageButton.onClick.AddListener(() => ChangePage(1));

            var detailPanel = CreatePanel(
                "LetterDetail", panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(445f, -105f), new Vector2(645f, 520f), new Vector2(0f, 1f),
                new Color(0.085f, 0.11f, 0.07f, 1f));
            detailSender = CreateText(
                "Sender", detailPanel.transform, string.Empty, 12, FontStyle.Bold,
                new Color(0.48f, 0.82f, 0.86f), new Vector2(28f, -22f),
                new Vector2(590f, 22f), TextAnchor.MiddleLeft);
            detailTitle = CreateText(
                "Title", detailPanel.transform, string.Empty, 23, FontStyle.Bold,
                AccentColor, new Vector2(28f, -52f), new Vector2(590f, 62f), TextAnchor.MiddleLeft);
            detailDate = CreateText(
                "Date", detailPanel.transform, string.Empty, 13, FontStyle.Bold,
                new Color(0.68f, 0.78f, 0.62f), new Vector2(28f, -118f),
                new Vector2(590f, 24f), TextAnchor.MiddleLeft);
            detailBody = CreateText(
                "Body", detailPanel.transform, string.Empty, 16, FontStyle.Normal,
                Color.white, new Vector2(28f, -158f), new Vector2(590f, 105f), TextAnchor.UpperLeft);
            detailReward = CreateText(
                "Reward", detailPanel.transform, string.Empty, 16, FontStyle.Bold,
                new Color(0.65f, 0.94f, 0.48f), new Vector2(28f, -270f),
                new Vector2(590f, 32f), TextAnchor.MiddleLeft);
            detailStatus = CreateText(
                "Status", detailPanel.transform, string.Empty, 13, FontStyle.Normal,
                new Color(0.72f, 0.80f, 0.68f), new Vector2(28f, -303f),
                new Vector2(590f, 25f), TextAnchor.MiddleLeft);
            giftHint = CreateText(
                "GiftHint", detailPanel.transform, string.Empty, 12, FontStyle.Bold,
                new Color(0.48f, 0.82f, 0.86f), new Vector2(28f, -334f),
                new Vector2(590f, 22f), TextAnchor.MiddleLeft);
            for (var index = 0; index < giftButtons.Length; index++)
            {
                var captured = index;
                giftButtons[index] = CreateButton(
                    $"Gift_{index + 1}", detailPanel.transform, string.Empty,
                    new Vector2(28f + index * 197f, -362f), new Vector2(184f, 54f));
                giftButtonLabels[index] = giftButtons[index].GetComponentInChildren<Text>();
                giftButtons[index].onClick.AddListener(() => GiftToContact(captured));
            }
            claimButton = CreateButton(
                "Claim", detailPanel.transform, "mail.claim_attachment",
                new Vector2(365f, -432f), new Vector2(252f, 54f));
            claimButtonLabel = claimButton.GetComponentInChildren<Text>();
            claimButton.onClick.AddListener(ClaimSelected);

            SetCanvasGroup(windowGroup, false);
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            var buttonObject = CreatePanel(
                name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                position, size, new Vector2(0f, 1f), new Color(0.18f, 0.25f, 0.13f, 1f));
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            var labelText = CreateText(
                "Label", buttonObject.transform, label, 13, FontStyle.Bold,
                Color.white, Vector2.zero, size, TextAnchor.MiddleCenter);
            labelText.rectTransform.anchorMin = Vector2.zero;
            labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private static GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Vector2 pivot,
            Color color)
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

        private Text CreateText(
            string name,
            Transform parent,
            string content,
            int size,
            FontStyle style,
            Color color,
            Vector2 position,
            Vector2 dimensions,
            TextAnchor alignment)
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
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetCanvasGroup(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private static string CalendarLabel(int day) => FarmLocalization.Format("mail.calendar", "YEAR {0}  •  {1} {2}/{3}",
            FarmDayClock.YearForDay(day), FarmDayClock.SeasonName(FarmDayClock.SeasonForDay(day)),
            FarmDayClock.DayInSeason(day), FarmDayClock.DaysPerSeason);
    }
}
