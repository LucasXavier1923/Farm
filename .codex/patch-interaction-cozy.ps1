$ErrorActionPreference='Stop'
function Replace-Required([string]$content,[string]$old,[string]$new,[string]$label){if(-not $content.Contains($old)){throw "Trecho ausente: $label"};$content.Replace($old,$new)}
$root=(Get-Location).Path

$plot=[IO.File]::ReadAllText((Join-Path $root 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs'))
$plot=Replace-Required $plot @'
        private const float SellStationInteractionDistance = 1f;
        private const float StorageInteractionDistance = 1f;
        private const float SleepInteractionDistance = 1f;
        private const float OrderBoardInteractionDistance = 1f;
'@ @'
        private const float SellStationInteractionDistance = 2.2f;
        private const float StorageInteractionDistance = 2.2f;
        private const float SleepInteractionDistance = 2.5f;
        private const float OrderBoardInteractionDistance = 2.2f;
'@ 'comfortable station ranges'
$plot=Replace-Required $plot @'
        public bool ShopVisible => sellStation != null && IsStationInRange();
        public bool ShopInRange => ShopVisible;
'@ @'
        public bool ShopVisible => hud != null && hud.IsShopOpen;
        public bool ShopInRange => sellStation != null && IsStationInRange();
'@ 'manual shop visibility'
$plot=Replace-Required $plot @'
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    if (SleepVisible) RequestSleep();
                    else if (OrderBoardVisible) RequestOrderBoard();
                    else if (StorageVisible) RequestStorage();
                    else if (ShopVisible) RequestSell();
                }
                if (keyboard.bKey.wasPressedThisFrame && ShopVisible) RequestBuySeeds();
                if (keyboard.uKey.wasPressedThisFrame && ShopVisible) RequestUpgradeActiveTool();
'@ @'
                if (keyboard.fKey.wasPressedThisFrame)
                {
                    if (SleepVisible) RequestSleep();
                    else if (OrderBoardVisible) RequestOrderBoard();
                    else if (StorageVisible) RequestStorage();
                    else if (ShopInRange) RequestShop();
                }
'@ 'unified F interaction'
$plot=Replace-Required $plot @'
        public void RequestSell()
        {
            InteractWithStation(false);
        }
'@ @'
        public void RequestShop()
        {
            if (!IsStationInRange())
            {
                feedback = "Chegue mais perto do caixote de vendas.";
                return;
            }
            feedback = "Com\u00E9rcio aberto.";
            hud?.SetShopOpen(true);
        }

        public void RequestSell()
        {
            InteractWithStation(false);
        }
'@ 'request shop'
$plot=Replace-Required $plot 'if (hoveredStation != null) RequestSell();' 'if (hoveredStation != null) RequestShop();' 'mouse opens shop'
$plot=Replace-Required $plot @'
                return $"Caixote: clique/E vende tudo \u2022 B compra sementes{range}";
'@ @'
                return $"Caixote: clique ou F para abrir o com\u00E9rcio{range}";
'@ 'shop prompt'
$plot=Replace-Required $plot 'return $"Dep\u00F3sito: clique ou E para organizar itens{range}";' 'return $"Dep\u00F3sito: clique ou F para organizar itens{range}";' 'storage prompt'
$plot=Replace-Required $plot 'return $"Cama: clique ou E para encerrar o dia{range}";' 'return $"Cama: clique ou F para encerrar o dia{range}";' 'sleep prompt'
$plot=Replace-Required $plot 'return $"Quadro de pedidos: clique ou E para ver entregas{range}";' 'return $"Quadro de pedidos: clique ou F para ver entregas{range}";' 'orders prompt'
$plot=Replace-Required $plot @'
            if (OrderBoardVisible) return "Quadro ao alcance: pressione E ou P para abrir os pedidos.";
            if (SleepVisible) return "Cama ao alcance: pressione E para descansar.";
            if (StorageVisible) return "Dep\u00F3sito ao alcance: pressione E para abrir.";
            return ShopVisible ? "Caixote ao alcance: use os bot\u00F5es de com\u00E9rcio." : "Passe o mouse sobre um canteiro ou aproxime-se de uma esta\u00E7\u00E3o.";
'@ @'
            if (OrderBoardVisible) return "Quadro ao alcance: pressione F para abrir os pedidos.";
            if (SleepVisible) return "Cama ao alcance: pressione F para descansar.";
            if (StorageVisible) return "Dep\u00F3sito ao alcance: pressione F para abrir.";
            return ShopInRange ? "Caixote ao alcance: pressione F para abrir o com\u00E9rcio." : "Passe o mouse sobre um canteiro ou aproxime-se de uma esta\u00E7\u00E3o.";
'@ 'nearby prompts'
$plot=$plot.Replace('saveStatus = data.Version < 12 ? "Save migrado para v12" : "Save carregado";','saveStatus = data.Version < 13 ? "Save migrado para v13" : "Save carregado";')
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmTestPlot.interaction.cs.txt'),$plot,[Text.UTF8Encoding]::new($false))

$hud=[IO.File]::ReadAllText((Join-Path $root 'Assets\_Project\Scripts\Farming\FarmHudController.cs'))
$hud=Replace-Required $hud @'
        private GameObject shopPanel;
        private Button sellButton;
'@ @'
        private GameObject shopPanel;
        private bool shopOpen;
        private Button sellButton;
'@ 'shop open field'
$hud=Replace-Required $hud @'
        public bool IsCraftingOpen => craftingOpen;
        public float DayTransitionAlpha
'@ @'
        public bool IsCraftingOpen => craftingOpen;
        public bool IsShopOpen => shopOpen;
        public float DayTransitionAlpha
'@ 'shop property'
$hud=Replace-Required $hud @'
            shopPanel.SetActive(!IsModalOpen && plot.ShopVisible);
            if (!IsModalOpen && plot.ShopVisible)
'@ @'
            shopPanel.SetActive(shopOpen);
            if (shopOpen)
'@ 'manual shop panel'
$hud=Replace-Required $hud @'
        public void SetInventoryOpen(bool value)
        {
            if (value && storageOpen) SetStorageOpen(false);
'@ @'
        public void SetInventoryOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            if (value && storageOpen) SetStorageOpen(false);
'@ 'inventory closes shop'
$hud=Replace-Required $hud @'
        public void SetStorageOpen(bool value)
        {
            if (value && inventoryOpen) SetInventoryOpen(false);
'@ @'
        public void SetStorageOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            if (value && inventoryOpen) SetInventoryOpen(false);
'@ 'storage closes shop'
$hud=Replace-Required $hud @'
        public void SetJournalOpen(bool value)
        {
            if (value && inventoryOpen) SetInventoryOpen(false);
'@ @'
        public void SetJournalOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            if (value && inventoryOpen) SetInventoryOpen(false);
'@ 'journal closes shop'
$hud=Replace-Required $hud @'
        public void SetSleepConfirmationOpen(bool value)
        {
            if (value && inventoryOpen) SetInventoryOpen(false);
'@ @'
        public void SetSleepConfirmationOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            if (value && inventoryOpen) SetInventoryOpen(false);
'@ 'sleep closes shop'
$hud=Replace-Required $hud @'
        public void SetDailyOrdersOpen(bool value)
        {
            if (value && inventoryOpen) SetInventoryOpen(false);
'@ @'
        public void SetDailyOrdersOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            if (value && inventoryOpen) SetInventoryOpen(false);
'@ 'orders closes shop'
$hud=Replace-Required $hud @'
        public void SetSettingsOpen(bool value)
        {
            settingsOpen = value;
'@ @'
        public void SetSettingsOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            settingsOpen = value;
'@ 'settings closes shop'
$hud=Replace-Required $hud @'
        public void SetMasteryOpen(bool value)
        {
            masteryOpen = value;
'@ @'
        public void SetMasteryOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            masteryOpen = value;
'@ 'mastery closes shop'
$hud=Replace-Required $hud @'
        public void SetCraftingOpen(bool value)
        {
            craftingOpen = value;
'@ @'
        public void SetCraftingOpen(bool value)
        {
            if (value && shopOpen) SetShopOpen(false);
            craftingOpen = value;
'@ 'crafting closes shop'
$hud=Replace-Required $hud @'
        public void CompleteDailyOrder(int index) => plot?.TryCompleteDailyOrder(index);
'@ @'
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
'@ 'shop setter'
$hud=Replace-Required $hud @'
            if (keyboard.pKey.wasPressedThisFrame)
            {
                if (dailyOrdersOpen) SetDailyOrdersOpen(false);
                else plot?.RequestOrderBoard();
            }
            else if (keyboard.jKey.wasPressedThisFrame)
'@ @'
            if (keyboard.jKey.wasPressedThisFrame)
'@ 'remove P interaction'
$hud=Replace-Required $hud @'
                if (dailyOrdersOpen) SetDailyOrdersOpen(false);
            }
'@ @'
                if (dailyOrdersOpen) SetDailyOrdersOpen(false);
                if (shopOpen) SetShopOpen(false);
            }
'@ 'escape closes shop'
$hud=Replace-Required $hud 'IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen || masteryOpen || craftingOpen;' 'IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen || masteryOpen || craftingOpen || shopOpen;' 'shop modal state'
$hud=Replace-Required $hud 'new Vector2(500f, 320f)' 'new Vector2(500f, 380f)' 'shop panel height'
$hud=Replace-Required $hud @'
            upgradeToolButton.onClick.AddListener(plot.RequestUpgradeActiveTool);
            shopPanel.SetActive(false);
'@ @'
            upgradeToolButton.onClick.AddListener(plot.RequestUpgradeActiveTool);
            var closeShopButton = CreateButton("CloseShop", shopPanel.transform, "FECHAR  [ESC]", new Vector2(140f, -310f), new Vector2(220f, 46f));
            closeShopButton.onClick.AddListener(() => SetShopOpen(false));
            shopPanel.SetActive(false);
'@ 'shop close button'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmHudController.interaction.cs.txt'),$hud,[Text.UTF8Encoding]::new($false))

$clock=[IO.File]::ReadAllText((Join-Path $root 'Assets\_Project\Scripts\Farming\FarmDayClock.cs'))
$clock=Replace-Required $clock @'
        private float originalAmbientIntensity;
        private float checkpointElapsed;
'@ @'
        private float originalAmbientIntensity;
        private UnityEngine.Rendering.AmbientMode originalAmbientMode;
        private Color originalAmbientSkyColor;
        private Color originalAmbientEquatorColor;
        private Color originalAmbientGroundColor;
        private bool originalFog;
        private float checkpointElapsed;
'@ 'lighting capture fields'
$clock=Replace-Required $clock @'
            originalAmbientIntensity = RenderSettings.ambientIntensity;
            if (sun != null)
'@ @'
            originalAmbientIntensity = RenderSettings.ambientIntensity;
            originalAmbientMode = RenderSettings.ambientMode;
            originalAmbientSkyColor = RenderSettings.ambientSkyColor;
            originalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            originalAmbientGroundColor = RenderSettings.ambientGroundColor;
            originalFog = RenderSettings.fog;
            if (sun != null)
'@ 'capture cozy values'
$clock=Replace-Required $clock @'
            var ambientWeatherFactor = Mathf.Lerp(0.78f, 1f, weatherLightMultiplier);
            RenderSettings.ambientIntensity = Mathf.Lerp(originalAmbientIntensity * 0.42f, originalAmbientIntensity, CurrentLightFactor) * ambientWeatherFactor;

            if (sun == null) return;
'@ @'
            var ambientWeatherFactor = Mathf.Lerp(0.90f, 1f, weatherLightMultiplier);
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            var nightSky = new Color(0.28f, 0.38f, 0.58f);
            var daySky = new Color(0.62f, 0.76f, 0.88f);
            var nightEquator = new Color(0.28f, 0.30f, 0.40f);
            var dayEquator = new Color(0.66f, 0.56f, 0.38f);
            var nightGround = new Color(0.16f, 0.18f, 0.25f);
            var dayGround = new Color(0.38f, 0.32f, 0.22f);
            RenderSettings.ambientSkyColor = Color.Lerp(nightSky, daySky, CurrentLightFactor);
            RenderSettings.ambientEquatorColor = Color.Lerp(nightEquator, dayEquator, CurrentLightFactor);
            RenderSettings.ambientGroundColor = Color.Lerp(nightGround, dayGround, CurrentLightFactor);
            RenderSettings.ambientIntensity = Mathf.Lerp(Mathf.Max(0.72f, originalAmbientIntensity * 0.72f), Mathf.Max(1.08f, originalAmbientIntensity * 1.08f), CurrentLightFactor) * ambientWeatherFactor;

            if (sun == null) return;
'@ 'vivid ambient'
$clock=Replace-Required $clock 'Mathf.Lerp(0.20f, 1f, CurrentLightFactor)' 'Mathf.Lerp(0.34f, 1.05f, CurrentLightFactor)' 'night sun readability'
$clock=Replace-Required $clock @'
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            if (sun == null) return;
'@ @'
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientSkyColor = originalAmbientSkyColor;
            RenderSettings.ambientEquatorColor = originalAmbientEquatorColor;
            RenderSettings.ambientGroundColor = originalAmbientGroundColor;
            RenderSettings.fog = originalFog;
            if (sun == null) return;
'@ 'restore scene lighting'
$clock=$clock.Replace('if (minute < 300f) return 0.12f;','if (minute < 300f) return 0.32f;')
$clock=$clock.Replace('Mathf.SmoothStep(0.12f, 0.85f','Mathf.SmoothStep(0.32f, 0.85f')
$clock=$clock.Replace('Mathf.SmoothStep(1f, 0.12f','Mathf.SmoothStep(1f, 0.32f')
$clock=$clock.Replace('return 0.12f;','return 0.32f;')
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmDayClock.cozy.cs.txt'),$clock,[Text.UTF8Encoding]::new($false))

$weather=[IO.File]::ReadAllText((Join-Path $root 'Assets\_Project\Scripts\Farming\FarmWeatherSystem.cs'))
$weather=$weather.Replace('FarmWeather.Cloudy => 0.84f,','FarmWeather.Cloudy => 0.92f,').Replace('FarmWeather.Rain => 0.72f,','FarmWeather.Rain => 0.84f,')
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmWeatherSystem.cozy.cs.txt'),$weather,[Text.UTF8Encoding]::new($false))

$craft=[IO.File]::ReadAllText((Join-Path $root 'Assets\_Project\Scripts\Farming\FarmCrafting.cs'))
$craft=$craft.Replace('public const float InteractionDistance = 1f;','public const float InteractionDistance = 2.2f;')
$craft=$craft.Replace('keyboard.cKey.wasPressedThisFrame','keyboard.fKey.wasPressedThisFrame')
$craft=$craft.Replace('[C]','[F]').Replace('FECHAR  [C]','FECHAR  [F]')
$craft=$craft.Replace('Chegue a at\\u00E9 1 unidade da bancada.','Chegue a at\\u00E9 2,2 unidades da bancada.')
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmCrafting.interaction.cs.txt'),$craft,[Text.UTF8Encoding]::new($false))

Write-Output 'Interaction and cozy lighting staging files created.'
