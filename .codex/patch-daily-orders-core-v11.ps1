$ErrorActionPreference='Stop'
$project='D:\Dev\Unity\Farm\Farm'
$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
function Replace-Once([string]$content,[string]$old,[string]$new,[string]$label){if(-not $content.Contains($old)){throw "Trecho nao encontrado: $label"};$content.Replace($old,$new)}
function Submit([string]$path,[string]$content,[string]$id){$payload=@{filePath=$path;content=$content;requestId=$id}|ConvertTo-Json -Compress;$result=$payload|& $cli run-tool script-update-or-create $project --input-file -;if($LASTEXITCODE-ne 0){throw "Falha: $path"};$result|Select-Object -Last 12;Start-Sleep -Seconds 2}

$orders=@'
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    [Serializable]
    public sealed class FarmDailyOrderProgress
    {
        public int Day = 1;
        public int CompletedMask;

        public bool IsCompleted(int index) => index >= 0 && index < 31 && (CompletedMask & (1 << index)) != 0;
        public void MarkCompleted(int index) { if (index >= 0 && index < 31) CompletedMask |= 1 << index; }
        public bool IsBoardComplete(int orderCount) => orderCount > 0 && (CompletedMask & ((1 << orderCount) - 1)) == ((1 << orderCount) - 1);
        public FarmDailyOrderProgress Clone() => new() { Day = Day, CompletedMask = CompletedMask };
    }

    public sealed class FarmDailyOrder
    {
        public string Id { get; init; }
        public int Day { get; init; }
        public int Slot { get; init; }
        public string CropId { get; init; }
        public int Quantity { get; init; }
        public int Reward { get; init; }
        public CropDefinition Crop => FarmContentDatabase.GetCrop(CropId);
        public string DisplayText => Crop != null ? $"{Crop.DisplayName} x{Quantity}" : $"{CropId} x{Quantity}";
    }

    public static class FarmDailyOrderGenerator
    {
        public const int OrderCount = 3;
        public const int BoardCompletionBonus = 25;

        public static List<FarmDailyOrder> Generate(int worldSeed, int day)
        {
            day = Mathf.Max(1, day);
            var crops = new List<CropDefinition>();
            foreach (var crop in FarmContentDatabase.Crops)
                if (crop != null && crop.HarvestItem != null) crops.Add(crop);
            crops.Sort((left, right) => string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase));
            DeterministicShuffle(crops, Hash(worldSeed, day, 0));

            var result = new List<FarmDailyOrder>(Mathf.Min(OrderCount, crops.Count));
            for (var slot = 0; slot < OrderCount && slot < crops.Count; slot++)
            {
                var crop = crops[slot];
                var hash = Hash(worldSeed, day, slot + 1);
                var quantity = 2 + (int)(hash % 3u);
                var baseValue = Mathf.Max(1, crop.HarvestItem.BaseSellPrice) * quantity;
                var reward = Mathf.CeilToInt((baseValue * 1.6f) / 5f) * 5;
                result.Add(new FarmDailyOrder
                {
                    Id = $"daily:{day}:{slot}:{crop.Id}:{quantity}",
                    Day = day,
                    Slot = slot,
                    CropId = crop.Id,
                    Quantity = quantity,
                    Reward = Mathf.Max(baseValue + 5, reward)
                });
            }
            return result;
        }

        private static void DeterministicShuffle(List<CropDefinition> crops, uint seed)
        {
            for (var index = crops.Count - 1; index > 0; index--)
            {
                seed = Mix(seed + (uint)index);
                var swap = (int)(seed % (uint)(index + 1));
                (crops[index], crops[swap]) = (crops[swap], crops[index]);
            }
        }

        private static uint Hash(int worldSeed, int day, int slot)
        {
            unchecked
            {
                var value = (uint)(worldSeed == 0 ? FarmGameState.DefaultWorldSeed : worldSeed);
                value ^= (uint)day * 747796405u;
                value ^= (uint)(slot + 1) * 2891336453u;
                return Mix(value);
            }
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 2246822519u;
            value ^= value >> 13;
            value *= 3266489917u;
            value ^= value >> 16;
            return value;
        }
    }
}
'@
Submit 'Assets/_Project/Scripts/Farming/FarmDailyOrders.cs' $orders 'daily-orders-core-v11'

$statePath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmGameState.cs'
$state=[IO.File]::ReadAllText($statePath)
if(-not $state.Contains('DailyOrders =')){
  $state=Replace-Once $state 'public int Version = 10;' 'public int Version = 11;' 'save version field'
  $state=Replace-Once $state @'
        public FarmJournalProgress Journal = new();
        public int PumpkinSeeds;
'@ @'
        public FarmJournalProgress Journal = new();
        public FarmDailyOrderProgress DailyOrders = new();
        public int PumpkinSeeds;
'@ 'save order progress'
  $state=Replace-Once $state @'
        [SerializeField] private FarmJournalProgress journal = new();
'@ @'
        [SerializeField] private FarmJournalProgress journal = new();
        [SerializeField] private FarmDailyOrderProgress dailyOrders = new();
'@ 'runtime order progress'
  $state=Replace-Once $state @'
        public FarmJournalProgress Journal => journal;
'@ @'
        public FarmJournalProgress Journal => journal;
        public FarmDailyOrderProgress DailyOrders
        {
            get
            {
                EnsureDailyOrdersForCurrentDay(false);
                return dailyOrders;
            }
        }
'@ 'order property'
  $state=Replace-Once $state @'
            if (notify) NotifyChanged();
            return dayNumber != previousDay;
        }

        public void SetClock
'@ @'
            var dayChanged = dayNumber != previousDay;
            if (dayChanged) EnsureDailyOrdersForCurrentDay(false);
            if (notify) NotifyChanged();
            return dayChanged;
        }

        public void SetClock
'@ 'advance clock reset'
  $state=Replace-Once $state @'
            minutesOfDay = Mathf.Repeat(totalMinutes, 1440f);
            if (notify) NotifyChanged();
        }

        public void SetWorldSeed
'@ @'
            minutesOfDay = Mathf.Repeat(totalMinutes, 1440f);
            EnsureDailyOrdersForCurrentDay(false);
            if (notify) NotifyChanged();
        }

        public void SetWorldSeed
'@ 'set clock reset'
  $state=Replace-Once $state @'
        public bool TryClaimJournalReward(string questId, out int reward)
'@ @'
        public bool TryCompleteDailyOrder(FarmDailyOrder order, int index, out int earned, out int completionBonus, out string error)
        {
            earned = 0;
            completionBonus = 0;
            error = string.Empty;
            EnsureDailyOrdersForCurrentDay(false);
            if (order == null || order.Day != dayNumber || index < 0 || index >= FarmDailyOrderGenerator.OrderCount)
            {
                error = "Esse pedido n\u00E3o pertence ao dia atual.";
                return false;
            }
            if (dailyOrders.IsCompleted(index))
            {
                error = "Esse pedido j\u00E1 foi entregue.";
                return false;
            }
            var crop = order.Crop;
            if (crop == null || crop.HarvestItem == null)
            {
                error = "O pedido possui uma cultura inv\u00E1lida.";
                return false;
            }
            if (GetQuantity(crop.HarvestItem.Id) < order.Quantity)
            {
                error = $"Faltam {order.Quantity - GetQuantity(crop.HarvestItem.Id)} {crop.DisplayName.ToLowerInvariant()}(s).";
                return false;
            }

            RemoveFromList(inventory, crop.HarvestItem.Id, order.Quantity);
            dailyOrders.MarkCompleted(index);
            if (dailyOrders.IsBoardComplete(FarmDailyOrderGenerator.OrderCount)) completionBonus = FarmDailyOrderGenerator.BoardCompletionBonus;
            earned = order.Reward + completionBonus;
            money += earned;
            RecordJournal(FarmJournalMetric.SoldUnits, order.Quantity, null, false);
            NotifyChanged();
            return true;
        }

        private void EnsureDailyOrdersForCurrentDay(bool notify)
        {
            dailyOrders ??= new FarmDailyOrderProgress();
            if (dailyOrders.Day == dayNumber) return;
            dailyOrders.Day = dayNumber;
            dailyOrders.CompletedMask = 0;
            if (notify) NotifyChanged();
        }

        public bool TryClaimJournalReward(string questId, out int reward)
'@ 'order completion API'
  $state=Replace-Once $state 'Version = 10,' 'Version = 11,' 'create save version'
  $state=Replace-Once $state @'
                Journal = (journal ?? new FarmJournalProgress()).Clone(),
                PumpkinSeeds = PumpkinSeeds,
'@ @'
                Journal = (journal ?? new FarmJournalProgress()).Clone(),
                DailyOrders = DailyOrders.Clone(),
                PumpkinSeeds = PumpkinSeeds,
'@ 'save order progress'
  $state=Replace-Once $state @'
            journal = data.Version >= 10 && data.Journal != null ? data.Journal.Clone() : new FarmJournalProgress();
            NotifyChanged();
'@ @'
            journal = data.Version >= 10 && data.Journal != null ? data.Journal.Clone() : new FarmJournalProgress();
            dailyOrders = data.Version >= 11 && data.DailyOrders != null
                ? data.DailyOrders.Clone()
                : new FarmDailyOrderProgress { Day = dayNumber, CompletedMask = 0 };
            EnsureDailyOrdersForCurrentDay(false);
            NotifyChanged();
'@ 'restore order progress'
  Submit 'Assets/_Project/Scripts/Farming/FarmGameState.cs' $state 'game-state-daily-orders-v11'
}
Write-Output 'Daily orders core v11 submitted.'
