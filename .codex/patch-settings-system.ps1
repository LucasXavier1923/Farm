$ErrorActionPreference='Stop';$project='D:\Dev\Unity\Farm\Farm';$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
function ReplaceExact([string]$c,[string]$o,[string]$n,[string]$l){if(-not $c.Contains($o)){throw "Trecho nao encontrado: $l"};$c.Replace($o,$n)}
function Submit([string]$p,[string]$c,[string]$id){$payload=@{filePath=$p;content=$c;requestId=$id}|ConvertTo-Json -Compress;$result=$payload|& $cli run-tool script-update-or-create $project --input-file -;if($LASTEXITCODE-ne 0){throw "Falha: $p"};$result|Select-Object -Last 12;Start-Sleep -Seconds 2}

$settings=@'
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmPrototype.Farming
{
    public static class FarmSettings
    {
        public const float DefaultCameraSensitivity = 0.12f;
        public const float DefaultZoomStep = 2f;
        public const float MinCameraSensitivity = 0.04f;
        public const float MaxCameraSensitivity = 0.40f;
        public const float MinZoomStep = 0.5f;
        public const float MaxZoomStep = 10f;
        private const string SensitivityKey = "Farm.Settings.CameraSensitivity";
        private const string ZoomKey = "Farm.Settings.ZoomStep";
        private const string InvertVerticalKey = "Farm.Settings.InvertVertical";
        private static bool loaded;
        private static float cameraSensitivity;
        private static float zoomStep;
        private static bool invertVertical;

        public static event Action Changed;
        public static float CameraSensitivity { get { EnsureLoaded(); return cameraSensitivity; } }
        public static float ZoomStep { get { EnsureLoaded(); return zoomStep; } }
        public static bool InvertVertical { get { EnsureLoaded(); return invertVertical; } }

        public static void EnsureLoaded()
        {
            if (loaded) return;
            cameraSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, DefaultCameraSensitivity), MinCameraSensitivity, MaxCameraSensitivity);
            zoomStep = Mathf.Clamp(PlayerPrefs.GetFloat(ZoomKey, DefaultZoomStep), MinZoomStep, MaxZoomStep);
            invertVertical = PlayerPrefs.GetInt(InvertVerticalKey, 0) != 0;
            loaded = true;
        }

        public static void SetCameraSensitivity(float value)
        {
            EnsureLoaded();
            cameraSensitivity = Mathf.Clamp(value, MinCameraSensitivity, MaxCameraSensitivity);
            PlayerPrefs.SetFloat(SensitivityKey, cameraSensitivity);
            SaveAndNotify();
        }

        public static void SetZoomStep(float value)
        {
            EnsureLoaded();
            zoomStep = Mathf.Clamp(value, MinZoomStep, MaxZoomStep);
            PlayerPrefs.SetFloat(ZoomKey, zoomStep);
            SaveAndNotify();
        }

        public static void SetInvertVertical(bool value)
        {
            EnsureLoaded();
            invertVertical = value;
            PlayerPrefs.SetInt(InvertVerticalKey, invertVertical ? 1 : 0);
            SaveAndNotify();
        }

        public static void ResetDefaults()
        {
            EnsureLoaded();
            cameraSensitivity = DefaultCameraSensitivity;
            zoomStep = DefaultZoomStep;
            invertVertical = false;
            PlayerPrefs.SetFloat(SensitivityKey, cameraSensitivity);
            PlayerPrefs.SetFloat(ZoomKey, zoomStep);
            PlayerPrefs.SetInt(InvertVerticalKey, 0);
            SaveAndNotify();
        }

        private static void SaveAndNotify()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public sealed class FarmSettingsMenu : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.06f, 0.08f, 0.055f, 0.96f);
        private static readonly Color AccentColor = new(0.95f, 0.67f, 0.18f, 1f);
        private FarmHudController hud;
        private Font font;
        private GameObject window;
        private CanvasGroup group;
        private Text sensitivityValue;
        private Text zoomValue;
        private Text invertValue;
        private bool open;

        public bool IsOpen => open;
        public string SensitivityLabel => sensitivityValue != null ? sensitivityValue.text : string.Empty;
        public string ZoomLabel => zoomValue != null ? zoomValue.text : string.Empty;
        public string InvertLabel => invertValue != null ? invertValue.text : string.Empty;

        public void Initialize(FarmHudController owner)
        {
            if (window != null) return;
            hud = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            FarmSettings.EnsureLoaded();
            var canvas = hud.GetComponentInChildren<Canvas>();
            if (canvas == null) throw new InvalidOperationException("Canvas da fazenda ausente para configuracoes.");
            CreateLauncher(canvas.transform);
            CreateWindow(canvas.transform);
            RefreshLabels();
            SetOpen(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f10Key.wasPressedThisFrame) SetOpen(!open);
            else if (open && keyboard.escapeKey.wasPressedThisFrame) SetOpen(false);
            if (open && (hud.IsInventoryOpen || hud.IsStorageOpen || hud.IsJournalOpen || hud.IsSleepConfirmationOpen || hud.IsDailyOrdersOpen)) SetOpen(false);
        }

        private void OnDisable()
        {
            if (hud != null) hud.SetSettingsOpen(false);
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
            hud.SetSettingsOpen(value);
            if (value)
            {
                RefreshLabels();
                window.transform.SetAsLastSibling();
            }
        }

        public void AdjustSensitivity(float delta)
        {
            FarmSettings.SetCameraSensitivity(FarmSettings.CameraSensitivity + delta);
            RefreshLabels();
        }

        public void AdjustZoom(float delta)
        {
            FarmSettings.SetZoomStep(FarmSettings.ZoomStep + delta);
            RefreshLabels();
        }

        public void ToggleInvert()
        {
            FarmSettings.SetInvertVertical(!FarmSettings.InvertVertical);
            RefreshLabels();
        }

        public void ResetDefaults()
        {
            FarmSettings.ResetDefaults();
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (sensitivityValue == null) return;
            sensitivityValue.text = $"{FarmSettings.CameraSensitivity:0.00}";
            zoomValue.text = $"{FarmSettings.ZoomStep:0.0} unidades";
            invertValue.text = FarmSettings.InvertVertical ? "ATIVADA" : "DESATIVADA";
            invertValue.color = FarmSettings.InvertVertical ? new Color(0.45f, 0.92f, 0.55f) : Color.white;
        }

        private void CreateLauncher(Transform root)
        {
            var button = CreateButton("OpenSettings", root, "OPCOES  [F10]", new Vector2(1f, 1f), new Vector2(-18f, -282f), new Vector2(210f, 44f), new Vector2(1f, 1f));
            button.onClick.AddListener(() => SetOpen(true));
        }

        private void CreateWindow(Transform root)
        {
            window = CreatePanel("SettingsWindow", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.015f, 0.78f));
            var backdrop = window.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            group = window.AddComponent<CanvasGroup>();
            var panel = CreatePanel("SettingsPanel", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 520f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("Title", panel.transform, "CONFIGURACOES", 30, FontStyle.Bold, AccentColor, new Vector2(30f, -22f), new Vector2(500f, 42f));
            CreateText("Subtitle", panel.transform, "Preferencias sao salvas neste computador.", 16, FontStyle.Normal, new Color(0.78f, 0.84f, 0.74f), new Vector2(30f, -66f), new Vector2(600f, 28f));
            var close = CreateButton("CloseSettings", panel.transform, "FECHAR  [ESC]", new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(160f, 44f), new Vector2(1f, 1f));
            close.onClick.AddListener(() => SetOpen(false));
            CreateSettingRow(panel.transform, "SENSIBILIDADE DA CAMERA", "Quanto o mouse gira a camera.", -122f, out sensitivityValue, () => AdjustSensitivity(-0.02f), () => AdjustSensitivity(0.02f));
            CreateSettingRow(panel.transform, "VELOCIDADE DO ZOOM", "Distancia alterada por passo da roda.", -238f, out zoomValue, () => AdjustZoom(-0.5f), () => AdjustZoom(0.5f));
            CreateText("InvertTitle", panel.transform, "INVERTER EIXO VERTICAL", 18, FontStyle.Bold, Color.white, new Vector2(38f, -354f), new Vector2(320f, 28f));
            CreateText("InvertHint", panel.transform, "Muda o sentido vertical ao arrastar com o botao direito.", 14, FontStyle.Normal, new Color(0.72f, 0.78f, 0.70f), new Vector2(38f, -385f), new Vector2(430f, 24f));
            var invert = CreateButton("ToggleInvert", panel.transform, "", new Vector2(0f, 1f), new Vector2(500f, -354f), new Vector2(175f, 52f), new Vector2(0f, 1f));
            invertValue = invert.GetComponentInChildren<Text>();
            invert.onClick.AddListener(ToggleInvert);
            var reset = CreateButton("ResetSettings", panel.transform, "RESTAURAR PADROES", new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(260f, 48f), new Vector2(0.5f, 0f));
            reset.onClick.AddListener(ResetDefaults);
        }

        private void CreateSettingRow(Transform parent, string title, string hint, float y, out Text value, UnityEngine.Events.UnityAction decrease, UnityEngine.Events.UnityAction increase)
        {
            CreateText(title + "_Title", parent, title, 18, FontStyle.Bold, Color.white, new Vector2(38f, y), new Vector2(330f, 28f));
            CreateText(title + "_Hint", parent, hint, 14, FontStyle.Normal, new Color(0.72f, 0.78f, 0.70f), new Vector2(38f, y - 31f), new Vector2(390f, 24f));
            var minus = CreateButton(title + "_Minus", parent, "-", new Vector2(0f, 1f), new Vector2(455f, y), new Vector2(54f, 52f), new Vector2(0f, 1f));
            var display = CreatePanel(title + "_Value", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(519f, y), new Vector2(100f, 52f), new Vector2(0f, 1f), new Color(0.12f, 0.15f, 0.10f, 1f));
            value = CreateText("Value", display.transform, "", 17, FontStyle.Bold, AccentColor, Vector2.zero, new Vector2(100f, 52f), TextAnchor.MiddleCenter);
            var plus = CreateButton(title + "_Plus", parent, "+", new Vector2(0f, 1f), new Vector2(629f, y), new Vector2(54f, 52f), new Vector2(0f, 1f));
            minus.onClick.AddListener(decrease);
            plus.onClick.AddListener(increase);
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private Text CreateText(string name, Transform parent, string content, int size, FontStyle style, Color color, Vector2 position, Vector2 dimensions, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Text)); item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var text = item.GetComponent<Text>(); text.font = font; text.text = content; text.fontSize = size; text.fontStyle = style; text.color = color; text.alignment = alignment; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            var item = CreatePanel(name, parent, anchor, anchor, position, size, pivot, new Color(0.18f, 0.23f, 0.14f, 1f));
            var button = item.AddComponent<Button>(); var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, 0.86f, 0.50f); colors.pressedColor = new Color(0.84f, 0.66f, 0.26f); button.colors = colors;
            var text = CreateText("Label", item.transform, label, 16, FontStyle.Bold, Color.white, Vector2.zero, size, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.pivot = new Vector2(0.5f, 0.5f); text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
'@
Submit 'Assets/_Project/Scripts/Farming/FarmSettings.cs' $settings 'farm-settings-create'

$cameraPath=Join-Path $project 'Assets\_Project\Scripts\Player\FarmThirdPersonCamera.cs';$camera=[IO.File]::ReadAllText($cameraPath)
if(-not $camera.Contains('ConfiguredSensitivity')){
 $camera=ReplaceExact $camera "        [SerializeField] private float zoomStep = 2f;`r`n        [SerializeField] private float sensitivity = 0.12f;`r`n" '' 'camera serialized settings'
 $camera=ReplaceExact $camera "        private float pitch = 55f;" "        private float pitch = 55f;`r`n`r`n        public float ConfiguredSensitivity => FarmSettings.CameraSensitivity;`r`n        public float ConfiguredZoomStep => FarmSettings.ZoomStep;`r`n        public bool InvertVertical => FarmSettings.InvertVertical;`r`n        public float CurrentDistance => distance;" 'camera properties'
 $camera=ReplaceExact $camera "            FindTarget();`r`n            Cursor.lockState" "            FarmSettings.EnsureLoaded();`r`n            FindTarget();`r`n            Cursor.lockState" 'camera load'
 $camera=ReplaceExact $camera "                    yaw += delta.x * sensitivity;`r`n                    pitch -= delta.y * sensitivity;" "                    yaw += delta.x * FarmSettings.CameraSensitivity;`r`n                    pitch += delta.y * FarmSettings.CameraSensitivity * (FarmSettings.InvertVertical ? 1f : -1f);" 'camera rotation'
 $camera=$camera.Replace('Mathf.Sign(scroll) * zoomStep','Mathf.Sign(scroll) * FarmSettings.ZoomStep')
 Submit 'Assets/_Project/Scripts/Player/FarmThirdPersonCamera.cs' $camera 'farm-camera-settings'
}

$hudPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmHudController.cs';$hud=[IO.File]::ReadAllText($hudPath)
if(-not $hud.Contains('IsSettingsOpen')){
 $hud=ReplaceExact $hud "        private bool dailyOrdersOpen;" "        private bool dailyOrdersOpen;`r`n        private bool settingsOpen;" 'hud settings field'
 $hud=ReplaceExact $hud "        public bool IsDailyOrdersOpen => dailyOrdersOpen;" "        public bool IsDailyOrdersOpen => dailyOrdersOpen;`r`n        public bool IsSettingsOpen => settingsOpen;" 'hud settings property'
 $hud=ReplaceExact $hud @'
        public void CompleteDailyOrder(int index) => plot?.TryCompleteDailyOrder(index);
'@ @'
        public void SetSettingsOpen(bool value)
        {
            settingsOpen = value;
            UpdateModalState();
        }

        public void CompleteDailyOrder(int index) => plot?.TryCompleteDailyOrder(index);
'@ 'hud settings method'
 $hud=$hud.Replace('inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen;','inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen;')
 Submit 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $hud 'farm-hud-settings-modal'
}

$plotPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs';$plot=[IO.File]::ReadAllText($plotPath)
if(-not $plot.Contains('FarmSettingsMenu')){
 $plot=ReplaceExact $plot @'
            hud.Initialize(this);
            dayClock = GetComponent<FarmDayClock>();
'@ @'
            hud.Initialize(this);
            var settingsMenu = GetComponent<FarmSettingsMenu>();
            if (settingsMenu == null) settingsMenu = gameObject.AddComponent<FarmSettingsMenu>();
            settingsMenu.Initialize(hud);
            dayClock = GetComponent<FarmDayClock>();
'@ 'settings bootstrap'
 Submit 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $plot 'farm-settings-bootstrap'
}
Write-Output 'Settings system submitted.'
