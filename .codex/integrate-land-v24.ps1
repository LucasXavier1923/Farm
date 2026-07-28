$ErrorActionPreference = 'Stop'

$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'

function Read-UnityScript([string]$assetPath, [string]$tag) {
    $inputPath = Join-Path $project ("Temp\read-$tag-v24.json")
    [System.IO.File]::WriteAllText($inputPath, (@{ filePath = $assetPath; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
    $lines = @(& $cli run-tool script-read $project --input-file $inputPath 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Falha lendo $assetPath`n$($lines -join "`n")" }
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq '{') { $start = $i; break }
    }
    if ($start -lt 0) { throw "JSON ausente na leitura de $assetPath" }
    $response = (($lines[$start..($lines.Count - 1)] -join "`n") | ConvertFrom-Json)
    return [string]$response.structured.result
}

function Submit-UnityScript([string]$assetPath, [string]$content, [string]$tag) {
    $inputPath = Join-Path $project ("Temp\submit-$tag-v24.json")
    [System.IO.File]::WriteAllText($inputPath, (@{ filePath = $assetPath; content = $content } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
    $output = @(& $cli run-tool script-update-or-create $project --input-file $inputPath 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Falha enviando $assetPath`n$($output -join "`n")" }
    $output | Select-Object -Last 12
}

function Replace-Checked([string]$text, [string]$old, [string]$new, [string]$label) {
    if (-not $text.Contains($old)) { throw "Trecho ausente: $label" }
    return $text.Replace($old, $new)
}

$statePath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'
$state = Read-UnityScript $statePath 'land-state'
$state = Replace-Checked $state '        public int Version = 18;' @'
        public int Version = 19;
        public int LandLevel = FarmGameState.MinLandLevel;
'@ 'save version and land'
$state = Replace-Checked $state '        public const int MaxEnergy = 100;' @'
        public const int MaxEnergy = 100;
        public const int MinLandLevel = 1;
        public const int MaxLandLevel = 3;
'@ 'land constants'
$state = Replace-Checked $state '        [SerializeField] private List<string> discoveredItemIds = new();' @'
        [SerializeField] private List<string> discoveredItemIds = new();
        [SerializeField, Range(MinLandLevel, MaxLandLevel)] private int landLevel = MinLandLevel;
'@ 'land field'
$state = Replace-Checked $state '        public IReadOnlyList<string> DiscoveredItemIds => discoveredItemIds;' @'
        public IReadOnlyList<string> DiscoveredItemIds => discoveredItemIds;
        public int LandLevel => landLevel;
        public int LandTileCount => GetLandTileCount(landLevel);
        public bool IsLandMaxed => landLevel >= MaxLandLevel;
'@ 'land properties'
$state = Replace-Checked $state '        public int GetToolUpgradeCost(FarmTool tool)' @'
        public static int GetLandTileCount(int level) => Mathf.Clamp(level, MinLandLevel, MaxLandLevel) switch
        {
            1 => 9,
            2 => 15,
            _ => 25
        };

        public int GetLandUpgradeCost()
        {
            if (landLevel >= MaxLandLevel) return 0;
            return landLevel == 1 ? 500 : 1500;
        }

        public bool CanUpgradeLand()
        {
            var cost = GetLandUpgradeCost();
            return cost > 0 && money >= cost;
        }

        public bool TryUpgradeLand(out int newLevel, out int cost)
        {
            newLevel = landLevel;
            cost = GetLandUpgradeCost();
            if (cost <= 0 || money < cost) return false;
            money -= cost;
            landLevel = Mathf.Clamp(landLevel + 1, MinLandLevel, MaxLandLevel);
            newLevel = landLevel;
            NotifyChanged();
            return true;
        }

        public int GetToolUpgradeCost(FarmTool tool)
'@ 'land economy'
$state = Replace-Checked $state '                Version = 18,' @'
                Version = 19,
                LandLevel = landLevel,
'@ 'land save capture'
$state = Replace-Checked $state '            lastEnergyActionWasFree = false;' @'
            landLevel = data.Version >= 19
                ? Mathf.Clamp(data.LandLevel, MinLandLevel, MaxLandLevel)
                : MinLandLevel;
            lastEnergyActionWasFree = false;
'@ 'land restore'
Submit-UnityScript $statePath $state 'FarmGameState-land'

$plotPath = 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs'
$plot = Read-UnityScript $plotPath 'land-plot'
$plot = Replace-Checked $plot '        private Transform pickupRoot;' @'
        private Transform pickupRoot;
        private Transform tileRoot;
'@ 'tile root'
$plot = Replace-Checked $plot '        public int ActiveToolUpgradeCost => gameState != null ? gameState.GetToolUpgradeCost(activeTool) : 0;' @'
        public int ActiveToolUpgradeCost => gameState != null ? gameState.GetToolUpgradeCost(activeTool) : 0;
        public int LandLevel => gameState != null ? gameState.LandLevel : FarmGameState.MinLandLevel;
        public int LandTileCount => tiles.Count;
        public int LandUpgradeCost => gameState != null ? gameState.GetLandUpgradeCost() : 0;
        public bool CanUpgradeLand => gameState != null && ShopInRange && gameState.CanUpgradeLand();
'@ 'plot land properties'
$plot = Replace-Checked $plot @'
            var root = new GameObject("Farm_Test_Grid").transform;
            root.SetParent(transform, true);
            var offset = (gridSize - 1) * tileSize * 0.5f;
            plotGridOffset = offset;
            var index = 0;
            for (var x = 0; x < gridSize; x++)
            {
                for (var z = 0; z < gridSize; z++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Farm_Tile_{x}_{z}";
                    tile.transform.SetParent(root, true);
                    tile.transform.position = center + new Vector3((x * tileSize) - offset, 0.09f, (z * tileSize) - offset);
                    tile.transform.localScale = new Vector3(tileSize - 0.12f, 0.18f, tileSize - 0.12f);
                    var farmTile = tile.AddComponent<FarmTestTile>();
                    farmTile.Initialize(this, index++, activeCrop);
                    tiles.Add(farmTile);
                }
            }

            CreateSellStation(center, forward, offset);
'@ @'
            tileRoot = new GameObject("Farm_Test_Grid").transform;
            tileRoot.SetParent(transform, true);
            var offset = (gridSize - 1) * tileSize * 0.5f;
            plotGridOffset = offset;
            EnsureFarmTilesForLandLevel();

            CreateSellStation(center, forward, offset);
'@ 'replace fixed grid'
$plot = Replace-Checked $plot '        public void RebuildWorldPickups()' @'
        public void EnsureFarmTilesForLandLevel()
        {
            if (tileRoot == null)
            {
                tileRoot = new GameObject("Farm_Test_Grid").transform;
                tileRoot.SetParent(transform, true);
            }

            var targetCount = gameState != null ? gameState.LandTileCount : FarmGameState.GetLandTileCount(FarmGameState.MinLandLevel);
            while (tiles.Count > targetCount)
            {
                var lastIndex = tiles.Count - 1;
                var removed = tiles[lastIndex];
                tiles.RemoveAt(lastIndex);
                if (removed == null) continue;
                removed.gameObject.SetActive(false);
                Destroy(removed.gameObject);
            }

            var coordinates = BuildLandCoordinates();
            while (tiles.Count < targetCount)
            {
                var index = tiles.Count;
                var coordinate = coordinates[index];
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = index < 9
                    ? $"Farm_Tile_{index / 3}_{index % 3}"
                    : $"Farm_Tile_Expansion_{index:00}";
                tile.transform.SetParent(tileRoot, true);
                tile.transform.position = plotCenter + new Vector3(coordinate.x * tileSize, 0.09f, coordinate.y * tileSize);
                tile.transform.localScale = new Vector3(tileSize - 0.12f, 0.18f, tileSize - 0.12f);
                var farmTile = tile.AddComponent<FarmTestTile>();
                farmTile.Initialize(this, index, activeCrop);
                tiles.Add(farmTile);
            }
        }

        private static List<Vector2Int> BuildLandCoordinates()
        {
            var result = new List<Vector2Int>(25);
            for (var x = -1; x <= 1; x++)
                for (var z = -1; z <= 1; z++)
                    result.Add(new Vector2Int(x, z));
            for (var z = -1; z <= 1; z++) result.Add(new Vector2Int(-2, z));
            for (var z = -1; z <= 1; z++) result.Add(new Vector2Int(2, z));
            for (var x = -2; x <= 2; x++) result.Add(new Vector2Int(x, -2));
            for (var x = -2; x <= 2; x++) result.Add(new Vector2Int(x, 2));
            return result;
        }

        public void RebuildWorldPickups()
'@ 'dynamic land grid'
$plot = Replace-Checked $plot '        public void CycleShopCrop(int direction)' @'
        public void RequestUpgradeLand()
        {
            if (!IsStationInRange())
            {
                feedback = "Chegue mais perto do caixote de vendas.";
                return;
            }
            if (gameState.IsLandMaxed)
            {
                feedback = "Todo o terreno disponível já foi comprado.";
                return;
            }
            var required = gameState.GetLandUpgradeCost();
            if (gameState.TryUpgradeLand(out var newLevel, out var cost))
            {
                EnsureFarmTilesForLandLevel();
                feedback = $"Terreno ampliado para {gameState.LandTileCount} canteiros por ${cost}.";
                actionFeedback?.PlayReward(sellStation.transform.position, newLevel + 2);
                RefreshHoveredArea();
            }
            else feedback = $"Dinheiro insuficiente. A expansão custa ${required}.";
        }

        public void CycleShopCrop(int direction)
'@ 'land purchase request'
$plot = Replace-Checked $plot @'
            gameState.Restore(data);
            if (hud != null) RebuildWorldPickups();
'@ @'
            gameState.Restore(data);
            EnsureFarmTilesForLandLevel();
            if (hud != null) RebuildWorldPickups();
'@ 'load land before tile restore'
$plot = Replace-Checked $plot 'saveStatus = data.Version < 18 ? "Save migrado para v18" : "Save carregado";' 'saveStatus = data.Version < 19 ? "Save migrado para v19" : "Save carregado";' 'migration status'
Submit-UnityScript $plotPath $plot 'FarmTestPlot-land'

$hudPath = 'Assets/_Project/Scripts/Farming/FarmHudController.cs'
$hud = Read-UnityScript $hudPath 'land-hud'
$hud = Replace-Checked $hud '        private Text upgradeToolButtonText;' @'
        private Text upgradeToolButtonText;
        private Button landUpgradeButton;
        private Text landUpgradeButtonText;
'@ 'land button fields'
$hud = Replace-Checked $hud @'
                upgradeToolButtonText.text = !upgradeable ? "SELECIONE UMA FERRAMENTA PARA MELHORAR"
                    : maxed ? $"{plot.ActiveToolDisplayName.ToUpperInvariant()} \u2022 N\u00CDVEL M\u00C1XIMO"
                    : $"MELHORAR {plot.ActiveToolDisplayName.ToUpperInvariant()} \u2022 ${upgradeCost}";
'@ @'
                upgradeToolButtonText.text = !upgradeable ? "SELECIONE UMA FERRAMENTA PARA MELHORAR"
                    : maxed ? $"{plot.ActiveToolDisplayName.ToUpperInvariant()} \u2022 N\u00CDVEL M\u00C1XIMO"
                    : $"MELHORAR {plot.ActiveToolDisplayName.ToUpperInvariant()} \u2022 ${upgradeCost}";
                landUpgradeButton.interactable = plot.CanUpgradeLand;
                landUpgradeButtonText.text = state.IsLandMaxed
                    ? $"TERRENO M\u00C1XIMO \u2022 {plot.LandTileCount} CANTEIROS"
                    : $"COMPRAR TERRENO N{state.LandLevel + 1} \u2022 {FarmGameState.GetLandTileCount(state.LandLevel + 1)} CANTEIROS \u2022 ${plot.LandUpgradeCost}";
'@ 'refresh land button'
$hud = Replace-Checked $hud 'shopPanel == null || upgradeToolButton == null || upgradeToolButtonText == null || inventoryWindow' 'shopPanel == null || upgradeToolButton == null || upgradeToolButtonText == null || landUpgradeButton == null || landUpgradeButtonText == null || inventoryWindow' 'ready land controls'
$hud = Replace-Checked $hud @'
            shopPanel = CreatePanel("Shop", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(620f, 450f), new Vector2(0.5f, 0.5f), PanelColor);
'@ @'
            shopPanel = CreatePanel("Shop", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(620f, 520f), new Vector2(0.5f, 0.5f), PanelColor);
'@ 'larger shop'
$hud = Replace-Checked $hud @'
            upgradeToolButton = CreateButton("UpgradeTool", shopPanel.transform, "MELHORAR FERRAMENTA", new Vector2(20f, -273f), new Vector2(580f, 52f));
            upgradeToolButtonText = upgradeToolButton.GetComponentInChildren<Text>();
            upgradeToolButton.onClick.AddListener(plot.RequestUpgradeActiveTool);
            var closeShopButton = CreateButton("CloseShop", shopPanel.transform, "FECHAR  [ESC]", new Vector2(200f, -351f), new Vector2(220f, 46f));
'@ @'
            upgradeToolButton = CreateButton("UpgradeTool", shopPanel.transform, "MELHORAR FERRAMENTA", new Vector2(20f, -273f), new Vector2(580f, 52f));
            upgradeToolButtonText = upgradeToolButton.GetComponentInChildren<Text>();
            upgradeToolButton.onClick.AddListener(plot.RequestUpgradeActiveTool);
            landUpgradeButton = CreateButton("UpgradeLand", shopPanel.transform, "COMPRAR TERRENO", new Vector2(20f, -341f), new Vector2(580f, 52f));
            landUpgradeButtonText = landUpgradeButton.GetComponentInChildren<Text>();
            landUpgradeButton.onClick.AddListener(plot.RequestUpgradeLand);
            var closeShopButton = CreateButton("CloseShop", shopPanel.transform, "FECHAR  [ESC]", new Vector2(200f, -419f), new Vector2(220f, 46f));
'@ 'land shop button'
Submit-UnityScript $hudPath $hud 'FarmHudController-land'

Write-Output 'LAND_V24_INTEGRATED'
