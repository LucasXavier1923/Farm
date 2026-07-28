$ErrorActionPreference = 'Stop'

function Replace-Required([string]$content, [string]$old, [string]$new, [string]$label) {
    if (-not $content.Contains($old)) { throw "Trecho ausente: $label" }
    return $content.Replace($old, $new)
}

$root = (Get-Location).Path

$gamePath = Join-Path $root 'Assets\_Project\Scripts\Farming\FarmGameState.cs'
$game = [IO.File]::ReadAllText($gamePath)
$game = Replace-Required $game 'public int Version = 12;' 'public int Version = 13;' 'save version field'
$game = Replace-Required $game @'
        public int Energy = FarmGameState.MaxEnergy;
        public int PumpkinSeeds;
'@ @'
        public int Energy = FarmGameState.MaxEnergy;
        public FarmMasteryProgress Mastery = new();
        public int PumpkinSeeds;
'@ 'save mastery'
$game = Replace-Required $game @'
        [SerializeField, Range(0, MaxEnergy)] private int energy = MaxEnergy;

        public int Money => money;
'@ @'
        [SerializeField, Range(0, MaxEnergy)] private int energy = MaxEnergy;
        [SerializeField] private FarmMasteryProgress mastery = new();
        private bool lastEnergyActionWasFree;

        public int Money => money;
'@ 'mastery runtime field'
$game = Replace-Required $game @'
        public int Energy => energy;
        public float EnergyRatio => energy / (float)MaxEnergy;
        public bool IsExhausted => energy <= 0;
'@ @'
        public int Energy => energy;
        public float EnergyRatio => energy / (float)MaxEnergy;
        public bool IsExhausted => energy <= 0;
        public FarmMasteryProgress Mastery => mastery;
        public bool LastEnergyActionWasFree => lastEnergyActionWasFree;
'@ 'mastery properties'
$game = Replace-Required $game @'
            energy = Mathf.Clamp(energy, 0, MaxEnergy);
            EnsureHotbar();
'@ @'
            energy = Mathf.Clamp(energy, 0, MaxEnergy);
            mastery ??= new FarmMasteryProgress();
            EnsureHotbar();
'@ 'mastery awake'
$game = Replace-Required $game @'
        public int SpendEnergy(FarmTool tool, int changedTiles)
        {
            var requested = EnergyCostPerTile(tool) * Mathf.Max(0, changedTiles);
            if (requested <= 0) return 0;
            var previous = energy;
            energy = Mathf.Max(0, energy - requested);
            if (energy != previous) NotifyChanged();
            return previous - energy;
        }
'@ @'
        public int SpendEnergy(FarmTool tool, int changedTiles)
        {
            lastEnergyActionWasFree = false;
            var requested = EnergyCostPerTile(tool) * Mathf.Max(0, changedTiles);
            if (requested <= 0) return 0;
            var cultivationAction = tool is FarmTool.Hoe or FarmTool.Seeds or FarmTool.WateringCan;
            if (cultivationAction && GetMasteryLevel(FarmMasterySkill.Cultivation) >= 2 && mastery.FreeCultivationDay != dayNumber)
            {
                mastery.FreeCultivationDay = dayNumber;
                lastEnergyActionWasFree = true;
                NotifyChanged();
                return 0;
            }
            var previous = energy;
            energy = Mathf.Max(0, energy - requested);
            if (energy != previous) NotifyChanged();
            return previous - energy;
        }
'@ 'free cultivation energy'
$game = Replace-Required $game @'
            collectedPickupIds.Add(pickupId);
            RecordJournal(FarmJournalMetric.WorldPickups, 1, null, false);
            NotifyChanged();
'@ @'
            collectedPickupIds.Add(pickupId);
            RecordJournal(FarmJournalMetric.WorldPickups, 1, null, false);
            AddMasteryExperience(FarmMasterySkill.Harvesting, 1, false);
            NotifyChanged();
'@ 'pickup mastery'
$game = Replace-Required $game @'
            if (GetQuantity(crop.HarvestItem.Id) < order.Quantity)
            {
                error = $"Faltam {order.Quantity - GetQuantity(crop.HarvestItem.Id)} {crop.DisplayName.ToLowerInvariant()}(s).";
                return false;
            }

            RemoveFromList(inventory, crop.HarvestItem.Id, order.Quantity);
'@ @'
            var inventoryQuantity = GetQuantity(crop.HarvestItem.Id);
            var storageQuantity = GetMasteryLevel(FarmMasterySkill.Commerce) >= 3 ? GetStorageQuantity(crop.HarvestItem.Id) : 0;
            var available = inventoryQuantity + storageQuantity;
            if (available < order.Quantity)
            {
                error = $"Faltam {order.Quantity - available} {crop.DisplayName.ToLowerInvariant()}(s).";
                return false;
            }

            var fromInventory = Mathf.Min(inventoryQuantity, order.Quantity);
            RemoveFromList(inventory, crop.HarvestItem.Id, fromInventory);
            if (fromInventory < order.Quantity) RemoveFromList(storage, crop.HarvestItem.Id, order.Quantity - fromInventory);
'@ 'order storage'
$game = Replace-Required $game @'
            RecordJournal(FarmJournalMetric.SoldUnits, order.Quantity, null, false);
            NotifyChanged();
'@ @'
            RecordJournal(FarmJournalMetric.SoldUnits, order.Quantity, null, false);
            AddMasteryExperience(FarmMasterySkill.Commerce, order.Quantity, false);
            NotifyChanged();
'@ 'order mastery'
$game = Replace-Required $game @'
            money += earned;
            NotifyChanged();
            return true;
        }

        public bool TrySellAllCrops
'@ @'
            money += earned;
            AddMasteryExperience(FarmMasterySkill.Commerce, quantity, false);
            NotifyChanged();
            return true;
        }

        public bool TrySellAllCrops
'@ 'single crop sell mastery'
$game = Replace-Required $game @'
            money += earned;
            RecordJournal(FarmJournalMetric.SoldUnits, quantity, null, false);
            NotifyChanged();
'@ @'
            money += earned;
            RecordJournal(FarmJournalMetric.SoldUnits, quantity, null, false);
            AddMasteryExperience(FarmMasterySkill.Commerce, quantity, false);
            NotifyChanged();
'@ 'sell all mastery'
$game = Replace-Required $game @'
            AddInternal(crop.SeedItem.Id, amount);
            RecordJournal(FarmJournalMetric.SeedPacksBought, 1, null, false);
            NotifyChanged();
'@ @'
            AddInternal(crop.SeedItem.Id, amount);
            RecordJournal(FarmJournalMetric.SeedPacksBought, 1, null, false);
            AddMasteryExperience(FarmMasterySkill.Commerce, 2, false);
            NotifyChanged();
'@ 'buy mastery'
$game = Replace-Required $game 'Version = 12,' 'Version = 13,' 'create save version'
$game = Replace-Required $game @'
                DailyOrders = DailyOrders.Clone(),
                Energy = energy,
                PumpkinSeeds = PumpkinSeeds,
'@ @'
                DailyOrders = DailyOrders.Clone(),
                Energy = energy,
                Mastery = (mastery ?? new FarmMasteryProgress()).Clone(),
                PumpkinSeeds = PumpkinSeeds,
'@ 'create save mastery'
$game = Replace-Required $game @'
            energy = data.Version >= 12 ? Mathf.Clamp(data.Energy, 0, MaxEnergy) : MaxEnergy;
            NotifyChanged();
        }

        private void EnsureHotbar()
'@ @'
            energy = data.Version >= 12 ? Mathf.Clamp(data.Energy, 0, MaxEnergy) : MaxEnergy;
            mastery = data.Version >= 13 && data.Mastery != null ? data.Mastery.Clone() : new FarmMasteryProgress();
            lastEnergyActionWasFree = false;
            NotifyChanged();
        }

        public int GetMasteryExperience(FarmMasterySkill skill)
        {
            mastery ??= new FarmMasteryProgress();
            return mastery.GetExperience(skill);
        }

        public int GetMasteryLevel(FarmMasterySkill skill) =>
            FarmMasteryRules.LevelForExperience(GetMasteryExperience(skill));

        public bool AddMasteryExperience(FarmMasterySkill skill, int amount, bool notify = true)
        {
            if (amount <= 0) return false;
            mastery ??= new FarmMasteryProgress();
            var previousLevel = GetMasteryLevel(skill);
            mastery.AddExperience(skill, amount);
            if (notify) NotifyChanged();
            return GetMasteryLevel(skill) > previousLevel;
        }

        public bool TryAddToStorage(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 || !CanAddToStorage(itemId, amount)) return false;
            AddToStorageInternal(itemId, amount);
            NotifyChanged();
            return true;
        }

        private void EnsureHotbar()
'@ 'mastery public methods'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmGameState.mastery.cs.txt'), $game, [Text.UTF8Encoding]::new($false))

$plotPath = Join-Path $root 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs'
$plot = [IO.File]::ReadAllText($plotPath)
$plot = Replace-Required $plot @'
            settingsMenu.Initialize(hud);
            dayClock = GetComponent<FarmDayClock>();
'@ @'
            settingsMenu.Initialize(hud);
            var masteryMenu = GetComponent<FarmMasteryMenu>();
            if (masteryMenu == null) masteryMenu = gameObject.AddComponent<FarmMasteryMenu>();
            masteryMenu.Initialize(hud, gameState);
            dayClock = GetComponent<FarmDayClock>();
'@ 'mastery bootstrap'
$plot = Replace-Required $plot @'
            var wasExhausted = gameState.IsExhausted;
            gameState.SpendEnergy(activeTool, changed);
            actionFeedback?.PlayTool(activeTool, target.transform.position, changed);
'@ @'
            var masterySkill = activeTool == FarmTool.Harvest ? FarmMasterySkill.Harvesting : FarmMasterySkill.Cultivation;
            var masteryLevelUp = gameState.AddMasteryExperience(masterySkill, activeTool == FarmTool.Harvest ? changed * 2 : changed);
            var wasExhausted = gameState.IsExhausted;
            gameState.SpendEnergy(activeTool, changed);
            actionFeedback?.PlayTool(activeTool, target.transform.position, changed);
            if (gameState.LastEnergyActionWasFree)
                resultMessage += " Primeiro f\u00F4lego: esta a\u00E7\u00E3o n\u00E3o gastou energia.";
            if (masteryLevelUp)
                resultMessage += $" {FarmMasteryRules.DisplayName(masterySkill)} subiu para o n\u00EDvel {gameState.GetMasteryLevel(masterySkill)}!";
'@ 'action mastery'
$plot = Replace-Required $plot @'
                var harvestYield = cropDefinition.HarvestYieldForSeason(season);
                if (!inventory.AddItem(cropDefinition.HarvestItem.Id, harvestYield))
                    return "Invent\u00E1rio cheio. Libere espa\u00E7o antes de colher.";
                inventory.RecordJournal(FarmJournalMetric.HarvestedUnits, harvestYield, cropDefinition.Id);
                state = State.Tilled;
                RefreshVisual();
                plot.NotifyTileChanged();
                plot.MarkMilestone(FarmMilestone.Harvested);
                var seasonalBonus = harvestYield - cropDefinition.HarvestYield;
                return seasonalBonus > 0
                    ? $"Colheu {harvestYield} {cropDefinition.DisplayName.ToLowerInvariant()}(s), incluindo +{seasonalBonus} da afinidade com {FarmDayClock.SeasonName(season)}."
                    : $"Colheu {harvestYield} {cropDefinition.DisplayName.ToLowerInvariant()}(s).";
'@ @'
                var harvestYield = cropDefinition.HarvestYieldForSeason(season);
                var sentToStorage = false;
                if (!inventory.AddItem(cropDefinition.HarvestItem.Id, harvestYield))
                {
                    if (inventory.GetMasteryLevel(FarmMasterySkill.Harvesting) < 3 || !inventory.TryAddToStorage(cropDefinition.HarvestItem.Id, harvestYield))
                        return "Invent\u00E1rio cheio. Libere espa\u00E7o antes de colher.";
                    sentToStorage = true;
                }
                inventory.RecordJournal(FarmJournalMetric.HarvestedUnits, harvestYield, cropDefinition.Id);
                var replanted = inventory.GetMasteryLevel(FarmMasterySkill.Cultivation) >= 3 &&
                    cropDefinition.SeedItem != null && inventory.TryRemoveItem(cropDefinition.SeedItem.Id, 1);
                state = replanted ? State.Seeded : State.Tilled;
                if (replanted) inventory.RecordJournal(FarmJournalMetric.Planted, 1, cropDefinition.Id, false);
                RefreshVisual();
                plot.NotifyTileChanged();
                plot.MarkMilestone(FarmMilestone.Harvested);
                var seasonalBonus = harvestYield - cropDefinition.HarvestYield;
                var message = seasonalBonus > 0
                    ? $"Colheu {harvestYield} {cropDefinition.DisplayName.ToLowerInvariant()}(s), incluindo +{seasonalBonus} da afinidade com {FarmDayClock.SeasonName(season)}."
                    : $"Colheu {harvestYield} {cropDefinition.DisplayName.ToLowerInvariant()}(s).";
                if (sentToStorage) message += " Cesta de apoio: colheita enviada ao dep\u00F3sito.";
                if (replanted) message += " Ciclo cont\u00EDnuo: uma semente foi replantada.";
                return message;
'@ 'harvest perks'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmTestPlot.mastery.cs.txt'), $plot, [Text.UTF8Encoding]::new($false))

$hudPath = Join-Path $root 'Assets\_Project\Scripts\Farming\FarmHudController.cs'
$hud = [IO.File]::ReadAllText($hudPath)
$hud = Replace-Required $hud @'
        private bool dailyOrdersOpen;
        private bool settingsOpen;
        private Text dailyOrdersSummaryText;
'@ @'
        private bool dailyOrdersOpen;
        private bool settingsOpen;
        private bool masteryOpen;
        private Text dailyOrdersSummaryText;
'@ 'hud mastery field'
$hud = Replace-Required $hud @'
        public bool IsDailyOrdersOpen => dailyOrdersOpen;
        public bool IsSettingsOpen => settingsOpen;
'@ @'
        public bool IsDailyOrdersOpen => dailyOrdersOpen;
        public bool IsSettingsOpen => settingsOpen;
        public bool IsMasteryOpen => masteryOpen;
'@ 'hud mastery property'
$hud = Replace-Required $hud 'J di\u00E1rio  \u2022  F5 salvar' 'J di\u00E1rio  \u2022  K dom\u00EDnio  \u2022  F5 salvar' 'hud key hint'
$hud = Replace-Required $hud @'
        public void SetSettingsOpen(bool value)
        {
            settingsOpen = value;
            UpdateModalState();
        }

        public void CompleteDailyOrder
'@ @'
        public void SetSettingsOpen(bool value)
        {
            settingsOpen = value;
            UpdateModalState();
        }

        public void SetMasteryOpen(bool value)
        {
            masteryOpen = value;
            UpdateModalState();
        }

        public void CompleteDailyOrder
'@ 'hud mastery setter'
$hud = Replace-Required $hud 'IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen;' 'IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen || masteryOpen;' 'hud modal state'
$hud = Replace-Required $hud @'
                var crop = order.Crop;
                var have = crop != null ? state.GetQuantity(crop.HarvestItem.Id) : 0;
                var done = state.DailyOrders.IsCompleted(index);
'@ @'
                var crop = order.Crop;
                var storageConnected = state.GetMasteryLevel(FarmMasterySkill.Commerce) >= 3;
                var have = crop != null ? state.GetQuantity(crop.HarvestItem.Id) + (storageConnected ? state.GetStorageQuantity(crop.HarvestItem.Id) : 0) : 0;
                var done = state.DailyOrders.IsCompleted(index);
'@ 'hud order storage count'
$hud = Replace-Required $hud @'
            dailyOrdersSummaryText.text = $"DIA {state.DayNumber}  \u2022  {completed}/{orders.Count} entregues  \u2022  Complete os 3: +${FarmDailyOrderGenerator.BoardCompletionBonus}";
'@ @'
            dailyOrdersSummaryText.text = $"DIA {state.DayNumber}  \u2022  {completed}/{orders.Count} entregues  \u2022  Complete os 3: +${FarmDailyOrderGenerator.BoardCompletionBonus}";
            if (state.GetMasteryLevel(FarmMasterySkill.Commerce) >= 2)
            {
                var tomorrow = FarmDailyOrderGenerator.Generate(state.WorldSeed, state.DayNumber + 1);
                var preview = new StringBuilder();
                for (var index = 0; index < tomorrow.Count; index++)
                {
                    if (index > 0) preview.Append(", ");
                    preview.Append(tomorrow[index].Crop != null ? tomorrow[index].Crop.DisplayName : tomorrow[index].CropId);
                }
                dailyOrdersSummaryText.text += $"\nAmanh\u00E3: {preview}";
            }
'@ 'tomorrow preview'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmHudController.mastery.cs.txt'), $hud, [Text.UTF8Encoding]::new($false))

$pickupPath = Join-Path $root 'Assets\_Project\Scripts\Farming\FarmWorldPickup.cs'
$pickup = [IO.File]::ReadAllText($pickupPath)
$pickup = Replace-Required $pickup @'
            if (distance <= magnetDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
'@ @'
            var effectiveMagnetDistance = gameState.GetMasteryLevel(FarmMasterySkill.Harvesting) >= 2
                ? Mathf.Max(magnetDistance, FarmMasteryRules.SkilledMagnetDistance)
                : magnetDistance;
            if (distance <= effectiveMagnetDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
'@ 'mastery magnet'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmWorldPickup.mastery.cs.txt'), $pickup, [Text.UTF8Encoding]::new($false))

$settingsPath = Join-Path $root 'Assets\_Project\Scripts\Farming\FarmSettings.cs'
$settings = [IO.File]::ReadAllText($settingsPath)
$settings = Replace-Required $settings 'hud.IsDailyOrdersOpen)) SetOpen(false);' 'hud.IsDailyOrdersOpen || hud.IsMasteryOpen)) SetOpen(false);' 'settings/mastery exclusivity'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmSettings.mastery.cs.txt'), $settings, [Text.UTF8Encoding]::new($false))

Write-Output 'Mastery integration staging files created.'
