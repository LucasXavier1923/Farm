$ErrorActionPreference = 'Stop'

$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'

function Read-UnityScript([string]$assetPath) {
    $inputPath = Join-Path $project 'Temp\read-v20-current.json'
    $input = @{ filePath = $assetPath; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($inputPath, $input, [System.Text.UTF8Encoding]::new($false))
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

function Replace-Checked([string]$text, [string]$old, [string]$new, [string]$label) {
    if (-not $text.Contains($old)) { throw "Trecho ausente: $label" }
    return $text.Replace($old, $new)
}

function Submit-UnityScript([string]$assetPath, [string]$content, [string]$className) {
    $inputPath = Join-Path $project ("Temp\submit-" + $className + "-v20.json")
    $payload = @{ filePath = $assetPath; content = $content } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($inputPath, $payload, [System.Text.UTF8Encoding]::new($false))
    $output = @(& $cli run-tool script-update-or-create $project --input-file $inputPath 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Falha enviando $assetPath`n$($output -join "`n")" }
    $output | Select-Object -Last 10
}

$statePath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'
$hudPath = 'Assets/_Project/Scripts/Farming/FarmHudController.cs'
$state = Read-UnityScript $statePath
$hud = Read-UnityScript $hudPath

$state = Replace-Checked $state @'
        public bool IsPickupCollected(string pickupId)
'@ @'
        public bool SortInventory()
        {
            return SortStacks(inventory);
        }

        public bool SortStorage()
        {
            return SortStacks(storage);
        }

        private bool SortStacks(List<InventoryStack> stacks)
        {
            if (stacks == null || stacks.Count <= 1) return false;
            var previous = new List<InventoryStack>(stacks.Count);
            foreach (var stack in stacks) previous.Add(new InventoryStack(stack.ItemId, stack.Quantity));
            stacks.Sort(CompareInventoryStacks);
            var changed = false;
            for (var index = 0; index < stacks.Count; index++)
            {
                if (string.Equals(stacks[index].ItemId, previous[index].ItemId, StringComparison.OrdinalIgnoreCase) &&
                    stacks[index].Quantity == previous[index].Quantity) continue;
                changed = true;
                break;
            }
            if (changed) NotifyChanged();
            return changed;
        }

        private static int CompareInventoryStacks(InventoryStack left, InventoryStack right)
        {
            var leftDefinition = left != null ? FarmContentDatabase.GetItem(left.ItemId) : null;
            var rightDefinition = right != null ? FarmContentDatabase.GetItem(right.ItemId) : null;
            var category = InventoryCategoryOrder(leftDefinition).CompareTo(InventoryCategoryOrder(rightDefinition));
            if (category != 0) return category;
            var name = string.Compare(
                leftDefinition != null ? leftDefinition.DisplayName : left?.ItemId,
                rightDefinition != null ? rightDefinition.DisplayName : right?.ItemId,
                StringComparison.CurrentCultureIgnoreCase);
            if (name != 0) return name;
            return string.Compare(left?.ItemId, right?.ItemId, StringComparison.OrdinalIgnoreCase);
        }

        private static int InventoryCategoryOrder(ItemDefinition definition)
        {
            if (definition == null) return 5;
            if (definition.Category == ItemCategory.Seed) return 0;
            if (definition.Category == ItemCategory.Crop) return 1;
            if (definition.Category == ItemCategory.Material &&
                !definition.Id.EndsWith("_kit", StringComparison.OrdinalIgnoreCase)) return 2;
            if (definition.Category == ItemCategory.Material) return 3;
            if (definition.Category == ItemCategory.Tool) return 4;
            return 5;
        }

        public bool IsPickupCollected(string pickupId)
'@ 'ordenacao de pilhas'

$hud = Replace-Checked $hud @'
        private readonly Text[] inventoryCounts = new Text[20];
        private readonly Image[] hotbarSlots
'@ @'
        private readonly Text[] inventoryCounts = new Text[20];
        private readonly Button[] inventoryFilterButtons = new Button[5];
        private readonly Image[] inventoryFilterBackgrounds = new Image[5];
        private FarmCollectionCategory inventoryFilter = FarmCollectionCategory.All;
        private int inventoryVisibleItemCount;
        private readonly Image[] hotbarSlots
'@ 'estado de filtros'
$hud = Replace-Checked $hud @'
        public string TooltipText => itemTooltipTitle == null || itemTooltipBody == null
            ? string.Empty
            : itemTooltipTitle.text + "\n" + itemTooltipBody.text;
'@ @'
        public string TooltipText => itemTooltipTitle == null || itemTooltipBody == null
            ? string.Empty
            : itemTooltipTitle.text + "\n" + itemTooltipBody.text;
        public FarmCollectionCategory InventoryFilter => inventoryFilter;
        public int InventoryVisibleItemCount => inventoryVisibleItemCount;
'@ 'propriedades de filtro'
$hud = Replace-Checked $hud @'
        public void ToggleJournal() => SetJournalOpen(!journalOpen);
'@ @'
        public void SetInventoryFilter(FarmCollectionCategory category)
        {
            inventoryFilter = category;
            HideItemTooltip();
            EndItemDrag();
            if (plot != null && plot.GameState != null) RefreshInventory(plot.GameState);
        }

        public bool OrganizeInventory()
        {
            if (plot == null || plot.GameState == null) return false;
            var changed = plot.GameState.SortInventory();
            ShowSystemToast(changed ? "Mochila organizada." : "A mochila j\u00E1 est\u00E1 organizada.", false);
            return changed;
        }

        public bool OrganizeStorage()
        {
            if (plot == null || plot.GameState == null) return false;
            var changed = plot.GameState.SortStorage();
            if (storageFeedbackText != null)
                storageFeedbackText.text = changed ? "Dep\u00F3sito organizado." : "O dep\u00F3sito j\u00E1 est\u00E1 organizado.";
            return changed;
        }

        public void ToggleJournal() => SetJournalOpen(!journalOpen);
'@ 'acoes de organizacao'
$hud = Replace-Checked $hud @'
        private void RefreshInventory(FarmGameState state)
        {
            inventoryCapacityText.text = $"MOCHILA  {state.UsedSlots}/{state.SlotCapacity} slots";
            for (var index = 0; index < inventorySlots.Length; index++)
            {
                var occupied = index < state.Inventory.Count;
                var stack = occupied ? state.Inventory[index] : null;
'@ @'
        private void RefreshInventory(FarmGameState state)
        {
            var filtered = new System.Collections.Generic.List<InventoryStack>();
            foreach (var candidate in state.Inventory)
            {
                var definition = candidate != null ? FarmContentDatabase.GetItem(candidate.ItemId) : null;
                if (MatchesInventoryFilter(definition, inventoryFilter)) filtered.Add(candidate);
            }
            inventoryVisibleItemCount = filtered.Count;
            var filterSuffix = inventoryFilter == FarmCollectionCategory.All
                ? string.Empty
                : $"  \u2022  EXIBINDO {filtered.Count}";
            inventoryCapacityText.text = $"MOCHILA  {state.UsedSlots}/{state.SlotCapacity} slots{filterSuffix}";
            for (var index = 0; index < inventorySlots.Length; index++)
            {
                var occupied = index < filtered.Count;
                var stack = occupied ? filtered[index] : null;
'@ 'refresh filtrado'
$hud = Replace-Checked $hud @'
                inventorySlots[index].GetComponent<FarmInventorySlotView>().Initialize(this, occupied ? stack.ItemId : null);
            }
        }

        private void RefreshHotbar
'@ @'
                inventorySlots[index].GetComponent<FarmInventorySlotView>().Initialize(this, occupied ? stack.ItemId : null);
            }
            for (var index = 0; index < inventoryFilterBackgrounds.Length; index++)
                if (inventoryFilterBackgrounds[index] != null)
                    inventoryFilterBackgrounds[index].color = index == (int)inventoryFilter
                        ? new Color(0.42f, 0.56f, 0.20f, 1f)
                        : new Color(0.20f, 0.28f, 0.13f, 1f);
        }

        private static bool MatchesInventoryFilter(ItemDefinition definition, FarmCollectionCategory filter)
        {
            if (filter == FarmCollectionCategory.All) return true;
            if (definition == null) return false;
            if (filter == FarmCollectionCategory.Seeds) return definition.Category == ItemCategory.Seed;
            if (filter == FarmCollectionCategory.Crops) return definition.Category == ItemCategory.Crop;
            if (filter == FarmCollectionCategory.Projects) return FarmCollectionDatabase.IsProject(definition);
            if (filter == FarmCollectionCategory.Materials)
                return definition.Category == ItemCategory.Material && !FarmCollectionDatabase.IsProject(definition);
            return true;
        }

        private void RefreshHotbar
'@ 'comparador de filtro'
$hud = Replace-Checked $hud @'
            var window = CreatePanel("Backpack", inventoryWindow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 45f), new Vector2(790f, 560f), new Vector2(0.5f, 0.5f), PanelColor);
'@ @'
            var window = CreatePanel("Backpack", inventoryWindow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(790f, 650f), new Vector2(0.5f, 0.5f), PanelColor);
'@ 'altura da mochila'
$hud = Replace-Checked $hud @'
            CreateText("Hint", window.transform, "Arraste um item para qualquer espa\u00E7o da barra. Clique direito na barra para limpar.", 15, FontStyle.Normal, new Color(0.78f, 0.84f, 0.72f), new Vector2(28f, -92f), new Vector2(690f, 26f));
            var close = CreateButton("Close", window.transform, "FECHAR  [I]", new Vector2(620f, -24f), new Vector2(140f, 44f));
            close.onClick.AddListener(() => SetInventoryOpen(false));

            for (var index = 0; index < inventorySlots.Length; index++)
'@ @'
            CreateText("Hint", window.transform, "Arraste para a barra \u2022 passe o mouse para detalhes \u2022 filtros n\u00E3o removem itens", 14, FontStyle.Normal, new Color(0.78f, 0.84f, 0.72f), new Vector2(28f, -92f), new Vector2(570f, 26f));
            var close = CreateButton("Close", window.transform, "FECHAR  [I]", new Vector2(620f, -24f), new Vector2(140f, 44f));
            close.onClick.AddListener(() => SetInventoryOpen(false));

            var filterLabels = new[] { "TODOS", "SEMENTES", "COLHEITAS", "MATERIAIS", "PROJETOS" };
            for (var index = 0; index < filterLabels.Length; index++)
            {
                var captured = (FarmCollectionCategory)index;
                inventoryFilterButtons[index] = CreateButton($"InventoryFilter_{filterLabels[index]}", window.transform, filterLabels[index], new Vector2(28f + index * 112f, -127f), new Vector2(106f, 34f));
                inventoryFilterBackgrounds[index] = inventoryFilterButtons[index].GetComponent<Image>();
                inventoryFilterButtons[index].onClick.AddListener(() => SetInventoryFilter(captured));
            }
            var organize = CreateButton("OrganizeInventory", window.transform, "ORGANIZAR", new Vector2(620f, -127f), new Vector2(140f, 34f));
            organize.onClick.AddListener(() => OrganizeInventory());

            for (var index = 0; index < inventorySlots.Length; index++)
'@ 'controles de filtro'
$hud = Replace-Checked $hud 'new Vector2(28f + column * 150f, -135f - row * 100f)' 'new Vector2(28f + column * 150f, -182f - row * 100f)' 'reposicionamento dos slots'
$hud = Replace-Checked $hud @'
            storageBackpackCapacityText = CreateText("BackpackCapacity", window.transform, "", 18, FontStyle.Bold, Color.white, new Vector2(28f, -100f), new Vector2(560f, 28f));
            storageChestCapacityText = CreateText("ChestCapacity", window.transform, "", 18, FontStyle.Bold, Color.white, new Vector2(690f, -100f), new Vector2(700f, 28f));
            CreateText("Direction"
'@ @'
            storageBackpackCapacityText = CreateText("BackpackCapacity", window.transform, "", 18, FontStyle.Bold, Color.white, new Vector2(28f, -100f), new Vector2(390f, 28f));
            var organizeBackpack = CreateButton("OrganizeBackpack", window.transform, "ORGANIZAR", new Vector2(445f, -94f), new Vector2(145f, 34f));
            organizeBackpack.onClick.AddListener(() => OrganizeInventory());
            storageChestCapacityText = CreateText("ChestCapacity", window.transform, "", 18, FontStyle.Bold, Color.white, new Vector2(690f, -100f), new Vector2(400f, 28f));
            var organizeChest = CreateButton("OrganizeChest", window.transform, "ORGANIZAR", new Vector2(1275f, -94f), new Vector2(145f, 34f));
            organizeChest.onClick.AddListener(() => OrganizeStorage());
            CreateText("Direction"
'@ 'botoes do deposito'

Submit-UnityScript $statePath $state 'FarmGameState'
Submit-UnityScript $hudPath $hud 'FarmHudController'
Write-Output 'INVENTORY_ORGANIZATION_V20_SUBMITTED'
