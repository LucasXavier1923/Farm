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

$content = Read-UnityScript '.codex\read-farm-content.json'
if ($content -notmatch 'GetCropForSeed') {
$content = Replace-Once $content @'
        public static IReadOnlyCollection<ItemDefinition> Items
'@ @'
        public static CropDefinition GetCropForSeed(string seedItemId)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(seedItemId)) return null;
            foreach (var crop in crops.Values)
                if (crop != null && crop.SeedItem != null && string.Equals(crop.SeedItem.Id, seedItemId, StringComparison.OrdinalIgnoreCase)) return crop;
            return null;
        }

        public static IReadOnlyCollection<ItemDefinition> Items
'@ 'FarmContent.GetCropForSeed'
Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmContent.cs' $content 'multicrop-content'
} else { Write-Output 'Já atualizado: FarmContent.cs' }

$content = Read-UnityScript '.codex\read-farm-game-state.json'
if ($content -notmatch 'TrySellAllCrops') {
$content = Replace-Once $content @'
        public bool TryBuySeedPack(CropDefinition crop, out int amount, out int cost)
'@ @'
        public bool TrySellAllCrops(out int quantity, out int earned)
        {
            quantity = 0;
            earned = 0;
            var soldItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var crop in FarmContentDatabase.Crops)
            {
                if (crop == null || crop.HarvestItem == null || !soldItemIds.Add(crop.HarvestItem.Id)) continue;
                var cropQuantity = GetQuantity(crop.HarvestItem.Id);
                if (cropQuantity <= 0) continue;
                RemoveFromList(inventory, crop.HarvestItem.Id, cropQuantity);
                quantity += cropQuantity;
                earned += cropQuantity * Mathf.Max(0, crop.HarvestItem.BaseSellPrice);
            }
            if (quantity <= 0) return false;
            money += earned;
            NotifyChanged();
            return true;
        }

        public bool TryBuySeedPack(CropDefinition crop, out int amount, out int cost)
'@ 'FarmGameState.TrySellAllCrops'
Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmGameState.cs' $content 'multicrop-state'
} else { Write-Output 'Já atualizado: FarmGameState.cs' }

$content = Read-UnityScript '.codex\read-farm-test-plot.json'
if ($content -notmatch 'ShopCropCount') {
$content = Replace-Once $content @'
        private readonly List<FarmTestTile> tiles = new();
'@ @'
        private readonly List<FarmTestTile> tiles = new();
        private readonly List<CropDefinition> shopCrops = new();
        private int shopCropIndex;
'@ 'Plot.fields'
$content = Replace-Once $content @'
        public CropDefinition ActiveCrop => activeCrop;
'@ @'
        public CropDefinition ActiveCrop => activeCrop;
        public CropDefinition ShopCrop => shopCrops.Count > 0 ? shopCrops[Mathf.Clamp(shopCropIndex, 0, shopCrops.Count - 1)] : activeCrop;
        public int ShopCropCount => shopCrops.Count;
        public int ShopCropIndex => shopCropIndex;
'@ 'Plot.properties'
$content = Replace-Once $content @'
            var playerObject = GameObject.Find(playerName);
'@ @'
            RefreshShopCatalog();
            var playerObject = GameObject.Find(playerName);
'@ 'Plot.start catalog'
$content = Replace-Once $content @'
        public void RequestStorage()
'@ @'
        public void CycleShopCrop(int direction)
        {
            if (shopCrops.Count <= 1 || direction == 0) return;
            shopCropIndex = (shopCropIndex + (direction > 0 ? 1 : -1) + shopCrops.Count) % shopCrops.Count;
            feedback = $"Catálogo: {ShopCrop.DisplayName}.";
        }

        public void RequestStorage()
'@ 'Plot.cycle shop'
$content = Replace-Once $content @'
                var item = FarmContentDatabase.GetItem(selectedItemId);
                activeTool = item != null && item.Category == ItemCategory.Seed ? FarmTool.Seeds : FarmTool.None;
'@ @'
                var item = FarmContentDatabase.GetItem(selectedItemId);
                activeTool = item != null && item.Category == ItemCategory.Seed ? FarmTool.Seeds : FarmTool.None;
                var selectedCrop = FarmContentDatabase.GetCropForSeed(selectedItemId);
                if (selectedCrop != null) activeCrop = selectedCrop;
'@ 'Plot.seed selects crop'
$content = Replace-Once $content @'
        private void CreatePlotAndStation()
'@ @'
        private void RefreshShopCatalog()
        {
            var selectedId = ShopCrop != null ? ShopCrop.Id : activeCrop != null ? activeCrop.Id : string.Empty;
            shopCrops.Clear();
            foreach (var crop in FarmContentDatabase.Crops)
                if (crop != null && crop.SeedItem != null && crop.HarvestItem != null) shopCrops.Add(crop);
            shopCrops.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.DisplayName, right.DisplayName));
            shopCropIndex = Mathf.Max(0, shopCrops.FindIndex(crop => string.Equals(crop.Id, selectedId, StringComparison.OrdinalIgnoreCase)));
        }

        private void CreatePlotAndStation()
'@ 'Plot.refresh catalog'
$old = @'
            if (buySeeds)
            {
                if (gameState.TryBuySeedPack(activeCrop, out var amount, out var cost))
                {
                    var advanced = gameState.MarkMilestone(FarmMilestone.BoughtSeeds);
                    feedback = $"Comprou {amount} {activeCrop.SeedItem.DisplayName.ToLowerInvariant()} por ${cost}.";
                    if (advanced && gameState.Tutorial.IsComplete) feedback += " Primeira colheita conclu\u00EDda: b\u00F4nus de $50!";
                }
                else
                    feedback = $"Compra indispon\u00EDvel. Custo: ${activeCrop.SeedPackPrice}; confira dinheiro e espa\u00E7o.";
            }
            else
            {
                if (gameState.TrySellAll(activeCrop, out var quantity, out var earned))
                {
                    var advanced = gameState.MarkMilestone(FarmMilestone.Sold);
                    feedback = $"Vendeu {quantity} {activeCrop.DisplayName.ToLowerInvariant()}(s) por ${earned}.";
                    if (advanced && gameState.Tutorial.IsComplete) feedback += " Primeira colheita conclu\u00EDda: b\u00F4nus de $50!";
                }
                else
                    feedback = $"N\u00E3o h\u00E1 {activeCrop.DisplayName.ToLowerInvariant()} no invent\u00E1rio para vender.";
            }
'@
$new = @'
            var shopCrop = ShopCrop;
            if (buySeeds)
            {
                if (shopCrop != null && gameState.TryBuySeedPack(shopCrop, out var amount, out var cost))
                {
                    var advanced = gameState.MarkMilestone(FarmMilestone.BoughtSeeds);
                    feedback = $"Comprou {amount} {shopCrop.SeedItem.DisplayName.ToLowerInvariant()} por ${cost}.";
                    if (advanced && gameState.Tutorial.IsComplete) feedback += " Primeira colheita conclu\u00EDda: b\u00F4nus de $50!";
                }
                else
                    feedback = $"Compra indispon\u00EDvel. Custo: ${shopCrop?.SeedPackPrice ?? 0}; confira dinheiro e espa\u00E7o.";
            }
            else
            {
                if (gameState.TrySellAllCrops(out var quantity, out var earned))
                {
                    var advanced = gameState.MarkMilestone(FarmMilestone.Sold);
                    feedback = $"Vendeu {quantity} produto(s) por ${earned}.";
                    if (advanced && gameState.Tutorial.IsComplete) feedback += " Primeira colheita conclu\u00EDda: b\u00F4nus de $50!";
                }
                else feedback = "N\u00E3o h\u00E1 produtos colhidos no invent\u00E1rio para vender.";
            }
'@
$content = Replace-Once $content $old $new 'Plot.commerce'
$content = Replace-Once $content @'
        public string StatusText => state switch
'@ @'
        public string CropId => cropDefinition != null ? cropDefinition.Id : string.Empty;

        public string StatusText => state switch
'@ 'Tile.crop id'
$old = @'
            if (tool == FarmTool.Seeds && state == State.Tilled)
            {
                if (!string.Equals(selectedItemId, cropDefinition.SeedItem.Id, StringComparison.OrdinalIgnoreCase))
                    return $"Selecione {cropDefinition.SeedItem.DisplayName.ToLowerInvariant()} na barra r\u00E1pida.";
                if (!inventory.TryRemoveItem(cropDefinition.SeedItem.Id, 1))
                    return $"Voc\u00EA n\u00E3o possui {cropDefinition.SeedItem.DisplayName.ToLowerInvariant()}.";
                state = State.Seeded;
'@
$new = @'
            if (tool == FarmTool.Seeds && state == State.Tilled)
            {
                var selectedCrop = FarmContentDatabase.GetCropForSeed(selectedItemId);
                if (selectedCrop == null)
                    return "Selecione uma semente válida na barra rápida.";
                if (!inventory.TryRemoveItem(selectedCrop.SeedItem.Id, 1))
                    return $"Voc\u00EA n\u00E3o possui {selectedCrop.SeedItem.DisplayName.ToLowerInvariant()}.";
                cropDefinition = selectedCrop;
                state = State.Seeded;
'@
$content = Replace-Once $content $old $new 'Tile.dynamic planting'
$content = Replace-Once $content @'
        public void Restore(FarmTileSaveData data)
        {
            var maxState = Enum.GetValues(typeof(State)).Length - 1;
'@ @'
        public void Restore(FarmTileSaveData data)
        {
            var savedCrop = FarmContentDatabase.GetCrop(data.CropId);
            if (savedCrop != null) cropDefinition = savedCrop;
            var maxState = Enum.GetValues(typeof(State)).Length - 1;
'@ 'Tile.restore crop'
Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $content 'multicrop-plot'
} else { Write-Output 'Já atualizado: FarmTestPlot.cs' }

$content = Read-UnityScript '.codex\read-farm-hud-controller.json'
if ($content -notmatch 'previousCropButton') {
$content = Replace-Once $content @'
        private Button buyButton;
'@ @'
        private Button buyButton;
        private Button previousCropButton;
        private Button nextCropButton;
'@ 'Hud.crop buttons fields'
$content = Replace-Once $content @'
            var crop = plot.ActiveCrop;
'@ @'
            var crop = plot.ActiveCrop;
            var shopCrop = plot.ShopCrop ?? crop;
'@ 'Hud.shop crop variable'
$content = Replace-Once $content @'
                shopInfoText.text = $"{crop.SeedPackAmount} sementes: ${crop.SeedPackPrice}\nVenda: ${crop.HarvestItem.BaseSellPrice} por {crop.DisplayName.ToLowerInvariant()}";
                sellButton.interactable = plot.ShopInRange;
                buyButton.interactable = plot.ShopInRange;
'@ @'
                shopInfoText.text = $"{shopCrop.DisplayName.ToUpperInvariant()}  ({plot.ShopCropIndex + 1}/{plot.ShopCropCount})\n{shopCrop.SeedPackAmount} sementes: ${shopCrop.SeedPackPrice}  \u2022  Venda: ${shopCrop.HarvestItem.BaseSellPrice} cada";
                sellButton.interactable = plot.ShopInRange;
                buyButton.interactable = plot.ShopInRange;
                previousCropButton.interactable = plot.ShopCropCount > 1;
                nextCropButton.interactable = plot.ShopCropCount > 1;
'@ 'Hud.shop display'
$old = @'
            shopPanel = CreatePanel("Shop", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(430f, 205f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("ShopTitle", shopPanel.transform, "CAIXOTE DE COM\u00C9RCIO", 23, FontStyle.Bold, AccentColor, new Vector2(20f, -16f), new Vector2(390f, 30f), TextAnchor.MiddleCenter);
            shopInfoText = CreateText("ShopInfo", shopPanel.transform, "", 17, FontStyle.Normal, Color.white, new Vector2(20f, -55f), new Vector2(390f, 55f), TextAnchor.MiddleCenter);
            sellButton = CreateButton("Sell", shopPanel.transform, "VENDER TUDO", new Vector2(20f, -128f), new Vector2(185f, 52f));
            buyButton = CreateButton("Buy", shopPanel.transform, "COMPRAR SEMENTES", new Vector2(225f, -128f), new Vector2(185f, 52f));
            sellButton.onClick.AddListener(plot.RequestSell);
            buyButton.onClick.AddListener(plot.RequestBuySeeds);
'@
$new = @'
            shopPanel = CreatePanel("Shop", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(500f, 250f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("ShopTitle", shopPanel.transform, "CAIXOTE DE COM\u00C9RCIO", 23, FontStyle.Bold, AccentColor, new Vector2(20f, -16f), new Vector2(460f, 30f), TextAnchor.MiddleCenter);
            previousCropButton = CreateButton("PreviousCrop", shopPanel.transform, "<", new Vector2(20f, -67f), new Vector2(52f, 54f));
            shopInfoText = CreateText("ShopInfo", shopPanel.transform, "", 17, FontStyle.Normal, Color.white, new Vector2(80f, -56f), new Vector2(340f, 78f), TextAnchor.MiddleCenter);
            nextCropButton = CreateButton("NextCrop", shopPanel.transform, ">", new Vector2(428f, -67f), new Vector2(52f, 54f));
            sellButton = CreateButton("Sell", shopPanel.transform, "VENDER TODOS", new Vector2(20f, -174f), new Vector2(220f, 52f));
            buyButton = CreateButton("Buy", shopPanel.transform, "COMPRAR SEMENTES", new Vector2(260f, -174f), new Vector2(220f, 52f));
            previousCropButton.onClick.AddListener(() => plot.CycleShopCrop(-1));
            nextCropButton.onClick.AddListener(() => plot.CycleShopCrop(1));
            sellButton.onClick.AddListener(plot.RequestSell);
            buyButton.onClick.AddListener(plot.RequestBuySeeds);
'@
$content = Replace-Once $content $old $new 'Hud.shop layout'
Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $content 'multicrop-hud'
} else { Write-Output 'Já atualizado: FarmHudController.cs' }
