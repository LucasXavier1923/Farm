$ErrorActionPreference = 'Stop'
$project = 'D:\Dev\Unity\Farm\Farm'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'

function Read-UnityScript([string]$inputFile) {
    $raw = (& $cli run-tool script-read $project --input-file $inputFile 2>&1) -join "`n"
    $jsonStart = $raw.IndexOf('{', $raw.IndexOf('SUCCESS: Response:'))
    if ($jsonStart -lt 0) { throw "Resposta inválida ao ler $inputFile" }
    return ($raw.Substring($jsonStart) | ConvertFrom-Json).structured.result
}

function Replace-Once([string]$content, [string]$old, [string]$new, [string]$label) {
    $first = $content.IndexOf($old, [StringComparison]::Ordinal)
    if ($first -lt 0) { throw "Trecho não encontrado: $label" }
    if ($content.IndexOf($old, $first + $old.Length, [StringComparison]::Ordinal) -ge 0) { throw "Trecho duplicado: $label" }
    return $content.Substring(0, $first) + $new + $content.Substring($first + $old.Length)
}

function Submit-UnityScript([string]$path, [string]$content, [string]$requestId) {
    $payload = @{ filePath = $path; content = $content; requestId = $requestId } | ConvertTo-Json -Compress
    $result = ($payload | & $cli run-tool script-update-or-create $project --input-file - 2>&1) -join "`n"
    if ($result -match 'timed out after 60 seconds') { Write-Output "Envio aplicado; resposta interrompida pelo reload: $path"; return }
    if ($LASTEXITCODE -ne 0 -or $result -notmatch 'SUCCESS: script-update-or-create completed') { throw $result }
    Write-Output "Atualizado: $path"
}

$content = Read-UnityScript '.codex\read-farm-game-state.json'
if ($content -notmatch 'MaxToolLevel') {
    $content = Replace-Once $content '        public int Version = 8;' '        public int Version = 9;' 'State.save version'
    $content = Replace-Once $content @'
        public int WorldSeed = FarmGameState.DefaultWorldSeed;
'@ @'
        public int WorldSeed = FarmGameState.DefaultWorldSeed;
        public int HoeLevel = 1;
        public int WateringCanLevel = 1;
        public int HarvestLevel = 1;
'@ 'State.save levels'
    $content = Replace-Once $content @'
        public const int DefaultWorldSeed = 7122040;
'@ @'
        public const int DefaultWorldSeed = 7122040;
        public const int MaxToolLevel = 3;
'@ 'State.max level'
    $content = Replace-Once $content @'
        [SerializeField] private int worldSeed = DefaultWorldSeed;
'@ @'
        [SerializeField] private int worldSeed = DefaultWorldSeed;
        [SerializeField, Range(1, MaxToolLevel)] private int hoeLevel = 1;
        [SerializeField, Range(1, MaxToolLevel)] private int wateringCanLevel = 1;
        [SerializeField, Range(1, MaxToolLevel)] private int harvestLevel = 1;
'@ 'State.serialized levels'
    $content = Replace-Once $content @'
        public int WorldSeed => worldSeed;
'@ @'
        public int WorldSeed => worldSeed;
        public int HoeLevel => hoeLevel;
        public int WateringCanLevel => wateringCanLevel;
        public int HarvestLevel => harvestLevel;
'@ 'State.level props'
    $content = Replace-Once $content @'
        public bool TrySellAll(CropDefinition crop, out int quantity, out int earned)
'@ @'
        public int GetToolLevel(FarmTool tool) => tool switch
        {
            FarmTool.Hoe => hoeLevel,
            FarmTool.WateringCan => wateringCanLevel,
            FarmTool.Harvest => harvestLevel,
            _ => 1
        };

        public int GetToolUpgradeCost(FarmTool tool)
        {
            if (!IsUpgradeableTool(tool)) return 0;
            var level = GetToolLevel(tool);
            if (level >= MaxToolLevel) return 0;
            return level == 1 ? 150 : 500;
        }

        public bool CanUpgradeTool(FarmTool tool)
        {
            var cost = GetToolUpgradeCost(tool);
            return cost > 0 && money >= cost;
        }

        public bool TryUpgradeTool(FarmTool tool, out int newLevel, out int cost)
        {
            newLevel = GetToolLevel(tool);
            cost = GetToolUpgradeCost(tool);
            if (cost <= 0 || money < cost) return false;
            money -= cost;
            SetToolLevelInternal(tool, newLevel + 1);
            newLevel = GetToolLevel(tool);
            NotifyChanged();
            return true;
        }

        public static bool IsUpgradeableTool(FarmTool tool) =>
            tool is FarmTool.Hoe or FarmTool.WateringCan or FarmTool.Harvest;

        private void SetToolLevelInternal(FarmTool tool, int value)
        {
            value = Mathf.Clamp(value, 1, MaxToolLevel);
            if (tool == FarmTool.Hoe) hoeLevel = value;
            else if (tool == FarmTool.WateringCan) wateringCanLevel = value;
            else if (tool == FarmTool.Harvest) harvestLevel = value;
        }

        public bool TrySellAll(CropDefinition crop, out int quantity, out int earned)
'@ 'State.upgrade methods'
    $content = Replace-Once $content '                Version = 8,' '                Version = 9,' 'State.snapshot version'
    $content = Replace-Once $content @'
                WorldSeed = worldSeed,
'@ @'
                WorldSeed = worldSeed,
                HoeLevel = hoeLevel,
                WateringCanLevel = wateringCanLevel,
                HarvestLevel = harvestLevel,
'@ 'State.snapshot levels'
    $content = Replace-Once $content @'
            SetWorldSeed(data.Version >= 8 ? data.WorldSeed : DefaultWorldSeed);
            NotifyChanged();
'@ @'
            SetWorldSeed(data.Version >= 8 ? data.WorldSeed : DefaultWorldSeed);
            if (data.Version >= 9)
            {
                SetToolLevelInternal(FarmTool.Hoe, data.HoeLevel);
                SetToolLevelInternal(FarmTool.WateringCan, data.WateringCanLevel);
                SetToolLevelInternal(FarmTool.Harvest, data.HarvestLevel);
            }
            else
            {
                hoeLevel = 1;
                wateringCanLevel = 1;
                harvestLevel = 1;
            }
            NotifyChanged();
'@ 'State.restore levels'
    Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmGameState.cs' $content 'tool-upgrades-v9-state'
} else { Write-Output 'Já atualizado: FarmGameState.cs' }

$content = Read-UnityScript '.codex\read-farm-test-plot.json'
if ($content -notmatch 'GetAffectedTiles') {
    $content = Replace-Once $content @'
        public string ActiveToolDisplayName => ToolName(activeTool);
'@ @'
        public int ActiveToolLevel => gameState != null ? gameState.GetToolLevel(activeTool) : 1;
        public string ActiveToolAreaText => ToolAreaText(activeTool, ActiveToolLevel);
        public string ActiveToolDisplayName => FarmGameState.IsUpgradeableTool(activeTool)
            ? $"{ToolName(activeTool)} L{ActiveToolLevel} \u2022 {ActiveToolAreaText}"
            : ToolName(activeTool);
        public int ActiveToolUpgradeCost => gameState != null ? gameState.GetToolUpgradeCost(activeTool) : 0;
        public bool CanUpgradeActiveTool => gameState != null && ShopInRange && gameState.CanUpgradeTool(activeTool);
'@ 'Plot.tool props'
    $content = Replace-Once $content @'
                if (keyboard.bKey.wasPressedThisFrame && ShopVisible) RequestBuySeeds();
'@ @'
                if (keyboard.bKey.wasPressedThisFrame && ShopVisible) RequestBuySeeds();
                if (keyboard.uKey.wasPressedThisFrame && ShopVisible) RequestUpgradeActiveTool();
'@ 'Plot.upgrade input'
    $content = Replace-Once $content @'
        public void CycleShopCrop(int direction)
'@ @'
        public void RequestUpgradeActiveTool()
        {
            if (!IsStationInRange())
            {
                feedback = "Chegue mais perto do caixote de vendas.";
                return;
            }
            if (!FarmGameState.IsUpgradeableTool(activeTool))
            {
                feedback = "Selecione a enxada, o regador ou a ferramenta de colheita.";
                return;
            }
            var currentLevel = gameState.GetToolLevel(activeTool);
            if (currentLevel >= FarmGameState.MaxToolLevel)
            {
                feedback = $"{ToolName(activeTool)} j\u00E1 est\u00E1 no n\u00EDvel m\u00E1ximo.";
                return;
            }
            var required = gameState.GetToolUpgradeCost(activeTool);
            if (gameState.TryUpgradeTool(activeTool, out var newLevel, out var cost))
            {
                feedback = $"{ToolName(activeTool)} melhorada para L{newLevel}: {ToolAreaText(activeTool, newLevel)} por ${cost}.";
                RefreshHoveredArea();
            }
            else feedback = $"Dinheiro insuficiente. A melhoria custa ${required}.";
        }

        public void CycleShopCrop(int direction)
'@ 'Plot.upgrade action'
    $content = Replace-Once $content @'
            else activeTool = FarmTool.None;

            if (!showFeedback) return;
'@ @'
            else activeTool = FarmTool.None;

            RefreshHoveredArea();
            if (!showFeedback) return;
'@ 'Plot.refresh area selection'
    $content = Replace-Once $content @'
        private void SetHoveredTile(FarmTestTile tile)
        {
            if (hoveredTile == tile) return;
            if (hoveredTile != null) hoveredTile.SetHovered(false);
            hoveredTile = tile;
            if (hoveredTile != null) hoveredTile.SetHovered(true);
        }
'@ @'
        private void SetHoveredTile(FarmTestTile tile)
        {
            if (hoveredTile == tile) return;
            hoveredTile = tile;
            RefreshHoveredArea();
        }

        private void RefreshHoveredArea()
        {
            foreach (var tile in tiles) tile.SetHovered(false);
            if (hoveredTile == null) return;
            foreach (var tile in GetAffectedTiles(hoveredTile, activeTool)) tile.SetHovered(true);
        }
'@ 'Plot.area highlight'
    $content = Replace-Once $content @'
                feedback = hoveredTile.Use(activeTool, gameState, selectedItemId);
'@ @'
                feedback = UseToolOnTile(hoveredTile);
'@ 'Plot.primary area call'
    $content = Replace-Once $content @'
        private void InteractWithStation(bool buySeeds)
'@ @'
        public IReadOnlyList<FarmTestTile> GetAffectedTiles(FarmTestTile target, FarmTool tool)
        {
            var result = new List<FarmTestTile>();
            if (target == null) return result;
            var level = gameState != null && FarmGameState.IsUpgradeableTool(tool) ? gameState.GetToolLevel(tool) : 1;
            if (level <= 1 || !FarmGameState.IsUpgradeableTool(tool))
            {
                result.Add(target);
                return result;
            }

            var center = target.Index;
            var centerRow = center / gridSize;
            var centerColumn = center % gridSize;
            var rowRadius = level >= 3 ? 1 : 0;
            for (var rowOffset = -rowRadius; rowOffset <= rowRadius; rowOffset++)
            {
                for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
                {
                    var row = centerRow + rowOffset;
                    var column = centerColumn + columnOffset;
                    if (row < 0 || row >= gridSize || column < 0 || column >= gridSize) continue;
                    var index = (row * gridSize) + column;
                    if (index >= 0 && index < tiles.Count) result.Add(tiles[index]);
                }
            }
            return result;
        }

        public string UseToolOnTile(FarmTestTile target)
        {
            if (target == null || gameState == null) return "Nenhum canteiro selecionado.";
            var affected = GetAffectedTiles(target, activeTool);
            var changed = 0;
            var successMessage = string.Empty;
            var targetMessage = string.Empty;
            foreach (var tile in affected)
            {
                var before = tile.CaptureSaveData();
                var message = tile.Use(activeTool, gameState, selectedItemId);
                var after = tile.CaptureSaveData();
                var didChange = before.State != after.State || !string.Equals(before.CropId, after.CropId, StringComparison.OrdinalIgnoreCase);
                if (didChange)
                {
                    changed++;
                    successMessage = message;
                }
                if (tile == target) targetMessage = message;
            }

            if (changed <= 1) return !string.IsNullOrEmpty(successMessage) ? successMessage : targetMessage;
            return activeTool switch
            {
                FarmTool.Hoe => $"Preparou {changed} canteiros de uma vez.",
                FarmTool.WateringCan => $"Regou {changed} canteiros de uma vez.",
                FarmTool.Harvest => $"Colheu {changed} canteiros de uma vez.",
                _ => successMessage
            };
        }

        private void InteractWithStation(bool buySeeds)
'@ 'Plot.area methods'
    $content = Replace-Once $content '            saveStatus = data.Version < 8 ? "Save migrado para v8" : "Save carregado";' '            saveStatus = data.Version < 9 ? "Save migrado para v9" : "Save carregado";' 'Plot.save status'
    $content = Replace-Once $content @'
        private static string ToolName(FarmTool tool) => tool switch
'@ @'
        private static string ToolAreaText(FarmTool tool, int level)
        {
            if (!FarmGameState.IsUpgradeableTool(tool)) return "1 canteiro";
            return level switch
            {
                1 => "1 canteiro",
                2 => "linha de 3",
                _ => "\u00E1rea 3x3"
            };
        }

        private static string ToolName(FarmTool tool) => tool switch
'@ 'Plot.area label'
    $content = Replace-Once $content @'
        public string CropId => cropDefinition != null ? cropDefinition.Id : string.Empty;
'@ @'
        public int Index => index;
        public string CropId => cropDefinition != null ? cropDefinition.Id : string.Empty;
'@ 'Tile.index prop'
    Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $content 'tool-upgrades-v9-plot'
} else { Write-Output 'Já atualizado: FarmTestPlot.cs' }

$content = Read-UnityScript '.codex\read-farm-hud-controller.json'
if ($content -notmatch 'upgradeToolButton') {
    $content = Replace-Once $content @'
        private Button nextCropButton;
'@ @'
        private Button nextCropButton;
        private Button upgradeToolButton;
        private Text upgradeToolButtonText;
'@ 'Hud.upgrade fields'
    $content = Replace-Once $content @'
                nextCropButton.interactable = plot.ShopCropCount > 1;
'@ @'
                nextCropButton.interactable = plot.ShopCropCount > 1;
                var upgradeable = FarmGameState.IsUpgradeableTool(plot.ActiveTool);
                var maxed = upgradeable && plot.ActiveToolLevel >= FarmGameState.MaxToolLevel;
                var upgradeCost = plot.ActiveToolUpgradeCost;
                upgradeToolButton.interactable = plot.CanUpgradeActiveTool;
                upgradeToolButtonText.text = !upgradeable ? "SELECIONE UMA FERRAMENTA PARA MELHORAR"
                    : maxed ? $"{plot.ActiveToolDisplayName.ToUpperInvariant()} \u2022 N\u00CDVEL M\u00C1XIMO"
                    : $"MELHORAR {plot.ActiveToolDisplayName.ToUpperInvariant()} \u2022 ${upgradeCost}";
'@ 'Hud.upgrade state'
    $content = Replace-Once $content @'
            if (resourcesText == null || toolText == null || promptText == null || feedbackText == null || saveText == null || tutorialText == null || clockText == null || weatherText == null || inventorySummaryText == null || shopInfoText == null || shopPanel == null || inventoryWindow == null || inventoryGroup == null || storageWindow == null || storageGroup == null) return false;
'@ @'
            if (resourcesText == null || toolText == null || promptText == null || feedbackText == null || saveText == null || tutorialText == null || clockText == null || weatherText == null || inventorySummaryText == null || shopInfoText == null || shopPanel == null || upgradeToolButton == null || upgradeToolButtonText == null || inventoryWindow == null || inventoryGroup == null || storageWindow == null || storageGroup == null) return false;
'@ 'Hud.ready upgrade'
    $content = Replace-Once $content @'
            shopPanel = CreatePanel("Shop", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(500f, 250f), new Vector2(0.5f, 0.5f), PanelColor);
'@ @'
            shopPanel = CreatePanel("Shop", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(500f, 320f), new Vector2(0.5f, 0.5f), PanelColor);
'@ 'Hud.shop height'
    $content = Replace-Once $content @'
            buyButton.onClick.AddListener(plot.RequestBuySeeds);
            shopPanel.SetActive(false);
'@ @'
            buyButton.onClick.AddListener(plot.RequestBuySeeds);
            upgradeToolButton = CreateButton("UpgradeTool", shopPanel.transform, "MELHORAR FERRAMENTA", new Vector2(20f, -242f), new Vector2(460f, 52f));
            upgradeToolButtonText = upgradeToolButton.GetComponentInChildren<Text>();
            upgradeToolButton.onClick.AddListener(plot.RequestUpgradeActiveTool);
            shopPanel.SetActive(false);
'@ 'Hud.upgrade button'
    $content = Replace-Once $content @'
            if (toolId == "hoe") { icon = "E"; label = "ENXADA"; color = new Color(0.88f, 0.62f, 0.27f); }
            else if (toolId == "watering_can") { icon = "R"; label = "REGADOR"; color = new Color(0.35f, 0.72f, 0.95f); }
            else if (toolId == "harvest") { icon = "C"; label = "COLHER"; color = new Color(0.55f, 0.88f, 0.42f); }
'@ @'
            if (toolId == "hoe") { icon = "E"; label = $"ENXADA L{state.GetToolLevel(FarmTool.Hoe)}"; color = new Color(0.88f, 0.62f, 0.27f); }
            else if (toolId == "watering_can") { icon = "R"; label = $"REGADOR L{state.GetToolLevel(FarmTool.WateringCan)}"; color = new Color(0.35f, 0.72f, 0.95f); }
            else if (toolId == "harvest") { icon = "C"; label = $"COLHER L{state.GetToolLevel(FarmTool.Harvest)}"; color = new Color(0.55f, 0.88f, 0.42f); }
'@ 'Hud.hotbar levels'
    Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $content 'tool-upgrades-v9-hud'
} else { Write-Output 'Já atualizado: FarmHudController.cs' }
