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
    if ($result -match 'timed out after 60 seconds') { Write-Output "Envio aplicado; reload interrompeu resposta: $path"; return }
    if ($LASTEXITCODE -ne 0 -or $result -notmatch 'SUCCESS: script-update-or-create completed') { throw $result }
    Write-Output "Atualizado: $path"
}

$content = Read-UnityScript '.codex\read-farm-game-state.json'
if ($content -notmatch 'TryClaimJournalReward') {
    $content = Replace-Once $content '        public int Version = 9;' '        public int Version = 10;' 'State.version10'
    $content = Replace-Once $content @'
        public int HarvestLevel = 1;
'@ @'
        public int HarvestLevel = 1;
        public FarmJournalProgress Journal = new();
'@ 'State.save journal'
    $content = Replace-Once $content @'
        [SerializeField, Range(1, MaxToolLevel)] private int harvestLevel = 1;
'@ @'
        [SerializeField, Range(1, MaxToolLevel)] private int harvestLevel = 1;
        [SerializeField] private FarmJournalProgress journal = new();
'@ 'State.journal field'
    $content = Replace-Once $content @'
        public int HarvestLevel => harvestLevel;
'@ @'
        public int HarvestLevel => harvestLevel;
        public FarmJournalProgress Journal => journal;
'@ 'State.journal prop'
    $content = Replace-Once $content @'
            collectedPickupIds.Add(pickupId);
            NotifyChanged();
'@ @'
            collectedPickupIds.Add(pickupId);
            RecordJournal(FarmJournalMetric.WorldPickups, 1, null, false);
            NotifyChanged();
'@ 'State.pickup hook'
    $content = Replace-Once $content @'
        public bool MarkMilestone(FarmMilestone milestone)
'@ @'
        public void RecordJournal(FarmJournalMetric metric, int amount = 1, string cropId = null, bool notify = true)
        {
            journal ??= new FarmJournalProgress();
            amount = Mathf.Max(0, amount);
            switch (metric)
            {
                case FarmJournalMetric.Tilled: journal.Tilled += amount; break;
                case FarmJournalMetric.Planted: journal.Planted += amount; break;
                case FarmJournalMetric.Watered: journal.Watered += amount; break;
                case FarmJournalMetric.HarvestedUnits:
                    journal.HarvestedUnits += amount;
                    journal.RecordCrop(cropId);
                    break;
                case FarmJournalMetric.SoldUnits: journal.SoldUnits += amount; break;
                case FarmJournalMetric.SeedPacksBought: journal.SeedPacksBought += amount; break;
                case FarmJournalMetric.WorldPickups: journal.WorldPickups += amount; break;
                case FarmJournalMetric.ToolUpgrades: journal.ToolUpgrades += amount; break;
            }
            if (notify) NotifyChanged();
        }

        public bool TryClaimJournalReward(string questId, out int reward)
        {
            reward = 0;
            journal ??= new FarmJournalProgress();
            var definition = FarmJournalDatabase.Get(questId);
            if (definition == null || journal.HasClaimed(questId) || !definition.IsComplete(journal)) return false;
            journal.ClaimedQuestIds ??= new List<string>();
            journal.ClaimedQuestIds.Add(definition.Id);
            reward = definition.RewardMoney;
            money += reward;
            NotifyChanged();
            return true;
        }

        public bool MarkMilestone(FarmMilestone milestone)
'@ 'State.journal methods'
    $content = Replace-Once $content @'
            SetToolLevelInternal(tool, newLevel + 1);
            newLevel = GetToolLevel(tool);
            NotifyChanged();
'@ @'
            SetToolLevelInternal(tool, newLevel + 1);
            newLevel = GetToolLevel(tool);
            RecordJournal(FarmJournalMetric.ToolUpgrades, 1, null, false);
            NotifyChanged();
'@ 'State.upgrade hook'
    $content = Replace-Once $content @'
            money += earned;
            NotifyChanged();
            return true;
        }

        public bool TryBuySeedPack
'@ @'
            money += earned;
            RecordJournal(FarmJournalMetric.SoldUnits, quantity, null, false);
            NotifyChanged();
            return true;
        }

        public bool TryBuySeedPack
'@ 'State.sell hook'
    $content = Replace-Once $content @'
            money -= cost;
            AddInternal(crop.SeedItem.Id, amount);
            NotifyChanged();
'@ @'
            money -= cost;
            AddInternal(crop.SeedItem.Id, amount);
            RecordJournal(FarmJournalMetric.SeedPacksBought, 1, null, false);
            NotifyChanged();
'@ 'State.buy hook'
    $content = Replace-Once $content '                Version = 9,' '                Version = 10,' 'State.snapshot version10'
    $content = Replace-Once $content @'
                HarvestLevel = harvestLevel,
'@ @'
                HarvestLevel = harvestLevel,
                Journal = (journal ?? new FarmJournalProgress()).Clone(),
'@ 'State.snapshot journal'
    $content = Replace-Once $content @'
            else
            {
                hoeLevel = 1;
                wateringCanLevel = 1;
                harvestLevel = 1;
            }
            NotifyChanged();
'@ @'
            else
            {
                hoeLevel = 1;
                wateringCanLevel = 1;
                harvestLevel = 1;
            }
            journal = data.Version >= 10 && data.Journal != null ? data.Journal.Clone() : new FarmJournalProgress();
            NotifyChanged();
'@ 'State.restore journal'
    Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmGameState.cs' $content 'journal-v10-state'
} else { Write-Output 'Já atualizado: FarmGameState.cs' }

$content = Read-UnityScript '.codex\read-farm-test-plot.json'
if ($content -notmatch 'Save migrado para v10') {
    $content = Replace-Once $content '            saveStatus = data.Version < 9 ? "Save migrado para v9" : "Save carregado";' '            saveStatus = data.Version < 10 ? "Save migrado para v10" : "Save carregado";' 'Plot.save v10'
    $content = Replace-Once $content @'
                state = State.Tilled;
                RefreshVisual();
                plot.NotifyTileChanged();
                plot.MarkMilestone(FarmMilestone.Tilled);
'@ @'
                state = State.Tilled;
                inventory.RecordJournal(FarmJournalMetric.Tilled);
                RefreshVisual();
                plot.NotifyTileChanged();
                plot.MarkMilestone(FarmMilestone.Tilled);
'@ 'Tile.till hook'
    $content = Replace-Once $content @'
                cropDefinition = selectedCrop;
                state = State.Seeded;
                RefreshVisual();
'@ @'
                cropDefinition = selectedCrop;
                state = State.Seeded;
                inventory.RecordJournal(FarmJournalMetric.Planted, 1, cropDefinition.Id);
                RefreshVisual();
'@ 'Tile.plant hook'
    $content = Replace-Once $content @'
            if (tool == FarmTool.WateringCan && state == State.Seeded)
            {
                state = State.Watered;
                middleAt = Time.time + (cropDefinition.GrowthSeconds * 0.5f);
'@ @'
            if (tool == FarmTool.WateringCan && state == State.Seeded)
            {
                state = State.Watered;
                inventory.RecordJournal(FarmJournalMetric.Watered);
                middleAt = Time.time + (cropDefinition.GrowthSeconds * 0.5f);
'@ 'Tile.water hook'
    $content = Replace-Once $content @'
                if (!inventory.AddItem(cropDefinition.HarvestItem.Id, cropDefinition.HarvestYield))
                    return "Invent\u00E1rio cheio. Libere espa\u00E7o antes de colher.";
                state = State.Tilled;
'@ @'
                if (!inventory.AddItem(cropDefinition.HarvestItem.Id, cropDefinition.HarvestYield))
                    return "Invent\u00E1rio cheio. Libere espa\u00E7o antes de colher.";
                inventory.RecordJournal(FarmJournalMetric.HarvestedUnits, cropDefinition.HarvestYield, cropDefinition.Id);
                state = State.Tilled;
'@ 'Tile.harvest hook'
    $content = Replace-Once $content @'
            state = State.Watered;
            middleAt = Time.time + (cropDefinition.GrowthSeconds * 0.5f);
'@ @'
            state = State.Watered;
            plot.GameState?.RecordJournal(FarmJournalMetric.Watered);
            middleAt = Time.time + (cropDefinition.GrowthSeconds * 0.5f);
'@ 'Tile.rain hook'
    Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $content 'journal-v10-plot'
} else { Write-Output 'Já atualizado: FarmTestPlot.cs' }

$content = Read-UnityScript '.codex\read-farm-hud-controller.json'
if ($content -notmatch 'CreateJournalWindow') {
    $content = Replace-Once $content @'
        private bool storageOpen;
'@ @'
        private bool storageOpen;
        private GameObject journalWindow;
        private CanvasGroup journalGroup;
        private bool journalOpen;
        private readonly Text[] journalQuestTexts = new Text[5];
        private readonly Button[] journalClaimButtons = new Button[5];
        private readonly Text[] journalClaimButtonTexts = new Text[5];
'@ 'Hud.journal fields'
    $content = Replace-Once $content @'
        public bool IsStorageOpen => storageOpen;
'@ @'
        public bool IsStorageOpen => storageOpen;
        public bool IsJournalOpen => journalOpen;
'@ 'Hud.journal prop'
    $content = Replace-Once $content @'
            RefreshStorage(state);
'@ @'
            RefreshStorage(state);
            RefreshJournal(state);
'@ 'Hud.refresh journal'
    $content = Replace-Once $content @'
            if (value && storageOpen) SetStorageOpen(false);
            inventoryOpen = value;
'@ @'
            if (value && storageOpen) SetStorageOpen(false);
            if (value && journalOpen) SetJournalOpen(false);
            inventoryOpen = value;
'@ 'Hud.inventory closes journal'
    $content = Replace-Once $content @'
            if (value && inventoryOpen) SetInventoryOpen(false);
            storageOpen = value;
'@ @'
            if (value && inventoryOpen) SetInventoryOpen(false);
            if (value && journalOpen) SetJournalOpen(false);
            storageOpen = value;
'@ 'Hud.storage closes journal'
    $content = Replace-Once $content @'
        private static void SetCanvasGroup(CanvasGroup group, bool visible)
'@ @'
        public void ToggleJournal() => SetJournalOpen(!journalOpen);

        public void SetJournalOpen(bool value)
        {
            if (value && inventoryOpen) SetInventoryOpen(false);
            if (value && storageOpen) SetStorageOpen(false);
            journalOpen = value;
            SetCanvasGroup(journalGroup, value);
            if (value) journalWindow.transform.SetAsLastSibling();
            UpdateModalState();
        }

        public void ClaimJournalQuest(int index)
        {
            if (plot == null || plot.GameState == null || index < 0 || index >= FarmJournalDatabase.Definitions.Count) return;
            var definition = FarmJournalDatabase.Definitions[index];
            if (plot.GameState.TryClaimJournalReward(definition.Id, out var reward))
                ShowToast($"Di\u00E1rio conclu\u00EDdo: +${reward}", false);
        }

        private static void SetCanvasGroup(CanvasGroup group, bool visible)
'@ 'Hud.journal methods'
    $content = Replace-Once $content '        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen;' '        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen || journalOpen;' 'Hud.modal journal'
    $content = Replace-Once $content @'
            if (keyboard.iKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)
'@ @'
            if (keyboard.jKey.wasPressedThisFrame)
            {
                ToggleJournal();
            }
            else if (keyboard.iKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)
'@ 'Hud.journal key'
    $content = Replace-Once $content @'
                if (storageOpen) SetStorageOpen(false);
            }
'@ @'
                if (storageOpen) SetStorageOpen(false);
                if (journalOpen) SetJournalOpen(false);
            }
'@ 'Hud.escape journal'
    $content = Replace-Once $content @'
        private static string DisplayItemName(string itemId)
'@ @'
        private void RefreshJournal(FarmGameState state)
        {
            var definitions = FarmJournalDatabase.Definitions;
            for (var index = 0; index < journalQuestTexts.Length; index++)
            {
                var definition = definitions[index];
                var current = Mathf.Min(definition.Current(state.Journal), definition.Target);
                var claimed = state.Journal != null && state.Journal.HasClaimed(definition.Id);
                var complete = definition.IsComplete(state.Journal);
                journalQuestTexts[index].text = $"{definition.Category}  \u2022  {definition.Title}\n{definition.Description}\n{current}/{definition.Target}  \u2022  Recompensa: ${definition.RewardMoney}";
                journalClaimButtons[index].interactable = complete && !claimed;
                journalClaimButtonTexts[index].text = claimed ? "RESGATADO" : complete ? "RESGATAR" : "EM PROGRESSO";
            }
        }

        private static string DisplayItemName(string itemId)
'@ 'Hud.refresh journal method'
    $content = Replace-Once $content @'
            if (resourcesText == null || toolText == null || promptText == null || feedbackText == null || saveText == null || tutorialText == null || clockText == null || weatherText == null || inventorySummaryText == null || shopInfoText == null || shopPanel == null || upgradeToolButton == null || upgradeToolButtonText == null || inventoryWindow == null || inventoryGroup == null || storageWindow == null || storageGroup == null) return false;
'@ @'
            if (resourcesText == null || toolText == null || promptText == null || feedbackText == null || saveText == null || tutorialText == null || clockText == null || weatherText == null || inventorySummaryText == null || shopInfoText == null || shopPanel == null || upgradeToolButton == null || upgradeToolButtonText == null || inventoryWindow == null || inventoryGroup == null || storageWindow == null || storageGroup == null || journalWindow == null || journalGroup == null) return false;
'@ 'Hud.ready journal'
    $content = Replace-Once $content @'
            saveText.text = $"I/Tab invent\u00E1rio  \u2022  F5 salvar  \u2022  F9 carregar    {plot.SaveStatus}";
'@ @'
            saveText.text = $"I/Tab invent\u00E1rio  \u2022  J di\u00E1rio  \u2022  F5 salvar  \u2022  F9 carregar    {plot.SaveStatus}";
'@ 'Hud.journal hint'
    $content = Replace-Once $content @'
            CreateStorageWindow(root.transform);
            CreatePickupToast(root.transform);
'@ @'
            CreateStorageWindow(root.transform);
            CreateJournalWindow(root.transform);
            CreatePickupToast(root.transform);
'@ 'Hud.create journal call'
    $content = Replace-Once $content @'
        private void CreateHotbar(Transform root)
'@ @'
        private void CreateJournalWindow(Transform root)
        {
            journalWindow = CreatePanel("JournalWindow", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.015f, 0.02f, 0.012f, 0.72f));
            var backdrop = journalWindow.GetComponent<RectTransform>();
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            journalGroup = journalWindow.AddComponent<CanvasGroup>();
            var panel = CreatePanel("JournalPanel", journalWindow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 760f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("JournalTitle", panel.transform, "DI\u00C1RIO DA FAZENDA", 28, FontStyle.Bold, AccentColor, new Vector2(30f, -22f), new Vector2(800f, 38f));
            CreateText("JournalSubtitle", panel.transform, "Objetivos permanentes acompanham seu estilo de jogo. Recompensas s\u00F3 s\u00E3o entregues ao resgatar.", 16, FontStyle.Normal, new Color(0.78f, 0.82f, 0.74f), new Vector2(30f, -62f), new Vector2(900f, 28f));
            var close = CreateButton("CloseJournal", panel.transform, "FECHAR", new Vector2(850f, -20f), new Vector2(120f, 44f));
            close.onClick.AddListener(() => SetJournalOpen(false));
            for (var index = 0; index < journalQuestTexts.Length; index++)
            {
                var card = CreatePanel($"Quest_{index + 1}", panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -112f - (index * 122f)), new Vector2(940f, 108f), new Vector2(0f, 1f), SlotColor);
                journalQuestTexts[index] = CreateText("QuestText", card.transform, "", 16, FontStyle.Normal, Color.white, new Vector2(18f, -10f), new Vector2(700f, 88f));
                var capturedIndex = index;
                journalClaimButtons[index] = CreateButton("Claim", card.transform, "EM PROGRESSO", new Vector2(740f, -28f), new Vector2(180f, 52f));
                journalClaimButtonTexts[index] = journalClaimButtons[index].GetComponentInChildren<Text>();
                journalClaimButtons[index].onClick.AddListener(() => ClaimJournalQuest(capturedIndex));
            }
            SetCanvasGroup(journalGroup, false);
        }

        private void CreateHotbar(Transform root)
'@ 'Hud.create journal method'
    Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $content 'journal-v10-hud'
} else { Write-Output 'Já atualizado: FarmHudController.cs' }
