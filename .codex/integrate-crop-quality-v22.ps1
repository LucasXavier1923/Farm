$ErrorActionPreference = 'Stop'

$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'

function Read-UnityScript([string]$assetPath, [string]$tag) {
    $inputPath = Join-Path $project ("Temp\read-$tag-v22.json")
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
    $inputPath = Join-Path $project ("Temp\submit-$tag-v22.json")
    [System.IO.File]::WriteAllText($inputPath, (@{ filePath = $assetPath; content = $content } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
    $output = @(& $cli run-tool script-update-or-create $project --input-file $inputPath 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Falha enviando $assetPath`n$($output -join "`n")" }
    $output | Select-Object -Last 12
}

function Replace-Section([string]$text, [string]$startMarker, [string]$endMarker, [string]$replacement, [string]$label) {
    $start = $text.IndexOf($startMarker, [System.StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Inicio ausente: $label" }
    $end = $text.IndexOf($endMarker, $start + $startMarker.Length, [System.StringComparison]::Ordinal)
    if ($end -lt 0) { throw "Fim ausente: $label" }
    return $text.Substring(0, $start) + $replacement + $text.Substring($end)
}

function Replace-Checked([string]$text, [string]$old, [string]$new, [string]$label) {
    if (-not $text.Contains($old)) { throw "Trecho ausente: $label" }
    return $text.Replace($old, $new)
}

$qualityPath = 'Assets/_Project/Scripts/Farming/FarmItemQuality.cs'
$quality = @'
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmItemQuality
    {
        Normal = 0,
        Silver = 1,
        Gold = 2
    }

    public static class FarmItemQualityRules
    {
        public static FarmItemQuality Clamp(FarmItemQuality quality) =>
            quality is >= FarmItemQuality.Normal and <= FarmItemQuality.Gold
                ? quality
                : FarmItemQuality.Normal;

        public static FarmItemQuality EvaluateHarvest(CropDefinition crop, FarmSeason season, int harvestingLevel)
        {
            var score = crop != null && crop.IsPreferredSeason(season) ? 1 : 0;
            score += harvestingLevel >= 3 ? 2 : harvestingLevel >= 2 ? 1 : 0;
            return score >= 3 ? FarmItemQuality.Gold
                : score >= 1 ? FarmItemQuality.Silver
                : FarmItemQuality.Normal;
        }

        public static float SellMultiplier(FarmItemQuality quality) => Clamp(quality) switch
        {
            FarmItemQuality.Silver => 1.25f,
            FarmItemQuality.Gold => 1.60f,
            _ => 1f
        };

        public static int UnitSellPrice(ItemDefinition definition, FarmItemQuality quality) =>
            definition == null ? 0 : Mathf.CeilToInt(Mathf.Max(0, definition.BaseSellPrice) * SellMultiplier(quality));

        public static string DisplayName(FarmItemQuality quality) => Clamp(quality) switch
        {
            FarmItemQuality.Silver => "Prata",
            FarmItemQuality.Gold => "Ouro",
            _ => "Normal"
        };

        public static string ShortMark(FarmItemQuality quality) => Clamp(quality) switch
        {
            FarmItemQuality.Silver => "\u25C6",
            FarmItemQuality.Gold => "\u2605",
            _ => string.Empty
        };
    }
}
'@
Submit-UnityScript $qualityPath $quality 'FarmItemQuality'

$statePath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'
$state = Read-UnityScript $statePath 'quality-state'

$inventoryStack = @'
    [Serializable]
    public sealed class InventoryStack
    {
        public string ItemId;
        public int Quantity;
        public FarmItemQuality Quality;

        public InventoryStack() { }
        public InventoryStack(string itemId, int quantity, FarmItemQuality quality = FarmItemQuality.Normal)
        {
            ItemId = itemId;
            Quantity = quantity;
            Quality = FarmItemQualityRules.Clamp(quality);
        }
    }

'@
$state = Replace-Section $state "    [Serializable]`n    public sealed class InventoryStack" "    [Serializable]`n    public sealed class FarmTileSaveData" $inventoryStack 'InventoryStack com qualidade'
$state = Replace-Checked $state '        public int Version = 17;' '        public int Version = 18;' 'versao default'

$inventoryMethods = @'
        public int GetQuantity(string itemId)
        {
            var total = 0;
            foreach (var stack in inventory)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase)) total += stack.Quantity;
            return total;
        }

        public int GetQuantity(string itemId, FarmItemQuality quality) =>
            GetQuantityInList(inventory, itemId, quality);

        public bool CanAdd(string itemId, int amount) =>
            CanAdd(itemId, amount, FarmItemQuality.Normal);

        public bool CanAdd(string itemId, int amount, FarmItemQuality quality) =>
            CanAddToList(inventory, slotCapacity, itemId, amount, quality);

        public bool AddItem(string itemId, int amount) =>
            AddItem(itemId, amount, FarmItemQuality.Normal);

        public bool AddItem(string itemId, int amount, FarmItemQuality quality)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 || !CanAdd(itemId, amount, quality)) return false;
            AddInternal(itemId, amount, quality);
            NotifyChanged();
            return true;
        }

        public bool TryRemoveItem(string itemId, int amount)
        {
            if (amount <= 0) return true;
            if (GetQuantity(itemId) < amount) return false;
            RemoveFromList(inventory, itemId, amount);
            NotifyChanged();
            return true;
        }

        public bool TryRemoveItem(string itemId, FarmItemQuality quality, int amount)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            if (amount <= 0) return true;
            if (GetQuantity(itemId, quality) < amount) return false;
            RemoveFromListExact(inventory, itemId, quality, amount);
            NotifyChanged();
            return true;
        }

'@
$state = Replace-Section $state '        public int GetQuantity(string itemId)' '        public string GetHotbarEntry(int index)' $inventoryMethods 'operacoes da mochila'

$transferMethods = @'
        public bool TransferToStorage(string itemId, int amount) =>
            TransferToStorage(itemId, FarmItemQuality.Normal, amount);

        public bool TransferToStorage(string itemId, FarmItemQuality quality, int amount)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 ||
                GetQuantity(itemId, quality) < amount || !CanAddToStorage(itemId, amount, quality)) return false;
            RemoveFromListExact(inventory, itemId, quality, amount);
            AddToStorageInternal(itemId, amount, quality);
            NotifyChanged();
            return true;
        }

        public bool TransferFromStorage(string itemId, int amount) =>
            TransferFromStorage(itemId, FarmItemQuality.Normal, amount);

        public bool TransferFromStorage(string itemId, FarmItemQuality quality, int amount)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 ||
                GetStorageQuantity(itemId, quality) < amount || !CanAdd(itemId, amount, quality)) return false;
            RemoveFromListExact(storage, itemId, quality, amount);
            AddInternal(itemId, amount, quality);
            NotifyChanged();
            return true;
        }

'@
$state = Replace-Section $state '        public bool TransferToStorage(string itemId, int amount)' '        public bool SortInventory()' $transferMethods 'transferencias por qualidade'
$state = Replace-Checked $state @'
        public int GetStorageQuantity(string itemId)
        {
            var total = 0;
            foreach (var stack in storage)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase)) total += stack.Quantity;
            return total;
        }
'@ @'
        public int GetStorageQuantity(string itemId)
        {
            var total = 0;
            foreach (var stack in storage)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase)) total += stack.Quantity;
            return total;
        }

        public int GetStorageQuantity(string itemId, FarmItemQuality quality) =>
            GetQuantityInList(storage, itemId, quality);
'@ 'quantidade por qualidade no deposito'

$sortMethods = @'
        private bool SortStacks(List<InventoryStack> stacks)
        {
            if (stacks == null || stacks.Count <= 1) return false;
            var previous = new List<InventoryStack>(stacks.Count);
            foreach (var stack in stacks) previous.Add(new InventoryStack(stack.ItemId, stack.Quantity, stack.Quality));
            stacks.Sort(CompareInventoryStacks);
            var changed = false;
            for (var index = 0; index < stacks.Count; index++)
            {
                if (string.Equals(stacks[index].ItemId, previous[index].ItemId, StringComparison.OrdinalIgnoreCase) &&
                    stacks[index].Quantity == previous[index].Quantity &&
                    stacks[index].Quality == previous[index].Quality) continue;
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
            var itemId = string.Compare(left?.ItemId, right?.ItemId, StringComparison.OrdinalIgnoreCase);
            if (itemId != 0) return itemId;
            return FarmItemQualityRules.Clamp(right != null ? right.Quality : FarmItemQuality.Normal)
                .CompareTo(FarmItemQualityRules.Clamp(left != null ? left.Quality : FarmItemQuality.Normal));
        }

'@
$state = Replace-Section $state '        private bool SortStacks(List<InventoryStack> stacks)' '        private static int InventoryCategoryOrder(ItemDefinition definition)' $sortMethods 'ordenacao por qualidade'

$sellMethods = @'
        public bool TrySellAll(CropDefinition crop, out int quantity, out int earned)
        {
            quantity = crop != null && crop.HarvestItem != null ? GetQuantity(crop.HarvestItem.Id) : 0;
            earned = crop != null && crop.HarvestItem != null
                ? SaleValueInList(inventory, crop.HarvestItem)
                : 0;
            if (quantity <= 0) return false;
            RemoveFromList(inventory, crop.HarvestItem.Id, quantity);
            money += earned;
            AddMasteryExperience(FarmMasterySkill.Commerce, quantity, false);
            NotifyChanged();
            return true;
        }

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
                earned += SaleValueInList(inventory, crop.HarvestItem);
                RemoveFromList(inventory, crop.HarvestItem.Id, cropQuantity);
                quantity += cropQuantity;
            }
            if (quantity <= 0) return false;
            money += earned;
            RecordJournal(FarmJournalMetric.SoldUnits, quantity, null, false);
            AddMasteryExperience(FarmMasterySkill.Commerce, quantity, false);
            NotifyChanged();
            return true;
        }

'@
$state = Replace-Section $state '        public bool TrySellAll(CropDefinition crop, out int quantity, out int earned)' '        public bool TryBuySeedPack(CropDefinition crop, out int amount, out int cost)' $sellMethods 'venda por qualidade'
$state = Replace-Checked $state '            foreach (var stack in inventory) stacks.Add(new InventoryStack(stack.ItemId, stack.Quantity));' '            foreach (var stack in inventory) stacks.Add(new InventoryStack(stack.ItemId, stack.Quantity, stack.Quality));' 'snapshot da mochila'
$state = Replace-Checked $state '                Version = 17,' '                Version = 18,' 'versao do snapshot'
$state = Replace-Checked $state '                    if (stack != null && stack.Quantity > 0 && !string.IsNullOrWhiteSpace(stack.ItemId)) AddInternal(stack.ItemId, stack.Quantity);' '                    if (stack != null && stack.Quantity > 0 && !string.IsNullOrWhiteSpace(stack.ItemId)) AddInternal(stack.ItemId, stack.Quantity, data.Version >= 18 ? stack.Quality : FarmItemQuality.Normal);' 'restore da mochila'
$state = Replace-Checked $state '                    if (stack != null && stack.Quantity > 0 && !string.IsNullOrWhiteSpace(stack.ItemId)) AddToStorageInternal(stack.ItemId, stack.Quantity);' '                    if (stack != null && stack.Quantity > 0 && !string.IsNullOrWhiteSpace(stack.ItemId)) AddToStorageInternal(stack.ItemId, stack.Quantity, data.Version >= 18 ? stack.Quality : FarmItemQuality.Normal);' 'restore do deposito'

$storagePublic = @'
        public bool TryAddToStorage(string itemId, int amount) =>
            TryAddToStorage(itemId, amount, FarmItemQuality.Normal);

        public bool TryAddToStorage(string itemId, int amount, FarmItemQuality quality)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 || !CanAddToStorage(itemId, amount, quality)) return false;
            AddToStorageInternal(itemId, amount, quality);
            NotifyChanged();
            return true;
        }

'@
$state = Replace-Section $state '        public bool TryAddToStorage(string itemId, int amount)' '        private void EnsureHotbar()' $storagePublic 'adicao ao deposito'

$listOps = @'
        private void AddInternal(string itemId, int amount, FarmItemQuality quality = FarmItemQuality.Normal)
        {
            AddToList(inventory, slotCapacity, itemId, amount, quality);
        }

        private bool CanAddToStorage(string itemId, int amount, FarmItemQuality quality = FarmItemQuality.Normal) =>
            CanAddToList(storage, storageSlotCapacity, itemId, amount, quality);

        private void AddToStorageInternal(string itemId, int amount, FarmItemQuality quality = FarmItemQuality.Normal)
        {
            AddToList(storage, storageSlotCapacity, itemId, amount, quality);
        }

        private void AddToList(List<InventoryStack> stacks, int capacity, string itemId, int amount, FarmItemQuality quality)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            var item = FarmContentDatabase.GetItem(itemId);
            if (item != null && amount > 0) RecordDiscoveredItem(itemId, false);
            var maxStack = item != null ? Mathf.Max(1, item.MaxStack) : 99;
            foreach (var stack in stacks)
            {
                if (!string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase) ||
                    FarmItemQualityRules.Clamp(stack.Quality) != quality || stack.Quantity >= maxStack) continue;
                var moved = Mathf.Min(maxStack - stack.Quantity, amount);
                stack.Quantity += moved;
                amount -= moved;
                if (amount <= 0) return;
            }
            while (amount > 0 && stacks.Count < capacity)
            {
                var moved = Mathf.Min(maxStack, amount);
                stacks.Add(new InventoryStack(itemId, moved, quality));
                amount -= moved;
            }
        }

        private static bool CanAddToList(List<InventoryStack> stacks, int capacity, string itemId, int amount, FarmItemQuality quality)
        {
            if (amount <= 0) return true;
            if (string.IsNullOrWhiteSpace(itemId)) return false;
            quality = FarmItemQualityRules.Clamp(quality);
            var item = FarmContentDatabase.GetItem(itemId);
            var maxStack = item != null ? Mathf.Max(1, item.MaxStack) : 99;
            var free = Mathf.Max(0, capacity - stacks.Count) * maxStack;
            foreach (var stack in stacks)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase) &&
                    FarmItemQualityRules.Clamp(stack.Quality) == quality)
                    free += Mathf.Max(0, maxStack - stack.Quantity);
            return free >= amount;
        }

        private static int GetQuantityInList(List<InventoryStack> stacks, string itemId, FarmItemQuality quality)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            var total = 0;
            foreach (var stack in stacks)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase) &&
                    FarmItemQualityRules.Clamp(stack.Quality) == quality)
                    total += stack.Quantity;
            return total;
        }

        private static void RemoveFromList(List<InventoryStack> stacks, string itemId, int amount)
        {
            for (var quality = FarmItemQuality.Normal; quality <= FarmItemQuality.Gold && amount > 0; quality++)
            {
                var available = GetQuantityInList(stacks, itemId, quality);
                var remove = Mathf.Min(available, amount);
                RemoveFromListExact(stacks, itemId, quality, remove);
                amount -= remove;
            }
        }

        private static void RemoveFromListExact(List<InventoryStack> stacks, string itemId, FarmItemQuality quality, int amount)
        {
            quality = FarmItemQualityRules.Clamp(quality);
            for (var index = stacks.Count - 1; index >= 0 && amount > 0; index--)
            {
                var stack = stacks[index];
                if (!string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase) ||
                    FarmItemQualityRules.Clamp(stack.Quality) != quality) continue;
                var removed = Mathf.Min(stack.Quantity, amount);
                stack.Quantity -= removed;
                amount -= removed;
                if (stack.Quantity <= 0) stacks.RemoveAt(index);
            }
        }

        private static int SaleValueInList(List<InventoryStack> stacks, ItemDefinition definition)
        {
            var total = 0;
            foreach (var stack in stacks)
                if (string.Equals(stack.ItemId, definition.Id, StringComparison.OrdinalIgnoreCase))
                    total += stack.Quantity * FarmItemQualityRules.UnitSellPrice(definition, stack.Quality);
            return total;
        }

'@
$state = Replace-Section $state '        private void AddInternal(string itemId, int amount)' '        private static List<FarmPlacedObjectSaveData> ClonePlacedObjects(List<FarmPlacedObjectSaveData> source)' $listOps 'operacoes internas de pilha'
$state = Replace-Checked $state '            foreach (var stack in source) result.Add(new InventoryStack(stack.ItemId, stack.Quantity));' '            foreach (var stack in source) result.Add(new InventoryStack(stack.ItemId, stack.Quantity, stack.Quality));' 'clone de pilhas'
Submit-UnityScript $statePath $state 'FarmGameState'

$plotPath = 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs'
$plot = Read-UnityScript $plotPath 'quality-plot'
$oldHarvest = @'
                var season = plot.DayClock != null ? plot.DayClock.CurrentSeason : FarmSeason.Spring;
                var harvestYield = cropDefinition.HarvestYieldForSeason(season);
                var sentToStorage = false;
                if (!inventory.AddItem(cropDefinition.HarvestItem.Id, harvestYield))
                {
                    if (inventory.GetMasteryLevel(FarmMasterySkill.Harvesting) < 3 || !inventory.TryAddToStorage(cropDefinition.HarvestItem.Id, harvestYield))
                        return "Invent\u00E1rio cheio. Libere espa\u00E7o antes de colher.";
                    sentToStorage = true;
                }
'@
$newHarvest = @'
                var season = plot.DayClock != null ? plot.DayClock.CurrentSeason : FarmSeason.Spring;
                var harvestYield = cropDefinition.HarvestYieldForSeason(season);
                var harvestQuality = FarmItemQualityRules.EvaluateHarvest(
                    cropDefinition, season, inventory.GetMasteryLevel(FarmMasterySkill.Harvesting));
                var sentToStorage = false;
                if (!inventory.AddItem(cropDefinition.HarvestItem.Id, harvestYield, harvestQuality))
                {
                    if (inventory.GetMasteryLevel(FarmMasterySkill.Harvesting) < 3 ||
                        !inventory.TryAddToStorage(cropDefinition.HarvestItem.Id, harvestYield, harvestQuality))
                        return "Invent\u00E1rio cheio. Libere espa\u00E7o antes de colher.";
                    sentToStorage = true;
                }
'@
$plot = Replace-Checked $plot $oldHarvest $newHarvest 'qualidade na colheita'
$oldMessage = @'
                if (sentToStorage) message += " Cesta de apoio: colheita enviada ao dep\u00F3sito.";
'@
$newMessage = @'
                message += $" Qualidade {FarmItemQualityRules.DisplayName(harvestQuality)}.";
                if (sentToStorage) message += " Cesta de apoio: colheita enviada ao dep\u00F3sito.";
'@
$plot = Replace-Checked $plot $oldMessage $newMessage 'feedback da qualidade'
$plot = Replace-Checked $plot 'saveStatus = data.Version < 17 ? "Save migrado para v17" : "Save carregado";' 'saveStatus = data.Version < 18 ? "Save migrado para v18" : "Save carregado";' 'status da migracao v18'
Submit-UnityScript $plotPath $plot 'FarmTestPlot'

$hudPath = 'Assets/_Project/Scripts/Farming/FarmHudController.cs'
$hud = Read-UnityScript $hudPath 'quality-hud'
$transferHud = @'
        public void TransferToStorage(string itemId, int requestedAmount) =>
            TransferToStorage(itemId, FarmItemQuality.Normal, requestedAmount);

        public void TransferToStorage(string itemId, FarmItemQuality quality, int requestedAmount)
        {
            if (plot == null || plot.GameState == null) return;
            var amount = Mathf.Min(requestedAmount, plot.GameState.GetQuantity(itemId, quality));
            if (amount > 0 && plot.GameState.TransferToStorage(itemId, quality, amount))
                storageFeedbackText.text = $"Guardado: {DisplayItemName(itemId)}{QualityInline(quality)} x{amount}.";
            else storageFeedbackText.text = "N\u00E3o foi poss\u00EDvel guardar: confira o espa\u00E7o do dep\u00F3sito.";
        }

        public void TransferFromStorage(string itemId, int requestedAmount) =>
            TransferFromStorage(itemId, FarmItemQuality.Normal, requestedAmount);

        public void TransferFromStorage(string itemId, FarmItemQuality quality, int requestedAmount)
        {
            if (plot == null || plot.GameState == null) return;
            var amount = Mathf.Min(requestedAmount, plot.GameState.GetStorageQuantity(itemId, quality));
            if (amount > 0 && plot.GameState.TransferFromStorage(itemId, quality, amount))
                storageFeedbackText.text = $"Retirado: {DisplayItemName(itemId)}{QualityInline(quality)} x{amount}.";
            else storageFeedbackText.text = "N\u00E3o foi poss\u00EDvel retirar: confira o espa\u00E7o da mochila.";
        }

        public int TransferHalf(string itemId, bool fromBackpack) =>
            TransferHalf(itemId, FarmItemQuality.Normal, fromBackpack);

        public int TransferHalf(string itemId, FarmItemQuality quality, bool fromBackpack)
        {
            if (plot == null || plot.GameState == null || string.IsNullOrWhiteSpace(itemId)) return 0;
            var available = fromBackpack
                ? plot.GameState.GetQuantity(itemId, quality)
                : plot.GameState.GetStorageQuantity(itemId, quality);
            if (available <= 0) return 0;
            var amount = Mathf.Max(1, Mathf.CeilToInt(available * 0.5f));
            var before = available;
            if (fromBackpack) TransferToStorage(itemId, quality, amount);
            else TransferFromStorage(itemId, quality, amount);
            var after = fromBackpack
                ? plot.GameState.GetQuantity(itemId, quality)
                : plot.GameState.GetStorageQuantity(itemId, quality);
            return Mathf.Max(0, before - after);
        }

'@
$hud = Replace-Section $hud '        public void TransferToStorage(string itemId, int requestedAmount)' '        public void SetInventoryFilter(FarmCollectionCategory category)' $transferHud 'transferencias do HUD'

$tooltip = @'
        public void ShowItemTooltip(string itemId, Vector2 screenPosition, string context) =>
            ShowItemTooltip(itemId, FarmItemQuality.Normal, screenPosition, context);

        public void ShowItemTooltip(string itemId, FarmItemQuality quality, Vector2 screenPosition, string context)
        {
            if (itemTooltipGroup == null || plot == null || plot.GameState == null ||
                string.IsNullOrWhiteSpace(itemId)) return;
            var definition = FarmContentDatabase.GetItem(itemId);
            if (definition == null) return;
            quality = FarmItemQualityRules.Clamp(quality);
            tooltipItemId = itemId;
            tooltipContext = context ?? string.Empty;
            var quantity = string.Equals(tooltipContext, "DEP\u00D3SITO", StringComparison.OrdinalIgnoreCase)
                ? plot.GameState.GetStorageQuantity(itemId, quality)
                : plot.GameState.GetQuantity(itemId, quality);
            itemTooltipTitle.text = definition.DisplayName.ToUpperInvariant() + QualityInline(quality).ToUpperInvariant();
            itemTooltipTitle.color = QualityColor(quality);
            var category = definition.Category switch
            {
                ItemCategory.Seed => "SEMENTE",
                ItemCategory.Crop => "COLHEITA",
                ItemCategory.Tool => "FERRAMENTA",
                ItemCategory.Material => "MATERIAL",
                _ => "ITEM"
            };
            var use = definition.Category switch
            {
                ItemCategory.Seed => "Plante em um canteiro preparado.",
                ItemCategory.Crop => "Venda, entregue em pedidos ou guarde.",
                ItemCategory.Tool => "Equipe pela barra r\u00E1pida.",
                ItemCategory.Material => "Use em receitas e constru\u00E7\u00F5es.",
                _ => "Item da fazenda."
            };
            var unitValue = definition.Category == ItemCategory.Crop
                ? FarmItemQualityRules.UnitSellPrice(definition, quality)
                : definition.BaseSellPrice;
            var value = unitValue > 0 ? $"Valor unit\u00E1rio: ${unitValue}" : "Sem venda direta";
            itemTooltipBody.text =
                $"{category}  \u2022  {tooltipContext}  \u2022  QUALIDADE {FarmItemQualityRules.DisplayName(quality).ToUpperInvariant()}\n" +
                $"Quantidade: {quantity}  \u2022  Pilha m\u00E1x.: {definition.MaxStack}\n" +
                $"{value}\n{use}";
            SetCanvasGroup(itemTooltipGroup, true);
            itemTooltip.transform.SetAsLastSibling();
            MoveItemTooltip(screenPosition);
        }

'@
$hud = Replace-Section $hud '        public void ShowItemTooltip(string itemId, Vector2 screenPosition, string context)' '        public void MoveItemTooltip(Vector2 screenPosition)' $tooltip 'tooltip de qualidade'

$hud = Replace-Checked $hud '                inventoryNames[index].text = occupied ? (definition != null ? definition.DisplayName : stack.ItemId) : "Vazio";' '                inventoryNames[index].text = occupied ? (definition != null ? definition.DisplayName : stack.ItemId) + QualityInline(stack.Quality) : "Vazio";' 'nome da pilha na mochila'
$hud = Replace-Checked $hud '                inventoryNames[index].color = occupied ? Color.white : new Color(0.48f, 0.52f, 0.45f);' '                inventoryNames[index].color = occupied ? QualityColor(stack.Quality) : new Color(0.48f, 0.52f, 0.45f);' 'cor da qualidade na mochila'
$hud = Replace-Checked $hud '                inventorySlots[index].GetComponent<FarmInventorySlotView>().Initialize(this, occupied ? stack.ItemId : null);' '                inventorySlots[index].GetComponent<FarmInventorySlotView>().Initialize(this, occupied ? stack.ItemId : null, occupied ? stack.Quality : FarmItemQuality.Normal);' 'slot da mochila com qualidade'
$hud = Replace-Checked $hud '            label.text = occupied ? (definition != null ? ShortName(definition.DisplayName) : stack.ItemId.ToUpperInvariant()) : "VAZIO";' '            label.text = occupied ? (definition != null ? ShortName(definition.DisplayName) : stack.ItemId.ToUpperInvariant()) + QualityShort(stack.Quality) : "VAZIO";' 'nome no deposito'
$hud = Replace-Checked $hud '            label.color = occupied ? Color.white : new Color(0.48f, 0.52f, 0.45f);' '            label.color = occupied ? QualityColor(stack.Quality) : new Color(0.48f, 0.52f, 0.45f);' 'cor no deposito'
$hud = Replace-Checked $hud '            view.Initialize(this, fromBackpack, occupied ? stack.ItemId : null);' '            view.Initialize(this, fromBackpack, occupied ? stack.ItemId : null, occupied ? stack.Quality : FarmItemQuality.Normal);' 'slot do deposito com qualidade'
$hud = Replace-Checked $hud '                builder.Append("\u2022 ").Append(definition != null ? definition.DisplayName : stack.ItemId).Append("  x").Append(stack.Quantity).Append(''\n'');' '                builder.Append("\u2022 ").Append(definition != null ? definition.DisplayName : stack.ItemId).Append(QualityInline(stack.Quality)).Append("  x").Append(stack.Quantity).Append(''\n'');' 'resumo compacto com qualidade'

$displayHelpers = @'
        private static string DisplayItemName(string itemId)
        {
            var definition = FarmContentDatabase.GetItem(itemId);
            return definition != null ? definition.DisplayName : itemId;
        }

        private static string QualityInline(FarmItemQuality quality) =>
            FarmItemQualityRules.Clamp(quality) == FarmItemQuality.Normal
                ? string.Empty
                : $"  {FarmItemQualityRules.ShortMark(quality)} {FarmItemQualityRules.DisplayName(quality)}";

        private static string QualityShort(FarmItemQuality quality) =>
            FarmItemQualityRules.Clamp(quality) == FarmItemQuality.Normal
                ? string.Empty
                : $" {FarmItemQualityRules.ShortMark(quality)}";

        private static Color QualityColor(FarmItemQuality quality) => FarmItemQualityRules.Clamp(quality) switch
        {
            FarmItemQuality.Silver => new Color(0.72f, 0.86f, 0.95f),
            FarmItemQuality.Gold => new Color(1f, 0.76f, 0.22f),
            _ => Color.white
        };
'@
$hud = Replace-Section $hud '        private static string DisplayItemName(string itemId)' '        private static void ResolveEntry(string entry, FarmGameState state, out string icon, out string label, out string count, out Color color)' $displayHelpers 'helpers visuais de qualidade'
Submit-UnityScript $hudPath $hud 'FarmHudController'

$inventoryViewPath = 'Assets/_Project/Scripts/Farming/FarmInventoryUiInteractions.cs'
$inventoryView = Read-UnityScript $inventoryViewPath 'quality-inventory-view'
$inventoryView = Replace-Checked $inventoryView '        private string itemId;' "        private string itemId;`n        private FarmItemQuality quality;" 'estado de qualidade do slot'
$inventoryView = Replace-Checked $inventoryView @'
        public void Initialize(FarmHudController owner, string id)
        {
            hud = owner;
            itemId = id;
        }
'@ @'
        public void Initialize(FarmHudController owner, string id, FarmItemQuality itemQuality = FarmItemQuality.Normal)
        {
            hud = owner;
            itemId = id;
            quality = FarmItemQualityRules.Clamp(itemQuality);
        }
'@ 'inicializacao do slot'
$inventoryView = Replace-Checked $inventoryView '                hud?.ShowItemTooltip(itemId, eventData.position, "MOCHILA");' '                hud?.ShowItemTooltip(itemId, quality, eventData.position, "MOCHILA");' 'tooltip do slot'
Submit-UnityScript $inventoryViewPath $inventoryView 'FarmInventoryUiInteractions'

$storageViewPath = 'Assets/_Project/Scripts/Farming/FarmStorageUiInteractions.cs'
$storageView = Read-UnityScript $storageViewPath 'quality-storage-view'
$storageView = Replace-Checked $storageView '        private string itemId;' "        private string itemId;`n        private FarmItemQuality quality;" 'estado de qualidade no deposito'
$storageView = Replace-Checked $storageView @'
        public void Initialize(FarmHudController owner, bool sourceIsBackpack, string id)
        {
            hud = owner;
            fromBackpack = sourceIsBackpack;
            itemId = id;
        }
'@ @'
        public void Initialize(FarmHudController owner, bool sourceIsBackpack, string id, FarmItemQuality itemQuality = FarmItemQuality.Normal)
        {
            hud = owner;
            fromBackpack = sourceIsBackpack;
            itemId = id;
            quality = FarmItemQualityRules.Clamp(itemQuality);
        }
'@ 'inicializacao do deposito'
$storageView = Replace-Checked $storageView '                hud.TransferHalf(itemId, fromBackpack);' '                hud.TransferHalf(itemId, quality, fromBackpack);' 'metade por qualidade'
$storageView = Replace-Checked $storageView 'hud?.ShowItemTooltip(itemId, eventData.position, fromBackpack ? "MOCHILA" : "DEPÓSITO")' 'hud?.ShowItemTooltip(itemId, quality, eventData.position, fromBackpack ? "MOCHILA" : "DEPÓSITO")' 'tooltip do deposito'
$storageView = Replace-Checked $storageView '            if (fromBackpack) hud.TransferToStorage(itemId, amount);' '            if (fromBackpack) hud.TransferToStorage(itemId, quality, amount);' 'guardar qualidade exata'
$storageView = Replace-Checked $storageView '            else hud.TransferFromStorage(itemId, amount);' '            else hud.TransferFromStorage(itemId, quality, amount);' 'retirar qualidade exata'
Submit-UnityScript $storageViewPath $storageView 'FarmStorageUiInteractions'
