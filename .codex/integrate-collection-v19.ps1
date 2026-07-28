$ErrorActionPreference = 'Stop'

$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'

function Read-UnityScript([string]$assetPath) {
    $inputPath = Join-Path $project 'Temp\read-v19-current.json'
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
    $inputPath = Join-Path $project ("Temp\submit-" + $className + "-v19.json")
    $payload = @{ filePath = $assetPath; content = $content } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($inputPath, $payload, [System.Text.UTF8Encoding]::new($false))
    $output = @(& $cli run-tool script-update-or-create $project --input-file $inputPath 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Falha enviando $assetPath`n$($output -join "`n")" }
    $output | Select-Object -Last 10
}

$statePath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'
$hudPath = 'Assets/_Project/Scripts/Farming/FarmHudController.cs'
$plotPath = 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs'

$state = Read-UnityScript $statePath
$hud = Read-UnityScript $hudPath
$plot = Read-UnityScript $plotPath

$state = Replace-Checked $state '        public int Version = 16;' '        public int Version = 17;' 'versao padrao'
$state = Replace-Checked $state @'
        public List<string> ClaimedMailIds = new();
        public int PumpkinSeeds;
'@ @'
        public List<string> ClaimedMailIds = new();
        public List<string> DiscoveredItemIds = new();
        public int PumpkinSeeds;
'@ 'campo de save das descobertas'
$state = Replace-Checked $state @'
        [SerializeField] private List<string> claimedMailIds = new();
        private bool lastEnergyActionWasFree;
'@ @'
        [SerializeField] private List<string> claimedMailIds = new();
        [SerializeField] private List<string> discoveredItemIds = new();
        private bool lastEnergyActionWasFree;
'@ 'campo runtime das descobertas'
$state = Replace-Checked $state @'
        public IReadOnlyList<string> ClaimedMailIds => claimedMailIds;
        public bool LastEnergyActionWasFree => lastEnergyActionWasFree;
'@ @'
        public IReadOnlyList<string> ClaimedMailIds => claimedMailIds;
        public IReadOnlyList<string> DiscoveredItemIds => discoveredItemIds;
        public bool LastEnergyActionWasFree => lastEnergyActionWasFree;
'@ 'propriedade das descobertas'
$state = Replace-Checked $state @'
            claimedMailIds ??= new List<string>();
            EnsureHotbar();
'@ @'
            claimedMailIds ??= new List<string>();
            discoveredItemIds ??= new List<string>();
            EnsureHotbar();
'@ 'inicializacao das descobertas'
$state = Replace-Checked $state @'
        public static int EnergyCostPerTile(FarmTool tool) => tool switch
'@ @'
        public bool IsItemDiscovered(string itemId)
        {
            if (discoveredItemIds == null || string.IsNullOrWhiteSpace(itemId)) return false;
            foreach (var id in discoveredItemIds)
                if (string.Equals(id, itemId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public bool RecordDiscoveredItem(string itemId, bool notify = true)
        {
            if (string.IsNullOrWhiteSpace(itemId) || FarmContentDatabase.GetItem(itemId) == null || IsItemDiscovered(itemId)) return false;
            discoveredItemIds ??= new List<string>();
            discoveredItemIds.Add(itemId);
            if (notify) NotifyChanged();
            return true;
        }

        public int CountDiscoveredItems()
        {
            var count = 0;
            foreach (var definition in FarmContentDatabase.Items)
                if (definition != null && IsItemDiscovered(definition.Id)) count++;
            return count;
        }

        public static int EnergyCostPerTile(FarmTool tool) => tool switch
'@ 'api de descobertas'
$state = Replace-Checked $state '                Version = 16,' '                Version = 17,' 'versao criada'
$state = Replace-Checked $state @'
                ClaimedMailIds = new List<string>(claimedMailIds ?? new List<string>()),
                PumpkinSeeds = PumpkinSeeds,
'@ @'
                ClaimedMailIds = new List<string>(claimedMailIds ?? new List<string>()),
                DiscoveredItemIds = new List<string>(discoveredItemIds ?? new List<string>()),
                PumpkinSeeds = PumpkinSeeds,
'@ 'save das descobertas'
$state = Replace-Checked $state @'
            if (data == null) return;
            money = Mathf.Max(0, data.Money);
'@ @'
            if (data == null) return;
            discoveredItemIds = new List<string>();
            money = Mathf.Max(0, data.Money);
'@ 'reset antes do restore'
$state = Replace-Checked $state @'
            lastEnergyActionWasFree = false;
            NotifyChanged();
'@ @'
            if (data.Version >= 17)
            {
                AddUniqueDiscoveredItemIds(data.DiscoveredItemIds);
            }
            else if (journal != null && journal.HarvestedCropIds != null)
            {
                foreach (var cropId in journal.HarvestedCropIds)
                {
                    var crop = FarmContentDatabase.GetCrop(cropId);
                    if (crop == null) continue;
                    if (crop.SeedItem != null) RecordDiscoveredItem(crop.SeedItem.Id, false);
                    if (crop.HarvestItem != null) RecordDiscoveredItem(crop.HarvestItem.Id, false);
                }
            }
            lastEnergyActionWasFree = false;
            NotifyChanged();
'@ 'restore e migracao das descobertas'
$state = Replace-Checked $state @'
        private static bool ContainsMailId(List<string> source, string mailId)
'@ @'
        private void AddUniqueDiscoveredItemIds(List<string> source)
        {
            if (source == null) return;
            foreach (var id in source) RecordDiscoveredItem(id, false);
        }

        private static bool ContainsMailId(List<string> source, string mailId)
'@ 'uniao de descobertas'
$state = Replace-Checked $state @'
        private void AddInternal(string itemId, int amount)
        {
            var item = FarmContentDatabase.GetItem(itemId);
'@ @'
        private void AddInternal(string itemId, int amount)
        {
            var item = FarmContentDatabase.GetItem(itemId);
            if (item != null && amount > 0) RecordDiscoveredItem(itemId, false);
'@ 'descoberta na mochila'
$state = Replace-Checked $state @'
        private void AddToStorageInternal(string itemId, int amount)
        {
            var item = FarmContentDatabase.GetItem(itemId);
'@ @'
        private void AddToStorageInternal(string itemId, int amount)
        {
            var item = FarmContentDatabase.GetItem(itemId);
            if (item != null && amount > 0) RecordDiscoveredItem(itemId, false);
'@ 'descoberta no deposito'

$hud = Replace-Checked $hud @'
        private bool mailboxOpen;
        private Text dailyOrdersSummaryText;
'@ @'
        private bool mailboxOpen;
        private bool collectionOpen;
        private Text dailyOrdersSummaryText;
'@ 'estado modal de colecao'
$hud = Replace-Checked $hud @'
        public bool IsMailboxOpen => mailboxOpen;
        public bool IsShopOpen => shopOpen;
'@ @'
        public bool IsMailboxOpen => mailboxOpen;
        public bool IsCollectionOpen => collectionOpen;
        public bool IsShopOpen => shopOpen;
'@ 'propriedade modal de colecao'
$hud = Replace-Checked $hud @'
        public void SetMailboxOpen(bool value)
        {
            mailboxOpen = value;
            UpdateModalState();
        }

        public void SetShopOpen
'@ @'
        public void SetMailboxOpen(bool value)
        {
            mailboxOpen = value;
            UpdateModalState();
        }

        public void SetCollectionOpen(bool value)
        {
            collectionOpen = value;
            UpdateModalState();
        }

        public void SetShopOpen
'@ 'setter modal de colecao'
$hud = Replace-Checked $hud 'buildingCatalogOpen || mailboxOpen || shopOpen;' 'buildingCatalogOpen || mailboxOpen || collectionOpen || shopOpen;' 'composicao modal'
$hud = Replace-Checked $hud 'if (buildingCatalogOpen || mailboxOpen) return;' 'if (buildingCatalogOpen || mailboxOpen || collectionOpen) return;' 'bloqueio de atalhos'

$plot = Replace-Checked $plot @'
        private FarmMailboxSystem mailboxSystem;
        private FarmTestTile hoveredTile;
'@ @'
        private FarmMailboxSystem mailboxSystem;
        private FarmCollectionBook collectionBook;
        private FarmTestTile hoveredTile;
'@ 'campo do livro'
$plot = Replace-Checked $plot @'
        public FarmMailboxSystem MailboxSystem => mailboxSystem;
        public CropDefinition ActiveCrop => activeCrop;
'@ @'
        public FarmMailboxSystem MailboxSystem => mailboxSystem;
        public FarmCollectionBook CollectionBook => collectionBook;
        public CropDefinition ActiveCrop => activeCrop;
'@ 'propriedade do livro'
$plot = Replace-Checked $plot @'
            hud.Initialize(this);
            var settingsMenu = GetComponent<FarmSettingsMenu>();
'@ @'
            hud.Initialize(this);
            collectionBook = GetComponent<FarmCollectionBook>();
            if (collectionBook == null) collectionBook = gameObject.AddComponent<FarmCollectionBook>();
            collectionBook.Initialize(gameState, hud);
            var settingsMenu = GetComponent<FarmSettingsMenu>();
'@ 'inicializacao do livro'

Submit-UnityScript $hudPath $hud 'FarmHudController'
Submit-UnityScript $statePath $state 'FarmGameState'
Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmCollectionBook.cs' ([System.IO.File]::ReadAllText((Join-Path $project '.codex\FarmCollectionBook.v19.cs.txt'))) 'FarmCollectionBook'
Submit-UnityScript $plotPath $plot 'FarmTestPlot'

Write-Output 'COLLECTION_V19_SUBMITTED'
