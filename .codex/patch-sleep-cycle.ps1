$ErrorActionPreference = 'Stop'
$project = 'D:\Dev\Unity\Farm\Farm'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'

function Replace-Once([string]$content, [string]$old, [string]$new, [string]$label) {
    if (-not $content.Contains($old)) { throw "Trecho nao encontrado: $label" }
    return $content.Replace($old, $new)
}

function Submit-UnityScript([string]$path, [string]$content, [string]$requestId) {
    $payload = @{ filePath = $path; content = $content; requestId = $requestId } | ConvertTo-Json -Compress
    $result = $payload | & $cli run-tool script-update-or-create $project --input-file -
    if ($LASTEXITCODE -ne 0) { throw "Falha ao enviar $path" }
    $result | Select-Object -Last 12
    Start-Sleep -Seconds 2
}

$dayPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmDayClock.cs'
$day = [IO.File]::ReadAllText($dayPath)
if (-not $day.Contains('RealSecondsForGameMinutes')) {
    $day = Replace-Once $day @'
        public float RealSecondsPerGameDay => realSecondsPerGameDay;
'@ @'
        public float RealSecondsPerGameDay => realSecondsPerGameDay;
        public float GameMinutesPerRealSecond => 1440f / Mathf.Max(60f, realSecondsPerGameDay);
'@ 'day clock conversion property'
    $day = Replace-Once $day @'
        public void SetClockForTesting(int day, float minute)
        {
            state?.SetClock(day, minute);
            ApplyLighting();
        }
'@ @'
        public float RealSecondsForGameMinutes(float gameMinutes) =>
            Mathf.Max(0f, gameMinutes) / GameMinutesPerRealSecond;

        public void SetClock(int day, float minute)
        {
            state?.SetClock(day, minute);
            ApplyLighting();
        }

        public void SetClockForTesting(int day, float minute) => SetClock(day, minute);
'@ 'day clock API'
    Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmDayClock.cs' $day 'sleep-day-clock-v10'
}

$plotPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs'
$plot = [IO.File]::ReadAllText($plotPath)
if (-not $plot.Contains('ConfirmSleep()')) {
    $plot = Replace-Once $plot @'
        private const float StorageInteractionDistance = 1f;
'@ @'
        private const float StorageInteractionDistance = 1f;
        private const float SleepInteractionDistance = 1f;
'@ 'sleep distance'
    $plot = Replace-Once $plot @'
        private FarmStorageStation storageStation;
'@ @'
        private FarmStorageStation storageStation;
        private FarmSleepStation sleepStation;
'@ 'sleep station field'
    $plot = Replace-Once $plot @'
        private FarmStorageStation hoveredStorage;
'@ @'
        private FarmStorageStation hoveredStorage;
        private FarmSleepStation hoveredSleep;
'@ 'hover sleep field'
    $plot = Replace-Once $plot @'
        public bool StorageVisible => storageStation != null && IsStorageInRange();
'@ @'
        public bool StorageVisible => storageStation != null && IsStorageInRange();
        public bool SleepVisible => sleepStation != null && IsSleepInRange();
        public bool SleepInRange => SleepVisible;
'@ 'sleep properties'
    $plot = Replace-Once $plot @'
                    if (StorageVisible) RequestStorage();
                    else if (ShopVisible) RequestSell();
'@ @'
                    if (SleepVisible) RequestSleep();
                    else if (StorageVisible) RequestStorage();
                    else if (ShopVisible) RequestSell();
'@ 'E sleep priority'
    $plot = Replace-Once $plot @'
        public void RequestStorage()
        {
            if (!IsStorageInRange())
            {
                feedback = "Chegue mais perto do dep\u00F3sito.";
                return;
            }
            feedback = "Dep\u00F3sito aberto.";
            hud?.SetStorageOpen(true);
        }
'@ @'
        public void RequestStorage()
        {
            if (!IsStorageInRange())
            {
                feedback = "Chegue mais perto do dep\u00F3sito.";
                return;
            }
            feedback = "Dep\u00F3sito aberto.";
            hud?.SetStorageOpen(true);
        }

        public void RequestSleep()
        {
            if (!IsSleepInRange())
            {
                feedback = "Chegue mais perto da cama.";
                return;
            }
            feedback = "Confirme para encerrar o dia.";
            hud?.SetSleepConfirmationOpen(true);
        }

        public void ConfirmSleep()
        {
            if (gameState == null || dayClock == null || !IsSleepInRange())
            {
                hud?.SetSleepConfirmationOpen(false);
                feedback = "N\u00E3o foi poss\u00EDvel descansar longe da cama.";
                return;
            }

            var currentMinute = Mathf.Repeat(gameState.MinutesOfDay, 1440f);
            var skippedGameMinutes = (1440f - currentMinute) + 360f;
            var growthSeconds = dayClock.RealSecondsForGameMinutes(skippedGameMinutes);
            var advancedCrops = 0;
            foreach (var tile in tiles)
                if (tile.AdvanceGrowth(growthSeconds)) advancedCrops++;

            var nextDay = Mathf.Max(1, gameState.DayNumber + 1);
            dayClock.SetClock(nextDay, 360f);
            weatherSystem?.Refresh();
            hud.SetSleepConfirmationOpen(false);
            hud.ShowDayTransition(nextDay, weatherSystem != null ? weatherSystem.DisplayName : string.Empty);
            feedback = advancedCrops > 0
                ? $"Bom dia! {advancedCrops} cultivo(s) avan\u00E7aram durante a noite."
                : "Bom dia! Um novo dia come\u00E7ou na fazenda.";
            SaveGame(false);
        }
'@ 'sleep API'
    $plot = Replace-Once $plot @'
            CreateStorageStation(center, forward, offset);
'@ @'
            CreateStorageStation(center, forward, offset);
            CreateSleepStation(center, forward, offset);
'@ 'create sleep call'
    $plot = Replace-Once $plot @'
        private static Vector3 PlaceOnGround(Vector3 position)
'@ @'
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

        private static Vector3 PlaceOnGround(Vector3 position)
'@ 'create sleep station'
    $plot = Replace-Once $plot @'
            SetHoveredStorage(null);
'@ @'
            SetHoveredStorage(null);
            SetHoveredSleep(null);
'@ 'clear sleep hover'
    $plot = Replace-Once $plot @'
                var storage = hit.collider.GetComponentInParent<FarmStorageStation>();
                if (storage != null)
                {
                    SetHoveredStorage(storage);
                    return;
                }
'@ @'
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
'@ 'raycast sleep hover'
    $plot = Replace-Once $plot @'
        private bool IsInRange(Vector3 targetPosition) =>
'@ @'
        private void SetHoveredSleep(FarmSleepStation station)
        {
            if (hoveredSleep == station) return;
            if (hoveredSleep != null) hoveredSleep.SetHovered(false);
            hoveredSleep = station;
            if (hoveredSleep != null) hoveredSleep.SetHovered(true);
        }

        private bool IsInRange(Vector3 targetPosition) =>
'@ 'sleep hover setter'
    $plot = Replace-Once $plot @'
        private void TryPrimaryInteraction()
'@ @'
        private bool IsSleepInRange() =>
            player != null && sleepStation != null && Vector3.Distance(player.position, sleepStation.transform.position) <= SleepInteractionDistance;

        private void TryPrimaryInteraction()
'@ 'sleep range'
    $plot = Replace-Once $plot @'
            if (hoveredStation != null) RequestSell();
            else if (hoveredStorage != null) RequestStorage();
'@ @'
            if (hoveredStation != null) RequestSell();
            else if (hoveredStorage != null) RequestStorage();
            else if (hoveredSleep != null) RequestSleep();
'@ 'mouse sleep'
    $plot = Replace-Once $plot @'
            if (StorageVisible) return "Dep\u00F3sito ao alcance: pressione E para abrir.";
'@ @'
            if (hoveredSleep != null)
            {
                var range = IsSleepInRange() ? "" : " (fora de alcance)";
                return $"Cama: clique ou E para encerrar o dia{range}";
            }
            if (SleepVisible) return "Cama ao alcance: pressione E para descansar.";
            if (StorageVisible) return "Dep\u00F3sito ao alcance: pressione E para abrir.";
'@ 'sleep prompt'
    $plot = Replace-Once $plot @'
    public sealed class FarmStorageStation : MonoBehaviour
'@ @'
    public sealed class FarmSleepStation : MonoBehaviour
    {
        private Vector3 normalScale;
        public void Initialize() => normalScale = transform.localScale;
        public void SetHovered(bool hovered) => transform.localScale = normalScale * (hovered ? 1.05f : 1f);
    }

    public sealed class FarmStorageStation : MonoBehaviour
'@ 'sleep station component'
    $plot = Replace-Once $plot @'
        public FarmTileSaveData CaptureSaveData() => new()
'@ @'
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
                readyAt = Time.time + remaining;
                var halfwayRemaining = cropDefinition.GrowthSeconds * 0.5f;
                if (remaining > halfwayRemaining)
                {
                    state = State.Watered;
                    middleAt = Time.time + (remaining - halfwayRemaining);
                }
                else
                {
                    state = State.Growing;
                    middleAt = Time.time;
                }
            }
            RefreshVisual();
            plot.NotifyTileChanged();
            return true;
        }

        public FarmTileSaveData CaptureSaveData() => new()
'@ 'overnight growth'
    Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $plot 'sleep-plot-v10'
} elseif ($plot.Contains('weatherSystem.DisplayName')) {
    $plot = $plot.Replace('weatherSystem.DisplayName', 'FarmWeatherSystem.WeatherName(weatherSystem.CurrentWeather)')
    Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $plot 'sleep-weather-name-v10'
}

$hudPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmHudController.cs'
$hud = [IO.File]::ReadAllText($hudPath)
if (-not $hud.Contains('CreateSleepConfirmation')) {
    $hud = Replace-Once $hud "using System;`n" "using System;`nusing System.Collections;`n" 'IEnumerator import'
    $hud = Replace-Once $hud @'
        private float pickupToastUntil;
'@ @'
        private float pickupToastUntil;
        private GameObject sleepConfirmationWindow;
        private CanvasGroup sleepConfirmationGroup;
        private bool sleepConfirmationOpen;
        private CanvasGroup dayTransitionGroup;
        private Text dayTransitionText;
        private Coroutine dayTransitionRoutine;
'@ 'sleep UI fields'
    $hud = Replace-Once $hud @'
        public bool IsJournalOpen => journalOpen;
'@ @'
        public bool IsJournalOpen => journalOpen;
        public bool IsSleepConfirmationOpen => sleepConfirmationOpen;
        public float DayTransitionAlpha => dayTransitionGroup != null ? dayTransitionGroup.alpha : 0f;
        public string DayTransitionText => dayTransitionText != null ? dayTransitionText.text : string.Empty;
'@ 'sleep UI properties'
    $hud = Replace-Once $hud @'
            if (value && journalOpen) SetJournalOpen(false);
            inventoryOpen = value;
'@ @'
            if (value && journalOpen) SetJournalOpen(false);
            if (value && sleepConfirmationOpen) SetSleepConfirmationOpen(false);
            inventoryOpen = value;
'@ 'inventory closes sleep'
    $hud = Replace-Once $hud @'
            if (value && journalOpen) SetJournalOpen(false);
            storageOpen = value;
'@ @'
            if (value && journalOpen) SetJournalOpen(false);
            if (value && sleepConfirmationOpen) SetSleepConfirmationOpen(false);
            storageOpen = value;
'@ 'storage closes sleep'
    $hud = Replace-Once $hud @'
            if (value && storageOpen) SetStorageOpen(false);
            journalOpen = value;
'@ @'
            if (value && storageOpen) SetStorageOpen(false);
            if (value && sleepConfirmationOpen) SetSleepConfirmationOpen(false);
            journalOpen = value;
'@ 'journal closes sleep'
    $hud = Replace-Once $hud @'
        public void ClaimJournalQuest(int index)
'@ @'
        public void SetSleepConfirmationOpen(bool value)
        {
            if (value && inventoryOpen) SetInventoryOpen(false);
            if (value && storageOpen) SetStorageOpen(false);
            if (value && journalOpen) SetJournalOpen(false);
            sleepConfirmationOpen = value;
            SetCanvasGroup(sleepConfirmationGroup, value);
            if (value) sleepConfirmationWindow.transform.SetAsLastSibling();
            UpdateModalState();
        }

        public void ConfirmSleep()
        {
            plot?.ConfirmSleep();
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
'@ 'sleep UI methods'
    $hud = Replace-Once $hud @'
        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen || journalOpen;
'@ @'
        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen;
'@ 'sleep modal state'
    $hud = Replace-Once $hud @'
                if (journalOpen) SetJournalOpen(false);
'@ @'
                if (journalOpen) SetJournalOpen(false);
                if (sleepConfirmationOpen) SetSleepConfirmationOpen(false);
'@ 'escape closes sleep'
    $hud = Replace-Once $hud @'
            if (resourcesText == null || toolText == null || promptText == null || feedbackText == null || saveText == null || tutorialText == null || clockText == null || weatherText == null || inventorySummaryText == null || shopInfoText == null || shopPanel == null || upgradeToolButton == null || upgradeToolButtonText == null || inventoryWindow == null || inventoryGroup == null || storageWindow == null || storageGroup == null || journalWindow == null || journalGroup == null) return false;
'@ @'
            if (resourcesText == null || toolText == null || promptText == null || feedbackText == null || saveText == null || tutorialText == null || clockText == null || weatherText == null || inventorySummaryText == null || shopInfoText == null || shopPanel == null || upgradeToolButton == null || upgradeToolButtonText == null || inventoryWindow == null || inventoryGroup == null || storageWindow == null || storageGroup == null || journalWindow == null || journalGroup == null || sleepConfirmationWindow == null || sleepConfirmationGroup == null || dayTransitionGroup == null || dayTransitionText == null) return false;
'@ 'interface ready sleep'
    $hud = Replace-Once $hud @'
            CreateJournalWindow(root.transform);
            CreatePickupToast(root.transform);
'@ @'
            CreateJournalWindow(root.transform);
            CreateSleepConfirmation(root.transform);
            CreateDayTransition(root.transform);
            CreatePickupToast(root.transform);
'@ 'create sleep UI'
    $hud = Replace-Once $hud @'
        private void CreateJournalWindow(Transform root)
'@ @'
        private void CreateSleepConfirmation(Transform root)
        {
            sleepConfirmationWindow = CreatePanel("SleepConfirmation", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.01f, 0.015f, 0.025f, 0.78f));
            var backdrop = sleepConfirmationWindow.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            sleepConfirmationGroup = sleepConfirmationWindow.AddComponent<CanvasGroup>();
            var panel = CreatePanel("SleepPanel", sleepConfirmationWindow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 280f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("Title", panel.transform, "ENCERRAR O DIA?", 28, FontStyle.Bold, new Color(0.65f, 0.58f, 1f), new Vector2(30f, -24f), new Vector2(500f, 40f), TextAnchor.MiddleCenter);
            CreateText("Description", panel.transform, "Dormir avan\u00E7a para o pr\u00F3ximo dia, \u00E0s 06:00.\nCultivos regados continuam crescendo durante a noite.", 18, FontStyle.Normal, Color.white, new Vector2(35f, -82f), new Vector2(490f, 72f), TextAnchor.MiddleCenter);
            var cancel = CreateButton("CancelSleep", panel.transform, "CANCELAR", new Vector2(35f, -202f), new Vector2(230f, 52f));
            var confirm = CreateButton("ConfirmSleep", panel.transform, "DORMIR", new Vector2(295f, -202f), new Vector2(230f, 52f));
            cancel.onClick.AddListener(() => SetSleepConfirmationOpen(false));
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
'@ 'sleep UI builders'
    Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $hud 'sleep-hud-v10'
}

Write-Output 'Sleep cycle patch submitted.'
