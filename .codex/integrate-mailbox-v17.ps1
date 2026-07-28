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
    $result | Select-Object -Last 16
    Start-Sleep -Seconds 2
}

$statePath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmGameState.cs'
$state = [IO.File]::ReadAllText($statePath)
$state = Replace-Checked $state '        public int Version = 15;' '        public int Version = 16;' 'versao do save'
$state = Replace-Checked $state @'
        public int LastMorningAutomationDay;
        public int PumpkinSeeds;
'@ @'
        public int LastMorningAutomationDay;
        public List<string> ReadMailIds = new();
        public List<string> ClaimedMailIds = new();
        public int PumpkinSeeds;
'@ 'campos salvos de cartas'
$state = Replace-Checked $state @'
        [SerializeField] private int lastMorningAutomationDay;
        private bool lastEnergyActionWasFree;
'@ @'
        [SerializeField] private int lastMorningAutomationDay;
        [SerializeField] private List<string> readMailIds = new();
        [SerializeField] private List<string> claimedMailIds = new();
        private bool lastEnergyActionWasFree;
'@ 'estado de cartas'
$state = Replace-Checked $state @'
        public int LastMorningAutomationDay => lastMorningAutomationDay;
        public bool LastEnergyActionWasFree => lastEnergyActionWasFree;
'@ @'
        public int LastMorningAutomationDay => lastMorningAutomationDay;
        public IReadOnlyList<string> ReadMailIds => readMailIds;
        public IReadOnlyList<string> ClaimedMailIds => claimedMailIds;
        public bool LastEnergyActionWasFree => lastEnergyActionWasFree;
'@ 'propriedades de cartas'
$state = Replace-Checked $state @'
            mastery ??= new FarmMasteryProgress();
            EnsureHotbar();
'@ @'
            mastery ??= new FarmMasteryProgress();
            readMailIds ??= new List<string>();
            claimedMailIds ??= new List<string>();
            EnsureHotbar();
'@ 'inicializacao de cartas'
$state = Replace-Checked $state '                Version = 15,' '                Version = 16,' 'versao criada'
$state = Replace-Checked $state @'
                LastMorningAutomationDay = lastMorningAutomationDay,
                PumpkinSeeds = PumpkinSeeds,
'@ @'
                LastMorningAutomationDay = lastMorningAutomationDay,
                ReadMailIds = new List<string>(readMailIds ?? new List<string>()),
                ClaimedMailIds = new List<string>(claimedMailIds ?? new List<string>()),
                PumpkinSeeds = PumpkinSeeds,
'@ 'serializacao de cartas'
$state = Replace-Checked $state @'
            lastMorningAutomationDay = data.Version >= 15 ? Mathf.Max(0, data.LastMorningAutomationDay) : 0;
            lastEnergyActionWasFree = false;
'@ @'
            lastMorningAutomationDay = data.Version >= 15 ? Mathf.Max(0, data.LastMorningAutomationDay) : 0;
            readMailIds = new List<string>();
            claimedMailIds = new List<string>();
            if (data.Version >= 16)
            {
                AddUniqueMailIds(readMailIds, data.ReadMailIds);
                AddUniqueMailIds(claimedMailIds, data.ClaimedMailIds);
            }
            else
            {
                foreach (var mail in FarmMailDatabase.GetInbox(dayNumber))
                    if (mail != null && !string.IsNullOrWhiteSpace(mail.Id)) readMailIds.Add(mail.Id);
            }
            lastEnergyActionWasFree = false;
'@ 'restauracao e migracao de cartas'
$state = Replace-Checked $state @'
        public bool TryBeginMorningAutomation(int day)
'@ @'
        public bool IsMailRead(string mailId) => ContainsMailId(readMailIds, mailId);

        public bool IsMailClaimed(string mailId) => ContainsMailId(claimedMailIds, mailId);

        public bool MarkMailRead(string mailId)
        {
            if (string.IsNullOrWhiteSpace(mailId) || IsMailRead(mailId)) return false;
            if (FarmMailDatabase.Get(mailId, dayNumber) == null) return false;
            readMailIds ??= new List<string>();
            readMailIds.Add(mailId);
            NotifyChanged();
            return true;
        }

        public int CountUnreadMail()
        {
            var count = 0;
            foreach (var mail in FarmMailDatabase.GetInbox(dayNumber))
                if (mail != null && !IsMailRead(mail.Id)) count++;
            return count;
        }

        public int CountClaimableMail()
        {
            var count = 0;
            foreach (var mail in FarmMailDatabase.GetInbox(dayNumber))
                if (mail != null && mail.HasReward && !IsMailClaimed(mail.Id)) count++;
            return count;
        }

        public bool TryClaimMail(FarmMailDefinition mail, out string error)
        {
            error = string.Empty;
            if (mail == null || mail.DeliveredDay > dayNumber ||
                FarmMailDatabase.Get(mail.Id, dayNumber) == null)
            {
                error = "Carta indispon\u00EDvel.";
                return false;
            }
            if (IsMailClaimed(mail.Id))
            {
                error = "Este anexo j\u00E1 foi resgatado.";
                return false;
            }
            if (!mail.HasReward)
            {
                error = "Esta carta n\u00E3o possui anexo.";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(mail.RewardItemId) && mail.RewardQuantity > 0)
            {
                if (FarmContentDatabase.GetItem(mail.RewardItemId) == null)
                {
                    error = "O item anexado n\u00E3o existe no cat\u00E1logo.";
                    return false;
                }
                if (!CanAdd(mail.RewardItemId, mail.RewardQuantity))
                {
                    error = "Mochila cheia. Libere espa\u00E7o antes de resgatar.";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(mail.RewardItemId) && mail.RewardQuantity > 0)
                AddInternal(mail.RewardItemId, mail.RewardQuantity);
            money += Mathf.Max(0, mail.RewardMoney);
            readMailIds ??= new List<string>();
            claimedMailIds ??= new List<string>();
            if (!IsMailRead(mail.Id)) readMailIds.Add(mail.Id);
            claimedMailIds.Add(mail.Id);
            NotifyChanged();
            return true;
        }

        private static bool ContainsMailId(List<string> source, string mailId)
        {
            if (source == null || string.IsNullOrWhiteSpace(mailId)) return false;
            foreach (var id in source)
                if (string.Equals(id, mailId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void AddUniqueMailIds(List<string> target, List<string> source)
        {
            if (target == null || source == null) return;
            foreach (var id in source)
                if (!string.IsNullOrWhiteSpace(id) && !ContainsMailId(target, id)) target.Add(id);
        }

        public bool TryBeginMorningAutomation(int day)
'@ 'regras de cartas'
Submit-Script 'Assets/_Project/Scripts/Farming/FarmGameState.cs' $state 'mailbox-state-v17'

$hudPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmHudController.cs'
$hud = [IO.File]::ReadAllText($hudPath)
$hud = Replace-Checked $hud @'
        private bool buildingCatalogOpen;
        private Text dailyOrdersSummaryText;
'@ @'
        private bool buildingCatalogOpen;
        private bool mailboxOpen;
        private Text dailyOrdersSummaryText;
'@ 'estado modal da caixa postal'
$hud = Replace-Checked $hud @'
        public bool IsBuildingCatalogOpen => buildingCatalogOpen;
        public bool IsShopOpen => shopOpen;
'@ @'
        public bool IsBuildingCatalogOpen => buildingCatalogOpen;
        public bool IsMailboxOpen => mailboxOpen;
        public bool IsShopOpen => shopOpen;
'@ 'propriedade da caixa postal'
$hud = Replace-Checked $hud @'
        public void SetBuildingCatalogOpen(bool value)
        {
            buildingCatalogOpen = value;
            UpdateModalState();
        }
'@ @'
        public void SetBuildingCatalogOpen(bool value)
        {
            buildingCatalogOpen = value;
            UpdateModalState();
        }

        public void SetMailboxOpen(bool value)
        {
            mailboxOpen = value;
            UpdateModalState();
        }
'@ 'setter da caixa postal'
$hud = Replace-Checked $hud @'
        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen || masteryOpen || craftingOpen || buildingCatalogOpen || shopOpen;
'@ @'
        private void UpdateModalState() => IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen || masteryOpen || craftingOpen || buildingCatalogOpen || mailboxOpen || shopOpen;
'@ 'agregacao modal da caixa postal'
$hud = Replace-Checked $hud '            if (buildingCatalogOpen) return;' '            if (buildingCatalogOpen || mailboxOpen) return;' 'bloqueio de atalhos da mochila'
Submit-Script 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $hud 'mailbox-hud-v17'

$plotPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs'
$plot = [IO.File]::ReadAllText($plotPath)
$plot = Replace-Checked $plot @'
        private FarmBuildingSystem buildingSystem;
        private FarmTestTile hoveredTile;
'@ @'
        private FarmBuildingSystem buildingSystem;
        private FarmMailboxSystem mailboxSystem;
        private FarmTestTile hoveredTile;
'@ 'campo da caixa postal'
$plot = Replace-Checked $plot @'
        public FarmBuildingSystem BuildingSystem => buildingSystem;
        public CropDefinition ActiveCrop => activeCrop;
'@ @'
        public FarmBuildingSystem BuildingSystem => buildingSystem;
        public FarmMailboxSystem MailboxSystem => mailboxSystem;
        public CropDefinition ActiveCrop => activeCrop;
'@ 'propriedade da caixa postal'
$plot = Replace-Checked $plot @'
        public bool OrderBoardVisible => orderBoardStation != null && IsOrderBoardInRange();
        public IReadOnlyList<FarmDailyOrder> DailyOrders
'@ @'
        public bool OrderBoardVisible => orderBoardStation != null && IsOrderBoardInRange();
        public bool MailboxVisible => mailboxSystem != null && mailboxSystem.IsInRange;
        public IReadOnlyList<FarmDailyOrder> DailyOrders
'@ 'alcance da caixa postal'
$plot = Replace-Checked $plot @'
            dayClock.Initialize(this, gameState);
            weatherSystem = GetComponent<FarmWeatherSystem>();
'@ @'
            dayClock.Initialize(this, gameState);
            mailboxSystem = GetComponent<FarmMailboxSystem>();
            if (mailboxSystem == null) mailboxSystem = gameObject.AddComponent<FarmMailboxSystem>();
            var mailboxRight = Vector3.Cross(Vector3.up, plotForward).normalized;
            var mailboxPosition = PlaceOnGround(
                plotCenter - (plotForward * (plotGridOffset + 4.3f)) - (mailboxRight * 1.8f));
            mailboxSystem.Initialize(this, gameState, hud, player, mailboxPosition, plotForward);
            weatherSystem = GetComponent<FarmWeatherSystem>();
'@ 'inicializacao da caixa postal'
$plot = Replace-Checked $plot @'
                    if (SleepVisible) RequestSleep();
                    else if (OrderBoardVisible) RequestOrderBoard();
'@ @'
                    if (MailboxVisible) RequestMailbox();
                    else if (SleepVisible) RequestSleep();
                    else if (OrderBoardVisible) RequestOrderBoard();
'@ 'interacao F da caixa postal'
$plot = Replace-Checked $plot @'
        public void RequestShop()
'@ @'
        public void RequestMailbox()
        {
            if (mailboxSystem == null || !mailboxSystem.IsInRange)
            {
                feedback = "Chegue mais perto da caixa postal.";
                return;
            }
            if (mailboxSystem.Open()) feedback = "Caixa postal aberta.";
        }

        public void RequestShop()
'@ 'pedido de abertura da caixa postal'
$plot = Replace-Checked $plot '            saveStatus = data.Version < 14 ? "Save migrado para v14" : "Save carregado";' '            saveStatus = data.Version < 16 ? "Save migrado para v16" : "Save carregado";' 'status de migracao'
$plot = Replace-Checked $plot @'
            if (OrderBoardVisible) return "Quadro ao alcance: pressione F para abrir os pedidos.";
            if (SleepVisible) return "Cama ao alcance: pressione F para descansar.";
'@ @'
            if (MailboxVisible)
            {
                var unread = mailboxSystem != null ? mailboxSystem.UnreadCount : 0;
                return unread > 0
                    ? $"Caixa postal ao alcance: pressione F. {unread} carta{(unread == 1 ? string.Empty : "s")} nova{(unread == 1 ? string.Empty : "s")}."
                    : "Caixa postal ao alcance: pressione F para ver a agenda.";
            }
            if (OrderBoardVisible) return "Quadro ao alcance: pressione F para abrir os pedidos.";
            if (SleepVisible) return "Cama ao alcance: pressione F para descansar.";
'@ 'prompt da caixa postal'
Submit-Script 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $plot 'mailbox-plot-v17'

$mailboxContent = [IO.File]::ReadAllText((Join-Path $project '.codex\FarmMailboxSystem.v17.cs.txt'))
Submit-Script 'Assets/_Project/Scripts/Farming/FarmMailboxSystem.cs' $mailboxContent 'mailbox-system-v17'
