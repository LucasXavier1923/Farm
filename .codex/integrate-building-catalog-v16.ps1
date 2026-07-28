$ErrorActionPreference = 'Stop'

$project = 'D:\Dev\Unity\Farm\Farm'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'

function Replace-Checked([string]$content, [string]$old, [string]$new, [string]$label) {
    $content = $content.Replace("`r`n", "`n")
    $old = $old.Replace("`r`n", "`n")
    $new = $new.Replace("`r`n", "`n")
    if (-not $content.Contains($old)) {
        throw "Trecho ausente: $label"
    }
    return $content.Replace($old, $new)
}

function Submit-Script([string]$path, [string]$content, [string]$requestId) {
    $payload = @{
        filePath = $path
        content = $content
        requestId = $requestId
    } | ConvertTo-Json -Compress
    $result = $payload | & $cli run-tool script-update-or-create $project --input-file -
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao atualizar $path"
    }
    $result | Select-Object -Last 14
    Start-Sleep -Seconds 2
}

$definitionPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmBuildableDefinition.cs'
$definition = [IO.File]::ReadAllText($definitionPath)
$definition = Replace-Checked $definition `
    '    public enum FarmBuildableFunction { Decorative, Sprinkler, Scarecrow, Fence }' `
    "    public enum FarmBuildableFunction { Decorative, Sprinkler, Scarecrow, Fence }`r`n    public enum FarmBuildableCategory { Farming, Automation, Fences, Decoration }" `
    'enum de categoria'
$definition = Replace-Checked $definition `
    "        public string DisplayName;`r`n        public ItemDefinition KitItem;" `
    "        public string DisplayName;`r`n        public FarmBuildableCategory Category;`r`n        [TextArea] public string Description;`r`n        public ItemDefinition KitItem;" `
    'dados do catalogo'
Submit-Script 'Assets/_Project/Scripts/Farming/FarmBuildableDefinition.cs' $definition 'build-catalog-definition-v16'

$hudPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmHudController.cs'
$hud = [IO.File]::ReadAllText($hudPath)
$hud = Replace-Checked $hud `
    "        private bool craftingOpen;`r`n        private Text dailyOrdersSummaryText;" `
    "        private bool craftingOpen;`r`n        private bool buildingCatalogOpen;`r`n        private Text dailyOrdersSummaryText;" `
    'estado modal de construcao'
$hud = Replace-Checked $hud `
    "        public bool IsCraftingOpen => craftingOpen;`r`n        public bool IsShopOpen => shopOpen;" `
    "        public bool IsCraftingOpen => craftingOpen;`r`n        public bool IsBuildingCatalogOpen => buildingCatalogOpen;`r`n        public bool IsShopOpen => shopOpen;" `
    'propriedade modal de construcao'
$hud = Replace-Checked $hud `
    "        public void SetCraftingOpen(bool value)`r`n        {`r`n            if (value && shopOpen) SetShopOpen(false);`r`n            craftingOpen = value;`r`n            UpdateModalState();`r`n        }" `
    "        public void SetCraftingOpen(bool value)`r`n        {`r`n            if (value && shopOpen) SetShopOpen(false);`r`n            craftingOpen = value;`r`n            UpdateModalState();`r`n        }`r`n`r`n        public void SetBuildingCatalogOpen(bool value)`r`n        {`r`n            buildingCatalogOpen = value;`r`n            UpdateModalState();`r`n        }" `
    'setter modal de construcao'
$hud = Replace-Checked $hud `
    '        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen || masteryOpen || craftingOpen || shopOpen;' `
    '        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen || masteryOpen || craftingOpen || buildingCatalogOpen || shopOpen;' `
    'agregacao modal'
$hud = Replace-Checked $hud `
    "            var keyboard = Keyboard.current;`r`n            if (keyboard == null) return;`r`n            if (keyboard.jKey.wasPressedThisFrame)" `
    "            var keyboard = Keyboard.current;`r`n            if (keyboard == null) return;`r`n            if (buildingCatalogOpen) return;`r`n            if (keyboard.jKey.wasPressedThisFrame)" `
    'bloqueio de atalhos sobre o catalogo'
Submit-Script 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $hud 'build-catalog-hud-v16'

$buildingPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmBuildingSystem.cs'
$building = [IO.File]::ReadAllText($buildingPath)
$building = Replace-Checked $building `
    "        private Text launcherText;`r`n        private GameObject instructionPanel;" `
    "        private Text launcherText;`r`n        private FarmBuildingCatalog catalog;`r`n        private GameObject instructionPanel;" `
    'campo catalogo'
$building = Replace-Checked $building `
    "        public string ActiveItemId => activeDefinition != null && activeDefinition.KitItem != null`r`n            ? activeDefinition.KitItem.Id`r`n            : string.Empty;" `
    "        public string ActiveItemId => activeDefinition != null && activeDefinition.KitItem != null`r`n            ? activeDefinition.KitItem.Id`r`n            : string.Empty;`r`n        public FarmBuildingCatalog Catalog => catalog;" `
    'propriedade catalogo'
$building = Replace-Checked $building `
    "            CreateInterface();`r`n            RebuildFromState();" `
    "            CreateInterface();`r`n            catalog = GetComponent<FarmBuildingCatalog>();`r`n            if (catalog == null) catalog = gameObject.AddComponent<FarmBuildingCatalog>();`r`n            catalog.Initialize(this, state, hud, canvas);`r`n            RebuildFromState();" `
    'inicializacao catalogo'
$building = Replace-Checked $building `
    '        private void OnDisable() => CancelPlacement();' `
    "        private void OnDisable()`r`n        {`r`n            catalog?.Close();`r`n            CancelPlacement();`r`n        }" `
    'fechamento catalogo'
$oldRefresh = @'
        private void RefreshLauncher()
        {
            if (launcherText == null || launcherButton == null || state == null || plot == null) return;
            var definition = FarmBuildableDatabase.GetByItemId(plot.SelectedItemId);
            var available = definition != null ? state.GetQuantity(definition.KitItem.Id) : 0;
            launcherButton.interactable = !FarmHudController.IsModalOpen && !IsPlacing && available > 0;
            launcherText.text = definition == null
                ? "CONSTRU\u00C7\u00C3O  \u2022  selecione um kit"
                : available <= 0
                    ? $"{definition.DisplayName.ToUpperInvariant()}  \u2022  sem kits"
                    : $"{definition.DisplayName.ToUpperInvariant()} x{available}  [G]";
            launcherText.color = launcherButton.interactable ? ValidColor : new Color(0.66f, 0.70f, 0.62f);
        }
'@
$newRefresh = @'
        private void RefreshLauncher()
        {
            if (launcherText == null || launcherButton == null || state == null) return;
            var ownedKits = 0;
            foreach (var definition in FarmBuildableDatabase.Definitions)
                if (definition != null && definition.KitItem != null)
                    ownedKits += state.GetQuantity(definition.KitItem.Id);
            launcherButton.interactable = !FarmHudController.IsModalOpen && !IsPlacing &&
                FarmBuildableDatabase.Definitions.Count > 0;
            launcherText.text = $"CONSTRUIR  [B]  \u2022  {ownedKits} kit{(ownedKits == 1 ? string.Empty : "s")}";
            launcherText.color = launcherButton.interactable ? ValidColor : new Color(0.66f, 0.70f, 0.62f);
        }
'@
$building = Replace-Checked $building $oldRefresh $newRefresh 'launcher do catalogo'
$building = Replace-Checked $building `
    '            launcherButton.onClick.AddListener(() => BeginPlacement(plot != null ? plot.SelectedItemId : null));' `
    '            launcherButton.onClick.AddListener(() => catalog?.Open());' `
    'botao do catalogo'
Submit-Script 'Assets/_Project/Scripts/Farming/FarmBuildingSystem.cs' $building 'build-catalog-system-v16'

$catalogContent = [IO.File]::ReadAllText((Join-Path $project '.codex\FarmBuildingCatalog.v16.cs.txt'))
Submit-Script 'Assets/_Project/Scripts/Farming/FarmBuildingCatalog.cs' $catalogContent 'build-catalog-ui-v16'
