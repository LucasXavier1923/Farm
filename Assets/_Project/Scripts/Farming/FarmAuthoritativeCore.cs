using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmCommandFailure { None, InvalidRequest, RequestInProgress, UnknownTile, TileNotUntilled, TileNotPrepared, TileNotSeeded, TileNotReady, TileNotFertilizable, AlreadyFertilized, InsufficientItems, InventoryFull, OutOfSeason }

    public sealed class FarmInventoryEntry
    {
        public string ItemId { get; }
        public int Quantity { get; }
        public FarmItemQuality Quality { get; }
        public FarmInventoryEntry(string itemId, int quantity, FarmItemQuality quality = FarmItemQuality.Normal)
        {
            ItemId = itemId ?? string.Empty;
            Quantity = Mathf.Max(0, quantity);
            Quality = FarmItemQualityRules.Clamp(quality);
        }
    }

    public sealed class FarmInventorySnapshot
    {
        public IReadOnlyList<FarmInventoryEntry> Items { get; }
        public IReadOnlyList<string> Hotbar { get; }
        public int SelectedHotbarIndex { get; }
        public FarmInventorySnapshot(IEnumerable<FarmInventoryEntry> items, IEnumerable<string> hotbar, int selectedHotbarIndex)
        {
            Items = (items ?? Array.Empty<FarmInventoryEntry>()).Select(item => new FarmInventoryEntry(item.ItemId, item.Quantity, item.Quality)).ToArray();
            Hotbar = (hotbar ?? Array.Empty<string>()).Select(entry => entry ?? string.Empty).ToArray();
            SelectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, FarmGameState.HotbarSlotCount - 1);
        }
    }

    public sealed class FarmTileSnapshot
    {
        public int TileIndex { get; }
        public bool IsPrepared { get; }
        public string CropId { get; }
        public bool IsWatered { get; }
        public bool IsFertilized { get; }
        public string LastHarvestedCropId { get; }
        public bool IsRotated { get; }
        public FarmTileSnapshot(int tileIndex, bool isPrepared, string cropId, bool isWatered = false, bool isFertilized = false, string lastHarvestedCropId = null, bool isRotated = false)
        {
            TileIndex = tileIndex; IsPrepared = isPrepared; CropId = cropId ?? string.Empty; IsWatered = isWatered; IsFertilized = isFertilized;
            LastHarvestedCropId = lastHarvestedCropId ?? string.Empty; IsRotated = isRotated;
        }
    }

    public sealed class FarmPrepareSoilRequest
    {
        public string CommandId { get; }
        public int TileIndex { get; }
        public FarmPrepareSoilRequest(string commandId, int tileIndex) { CommandId = commandId; TileIndex = tileIndex; }
    }

    public sealed class FarmPlantSeedRequest
    {
        public string CommandId { get; }
        public int TileIndex { get; }
        public string SeedItemId { get; }
        public string CropId { get; }
        public bool IsWatered { get; }
        public FarmPlantSeedRequest(string commandId, int tileIndex, string seedItemId, string cropId)
        {
            CommandId = commandId; TileIndex = tileIndex; SeedItemId = seedItemId ?? string.Empty; CropId = cropId ?? string.Empty;
        }
    }

    public sealed class FarmWaterTileRequest
    {
        public string CommandId { get; }
        public int TileIndex { get; }
        public FarmWaterTileRequest(string commandId, int tileIndex) { CommandId = commandId; TileIndex = tileIndex; }
    }

    public sealed class FarmFertilizeTileRequest
    {
        public string CommandId { get; }
        public int TileIndex { get; }
        public string FertilizerItemId { get; }
        public FarmFertilizeTileRequest(string commandId, int tileIndex, string fertilizerItemId)
        {
            CommandId = commandId;
            TileIndex = tileIndex;
            FertilizerItemId = fertilizerItemId ?? string.Empty;
        }
    }

    public sealed class FarmSetHotbarRequest
    {
        public string CommandId { get; }
        public int HotbarIndex { get; }
        public string ItemId { get; }
        public FarmSetHotbarRequest(string commandId, int hotbarIndex, string itemId) { CommandId = commandId; HotbarIndex = hotbarIndex; ItemId = itemId ?? string.Empty; }
    }

    public sealed class FarmSelectHotbarRequest
    {
        public string CommandId { get; }
        public int HotbarIndex { get; }
        public FarmSelectHotbarRequest(string commandId, int hotbarIndex) { CommandId = commandId; HotbarIndex = hotbarIndex; }
    }

    public sealed class FarmSwapHotbarRequest
    {
        public string CommandId { get; }
        public int SourceIndex { get; }
        public int TargetIndex { get; }
        public FarmSwapHotbarRequest(string commandId, int sourceIndex, int targetIndex)
        {
            CommandId = commandId;
            SourceIndex = sourceIndex;
            TargetIndex = targetIndex;
        }
    }

    public sealed class FarmHarvestTileRequest
    {
        public string CommandId { get; }
        public int TileIndex { get; }
        public FarmHarvestTileRequest(string commandId, int tileIndex) { CommandId = commandId; TileIndex = tileIndex; }
    }

    public sealed class FarmHarvestSnapshot
    {
        public string CropId { get; }
        public string ItemId { get; }
        public int Yield { get; }
        public FarmItemQuality Quality { get; }
        public bool Replanted { get; }
        public bool WasFertilized { get; }
        public bool WasRotated { get; }
        public FarmHarvestSnapshot(string cropId, string itemId, int yield, FarmItemQuality quality, bool replanted, bool wasFertilized = false, bool wasRotated = false)
        {
            CropId = cropId ?? string.Empty;
            ItemId = itemId ?? string.Empty;
            Yield = Mathf.Max(0, yield);
            Quality = FarmItemQualityRules.Clamp(quality);
            Replanted = replanted;
            WasFertilized = wasFertilized;
            WasRotated = wasRotated;
        }
    }

    public sealed class FarmCommandResult
    {
        public bool Succeeded { get; }
        public FarmCommandFailure Failure { get; }
        public string Message { get; }
        public FarmInventorySnapshot Inventory { get; }
        public FarmTileSnapshot Tile { get; }
        public FarmHarvestSnapshot Harvest { get; }
        private FarmCommandResult(bool succeeded, FarmCommandFailure failure, string message, FarmInventorySnapshot inventory, FarmTileSnapshot tile, FarmHarvestSnapshot harvest)
        {
            Succeeded = succeeded; Failure = failure; Message = message ?? string.Empty; Inventory = inventory; Tile = tile; Harvest = harvest;
        }
        public static FarmCommandResult Success(string message, FarmInventorySnapshot inventory, FarmTileSnapshot tile = null, FarmHarvestSnapshot harvest = null) => new(true, FarmCommandFailure.None, message, inventory, tile, harvest);
        public static FarmCommandResult Fail(FarmCommandFailure failure, string message, FarmInventorySnapshot inventory = null, FarmTileSnapshot tile = null) => new(false, failure, message, inventory, tile, null);
    }

    public interface IFarmBackend
    {
        Task<FarmInventorySnapshot> GetInventoryAsync(CancellationToken cancellationToken);
        Task<FarmCommandResult> PrepareSoilAsync(FarmPrepareSoilRequest request, CancellationToken cancellationToken);
        Task<FarmCommandResult> PlantSeedAsync(FarmPlantSeedRequest request, CancellationToken cancellationToken);
        Task<FarmCommandResult> WaterTileAsync(FarmWaterTileRequest request, CancellationToken cancellationToken);
        Task<FarmCommandResult> FertilizeTileAsync(FarmFertilizeTileRequest request, CancellationToken cancellationToken);
        Task<FarmCommandResult> HarvestTileAsync(FarmHarvestTileRequest request, CancellationToken cancellationToken);
        Task<FarmCommandResult> SetHotbarAsync(FarmSetHotbarRequest request, CancellationToken cancellationToken);
        Task<FarmCommandResult> SelectHotbarAsync(FarmSelectHotbarRequest request, CancellationToken cancellationToken);
        Task<FarmCommandResult> SwapHotbarAsync(FarmSwapHotbarRequest request, CancellationToken cancellationToken);
    }

    public sealed class FarmMockBackend : IFarmBackend
    {
        private sealed class TileState
        {
            public bool IsPrepared;
            public bool IsWatered;
            public bool IsFertilized;
            public string CropId = string.Empty;
            public string LastHarvestedCropId = string.Empty;
            public bool IsRotated;
            public float ReadyAt;
        }
        private readonly List<FarmInventoryEntry> inventory = new();
        private readonly List<string> hotbar = new();
        private readonly Dictionary<int, TileState> tiles = new();
        private readonly Dictionary<string, FarmCommandResult> completedCommands = new(StringComparer.Ordinal);
        private readonly SemaphoreSlim gate = new(1, 1);
        private readonly int latencyMilliseconds;
        private readonly int inventoryCapacity;
        private readonly Func<FarmSeason> seasonProvider;
        private readonly Func<int> harvestingLevelProvider;
        private readonly Func<int> cultivationLevelProvider;
        private readonly Func<float> nowProvider;
        private readonly Func<int, bool> greenhouseCoverageProvider;
        private int selectedHotbarIndex;

        public FarmMockBackend(
            IEnumerable<int> tileIndexes,
            string initialSeedItemId,
            int initialSeedQuantity,
            int inventoryCapacity,
            int latencyMilliseconds,
            Func<FarmSeason> seasonProvider,
            Func<int> harvestingLevelProvider,
            Func<int> cultivationLevelProvider,
            Func<float> nowProvider,
            Func<int, bool> greenhouseCoverageProvider = null)
        {
            this.latencyMilliseconds = Mathf.Max(0, latencyMilliseconds);
            this.inventoryCapacity = Mathf.Max(1, inventoryCapacity);
            this.seasonProvider = seasonProvider ?? (() => FarmSeason.Spring);
            this.harvestingLevelProvider = harvestingLevelProvider ?? (() => 1);
            this.cultivationLevelProvider = cultivationLevelProvider ?? (() => 1);
            this.nowProvider = nowProvider ?? (() => FarmSessionTime.Now);
            this.greenhouseCoverageProvider = greenhouseCoverageProvider ?? (_ => false);
            if (!string.IsNullOrWhiteSpace(initialSeedItemId) && initialSeedQuantity > 0) AddInventory(initialSeedItemId, initialSeedQuantity, FarmItemQuality.Normal);
            foreach (var tileIndex in tileIndexes ?? Array.Empty<int>()) tiles[tileIndex] = new TileState();
            hotbar.AddRange(new[] { "tool:hoe", FarmGameState.ItemPrefix + initialSeedItemId, "tool:watering_can", "tool:harvest", string.Empty, string.Empty, string.Empty, string.Empty });
        }

        public Task<FarmInventorySnapshot> GetInventoryAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            gate.Wait(cancellationToken);
            try { return Task.FromResult(BuildInventorySnapshot()); }
            finally { gate.Release(); }
        }

        public Task<FarmCommandResult> PrepareSoilAsync(FarmPrepareSoilRequest request, CancellationToken cancellationToken)
        {
            if (request == null) return Task.FromResult(FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.prepare.invalid", "Invalid soil preparation request.")));
            return ExecuteCommandAsync(request.CommandId, () =>
            {
                if (!tiles.TryGetValue(request.TileIndex, out var tile)) return Fail(FarmCommandFailure.UnknownTile, FarmLocalization.Get("backend.tile.unknown", "Unknown plot tile."));
                if (tile.IsPrepared || !string.IsNullOrEmpty(tile.CropId)) return Fail(FarmCommandFailure.TileNotUntilled, FarmLocalization.Get("backend.tile.already_prepared", "This plot tile is already prepared."), request.TileIndex, tile);
                tile.IsPrepared = true;
                return FarmCommandResult.Success(FarmLocalization.Get("backend.soil.prepared", "Soil prepared."), BuildInventorySnapshot(), Snapshot(request.TileIndex, tile));
            }, cancellationToken);
        }

        public Task<FarmCommandResult> PlantSeedAsync(FarmPlantSeedRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SeedItemId) || string.IsNullOrWhiteSpace(request.CropId)) return Task.FromResult(FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.plant.invalid", "Invalid planting request.")));
            return ExecuteCommandAsync(request.CommandId, () =>
            {
                if (!tiles.TryGetValue(request.TileIndex, out var tile)) return Fail(FarmCommandFailure.UnknownTile, FarmLocalization.Get("backend.tile.unknown", "Unknown plot tile."));
                if (!tile.IsPrepared || !string.IsNullOrEmpty(tile.CropId)) return Fail(FarmCommandFailure.TileNotPrepared, FarmLocalization.Get("backend.plant.requires_prepared", "Plant only in prepared soil."), request.TileIndex, tile);
                var crop = FarmContentDatabase.GetCrop(request.CropId);
                if (crop == null || crop.SeedItem == null || !string.Equals(crop.SeedItem.Id, request.SeedItemId, StringComparison.OrdinalIgnoreCase))
                    return Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.plant.invalid", "Invalid planting request."), request.TileIndex, tile);
                var climateControlled = greenhouseCoverageProvider(request.TileIndex);
                if (seasonProvider() != crop.PreferredSeason && !climateControlled)
                    return Fail(FarmCommandFailure.OutOfSeason, FarmLocalization.Format("backend.plant.out_of_season", "{0} can only be planted in {1}, unless this tile is covered by a greenhouse.", crop.LocalizedName, FarmDayClock.SeasonName(crop.PreferredSeason)), request.TileIndex, tile);
                if (!TryRemoveInventory(request.SeedItemId, 1, FarmItemQuality.Normal)) return Fail(FarmCommandFailure.InsufficientItems, FarmLocalization.Get("backend.plant.seed_missing", "You do not have that seed."), request.TileIndex, tile);
                tile.CropId = request.CropId;
                tile.IsRotated = FarmSoilRules.IsRotation(tile.LastHarvestedCropId, request.CropId);
                tile.IsWatered = false;
                tile.ReadyAt = 0f;
                return FarmCommandResult.Success(FarmLocalization.Get("backend.plant.confirmed", "Seed confirmed by server."), BuildInventorySnapshot(), Snapshot(request.TileIndex, tile));
            }, cancellationToken);
        }

        public Task<FarmCommandResult> WaterTileAsync(FarmWaterTileRequest request, CancellationToken cancellationToken)
        {
            if (request == null) return Task.FromResult(FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.water.invalid", "Invalid watering request.")));
            return ExecuteCommandAsync(request.CommandId, () =>
            {
                if (!tiles.TryGetValue(request.TileIndex, out var tile)) return Fail(FarmCommandFailure.UnknownTile, FarmLocalization.Get("backend.tile.unknown", "Unknown plot tile."));
                if (!tile.IsPrepared || string.IsNullOrEmpty(tile.CropId)) return Fail(FarmCommandFailure.TileNotSeeded, FarmLocalization.Get("backend.water.requires_seed", "Plant a seed before watering."), request.TileIndex, tile);
                if (tile.IsWatered) return Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.water.already", "This plot tile is already watered."), request.TileIndex, tile);
                var crop = FarmContentDatabase.GetCrop(tile.CropId);
                if (crop == null) return Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.harvest.crop_unknown", "The plot crop is invalid."), request.TileIndex, tile);
                tile.IsWatered = true;
                tile.ReadyAt = nowProvider() + crop.GrowthSeconds;
                return FarmCommandResult.Success(FarmLocalization.Get("backend.water.confirmed", "Watering confirmed by server."), BuildInventorySnapshot(), Snapshot(request.TileIndex, tile));
            }, cancellationToken);
        }

        public Task<FarmCommandResult> FertilizeTileAsync(FarmFertilizeTileRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FertilizerItemId))
                return Task.FromResult(FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.fertilize.invalid", "Invalid compost request.")));
            return ExecuteCommandAsync(request.CommandId, () =>
            {
                if (!tiles.TryGetValue(request.TileIndex, out var tile)) return Fail(FarmCommandFailure.UnknownTile, FarmLocalization.Get("backend.tile.unknown", "Unknown plot tile."));
                if (!tile.IsPrepared || (tile.IsWatered && tile.ReadyAt > 0f && tile.ReadyAt <= nowProvider()))
                    return Fail(FarmCommandFailure.TileNotFertilizable, FarmLocalization.Get("backend.fertilize.requires_active_crop", "Compost can only enrich prepared soil or a crop before it is ready."), request.TileIndex, tile);
                if (tile.IsFertilized) return Fail(FarmCommandFailure.AlreadyFertilized, FarmLocalization.Get("backend.fertilize.already", "This plot tile is already enriched."), request.TileIndex, tile);
                var fertilizer = FarmContentDatabase.GetItem(request.FertilizerItemId);
                if (fertilizer == null || fertilizer.Category != ItemCategory.Fertilizer)
                    return Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.fertilize.invalid", "Invalid compost request."), request.TileIndex, tile);
                if (!TryRemoveInventory(fertilizer.Id, 1, FarmItemQuality.Normal))
                    return Fail(FarmCommandFailure.InsufficientItems, FarmLocalization.Format("backend.fertilize.missing", "You do not have {0}.", fertilizer.LocalizedName), request.TileIndex, tile);
                tile.IsFertilized = true;
                return FarmCommandResult.Success(FarmLocalization.Get("backend.fertilize.confirmed", "Compost confirmed by server. This crop will yield +1 and gain quality."), BuildInventorySnapshot(), Snapshot(request.TileIndex, tile));
            }, cancellationToken);
        }

        public Task<FarmCommandResult> HarvestTileAsync(FarmHarvestTileRequest request, CancellationToken cancellationToken)
        {
            if (request == null) return Task.FromResult(FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.harvest.invalid", "Invalid harvest request.")));
            return ExecuteCommandAsync(request.CommandId, () =>
            {
                if (!tiles.TryGetValue(request.TileIndex, out var tile)) return Fail(FarmCommandFailure.UnknownTile, FarmLocalization.Get("backend.tile.unknown", "Unknown plot tile."));
                if (!tile.IsPrepared || string.IsNullOrEmpty(tile.CropId)) return Fail(FarmCommandFailure.TileNotSeeded, FarmLocalization.Get("backend.harvest.requires_crop", "Plant a crop before harvesting."), request.TileIndex, tile);
                if (!tile.IsWatered || tile.ReadyAt > nowProvider()) return Fail(FarmCommandFailure.TileNotReady, FarmLocalization.Get("backend.harvest.not_ready", "The crop is not ready yet."), request.TileIndex, tile);
                var crop = FarmContentDatabase.GetCrop(tile.CropId);
                if (crop == null || crop.HarvestItem == null) return Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.harvest.crop_unknown", "The plot crop is invalid."), request.TileIndex, tile);

                var season = greenhouseCoverageProvider(request.TileIndex) ? crop.PreferredSeason : seasonProvider();
                var wasFertilized = tile.IsFertilized;
                var wasRotated = tile.IsRotated;
                var yield = crop.HarvestYieldForSeason(season) + (wasFertilized ? 1 : 0);
                var quality = FarmItemQualityRules.EvaluateHarvest(crop, season, Mathf.Max(1, harvestingLevelProvider()), wasFertilized, wasRotated);
                if (!CanAddInventory(crop.HarvestItem.Id, yield, quality))
                    return Fail(FarmCommandFailure.InventoryFull, FarmLocalization.Get("backend.harvest.inventory_full", "Inventory full. Make room before harvesting."), request.TileIndex, tile);

                AddInventory(crop.HarvestItem.Id, yield, quality);
                var replanted = cultivationLevelProvider() >= 3 && crop.SeedItem != null &&
                    TryRemoveInventory(crop.SeedItem.Id, 1, FarmItemQuality.Normal);
                tile.CropId = replanted ? crop.Id : string.Empty;
                tile.LastHarvestedCropId = crop.Id;
                tile.IsRotated = false;
                tile.IsWatered = false;
                tile.IsFertilized = false;
                tile.ReadyAt = 0f;
                var harvest = new FarmHarvestSnapshot(crop.Id, crop.HarvestItem.Id, yield, quality, replanted, wasFertilized, wasRotated);
                return FarmCommandResult.Success(FarmLocalization.Get("backend.harvest.confirmed", "Harvest confirmed by server."), BuildInventorySnapshot(), Snapshot(request.TileIndex, tile), harvest);
            }, cancellationToken);
        }

        public Task<FarmCommandResult> SetHotbarAsync(FarmSetHotbarRequest request, CancellationToken cancellationToken)
        {
            if (request == null) return Task.FromResult(FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.hotbar.invalid", "Invalid hotbar request.")));
            return ExecuteCommandAsync(request.CommandId, () =>
            {
                if (request.HotbarIndex < 0 || request.HotbarIndex >= FarmGameState.HotbarSlotCount) return Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.hotbar.slot_invalid", "Invalid hotbar slot."));
                var entry = string.Empty;
                if (FarmGameState.IsCoreToolEntry(request.ItemId)) entry = request.ItemId;
                else if (!string.IsNullOrWhiteSpace(request.ItemId))
                {
                    if (GetInventoryQuantity(request.ItemId) <= 0) return Fail(FarmCommandFailure.InsufficientItems, FarmLocalization.Get("backend.hotbar.item_missing", "That item is not in the inventory."));
                    entry = FarmGameState.ItemPrefix + request.ItemId;
                }
                hotbar[request.HotbarIndex] = entry;
                selectedHotbarIndex = request.HotbarIndex;
                return FarmCommandResult.Success(FarmLocalization.Get("backend.hotbar.confirmed", "Hotbar shortcut confirmed by server."), BuildInventorySnapshot());
            }, cancellationToken);
        }

        public Task<FarmCommandResult> SelectHotbarAsync(FarmSelectHotbarRequest request, CancellationToken cancellationToken)
        {
            if (request == null) return Task.FromResult(FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.hotbar.invalid", "Invalid hotbar request.")));
            return ExecuteCommandAsync(request.CommandId, () =>
            {
                if (request.HotbarIndex < 0 || request.HotbarIndex >= FarmGameState.HotbarSlotCount)
                    return Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.hotbar.slot_invalid", "Invalid hotbar slot."));
                selectedHotbarIndex = request.HotbarIndex;
                return FarmCommandResult.Success(FarmLocalization.Get("backend.hotbar.selection_confirmed", "Hotbar selection confirmed by server."), BuildInventorySnapshot());
            }, cancellationToken);
        }

        public Task<FarmCommandResult> SwapHotbarAsync(FarmSwapHotbarRequest request, CancellationToken cancellationToken)
        {
            if (request == null) return Task.FromResult(FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.hotbar.invalid", "Invalid hotbar request.")));
            return ExecuteCommandAsync(request.CommandId, () =>
            {
                if (request.SourceIndex < 0 || request.SourceIndex >= FarmGameState.HotbarSlotCount ||
                    request.TargetIndex < 0 || request.TargetIndex >= FarmGameState.HotbarSlotCount)
                    return Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.hotbar.slot_invalid", "Invalid hotbar slot."));
                if (request.SourceIndex == request.TargetIndex)
                    return FarmCommandResult.Success(FarmLocalization.Get("backend.hotbar.swap_confirmed", "Hotbar arrangement confirmed by server."), BuildInventorySnapshot());
                (hotbar[request.SourceIndex], hotbar[request.TargetIndex]) = (hotbar[request.TargetIndex], hotbar[request.SourceIndex]);
                if (selectedHotbarIndex == request.SourceIndex) selectedHotbarIndex = request.TargetIndex;
                else if (selectedHotbarIndex == request.TargetIndex) selectedHotbarIndex = request.SourceIndex;
                return FarmCommandResult.Success(FarmLocalization.Get("backend.hotbar.swap_confirmed", "Hotbar arrangement confirmed by server."), BuildInventorySnapshot());
            }, cancellationToken);
        }

        // The prototype still has local-only reward and storage systems. Keep the mock's
        // simulated server inventory aligned before it validates a hotbar assignment.
        public void SynchronizePrototypeInventory(IEnumerable<FarmInventoryEntry> entries)
        {
            gate.Wait();
            try
            {
                inventory.Clear();
                foreach (var entry in entries ?? Array.Empty<FarmInventoryEntry>())
                {
                    if (entry == null || entry.Quantity <= 0) continue;
                    AddInventory(entry.ItemId, entry.Quantity, entry.Quality);
                }
            }
            finally { gate.Release(); }
        }

        private async Task<FarmCommandResult> ExecuteCommandAsync(string commandId, Func<FarmCommandResult> operation, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(commandId)) return FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.command.missing_id", "Command has no identifier."));
            await SimulateLatencyAsync(cancellationToken);
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (completedCommands.TryGetValue(commandId, out var completed)) return completed;
                var result = operation();
                completedCommands[commandId] = result;
                return result;
            }
            finally { gate.Release(); }
        }

        private async Task SimulateLatencyAsync(CancellationToken cancellationToken) { if (latencyMilliseconds > 0) await Task.Delay(latencyMilliseconds, cancellationToken); }
        private int GetInventoryQuantity(string itemId, FarmItemQuality? quality = null)
        {
            var quantity = 0;
            foreach (var entry in inventory)
                if (string.Equals(entry.ItemId, itemId, StringComparison.OrdinalIgnoreCase) && (!quality.HasValue || entry.Quality == quality.Value)) quantity += entry.Quantity;
            return quantity;
        }

        private bool CanAddInventory(string itemId, int amount, FarmItemQuality quality)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return false;
            var definition = FarmContentDatabase.GetItem(itemId);
            if (definition == null) return false;
            var usedSlots = 0;
            foreach (var entry in inventory)
            {
                var entryDefinition = FarmContentDatabase.GetItem(entry.ItemId);
                if (entryDefinition == null) continue;
                usedSlots += Mathf.CeilToInt(entry.Quantity / (float)Mathf.Max(1, entryDefinition.MaxStack));
            }
            var current = GetInventoryQuantity(itemId, quality);
            var maxStack = Mathf.Max(1, definition.MaxStack);
            var currentSlots = Mathf.CeilToInt(current / (float)maxStack);
            var finalSlots = Mathf.CeilToInt((current + amount) / (float)maxStack);
            return usedSlots - currentSlots + finalSlots <= inventoryCapacity;
        }

        private void AddInventory(string itemId, int amount, FarmItemQuality quality)
        {
            if (amount <= 0) return;
            quality = FarmItemQualityRules.Clamp(quality);
            for (var index = 0; index < inventory.Count; index++)
            {
                var entry = inventory[index];
                if (!string.Equals(entry.ItemId, itemId, StringComparison.OrdinalIgnoreCase) || entry.Quality != quality) continue;
                inventory[index] = new FarmInventoryEntry(itemId, entry.Quantity + amount, quality);
                return;
            }
            inventory.Add(new FarmInventoryEntry(itemId, amount, quality));
        }

        private bool TryRemoveInventory(string itemId, int amount, FarmItemQuality quality)
        {
            if (amount <= 0) return true;
            if (GetInventoryQuantity(itemId, quality) < amount) return false;
            for (var index = inventory.Count - 1; index >= 0 && amount > 0; index--)
            {
                var entry = inventory[index];
                if (!string.Equals(entry.ItemId, itemId, StringComparison.OrdinalIgnoreCase) || entry.Quality != quality) continue;
                var removed = Mathf.Min(entry.Quantity, amount);
                var remaining = entry.Quantity - removed;
                amount -= removed;
                if (remaining <= 0) inventory.RemoveAt(index);
                else inventory[index] = new FarmInventoryEntry(entry.ItemId, remaining, entry.Quality);
            }
            return true;
        }

        private FarmCommandResult Fail(FarmCommandFailure failure, string message, int tileIndex = -1, TileState tile = null) => FarmCommandResult.Fail(failure, message, BuildInventorySnapshot(), tile == null ? null : Snapshot(tileIndex, tile));
        private FarmInventorySnapshot BuildInventorySnapshot() => new(inventory.Where(entry => entry.Quantity > 0).OrderBy(entry => entry.ItemId).ThenBy(entry => entry.Quality).Select(entry => new FarmInventoryEntry(entry.ItemId, entry.Quantity, entry.Quality)), hotbar, selectedHotbarIndex);
        private static FarmTileSnapshot Snapshot(int tileIndex, TileState tile) => new(tileIndex, tile != null && tile.IsPrepared, tile?.CropId, tile != null && tile.IsWatered, tile != null && tile.IsFertilized, tile?.LastHarvestedCropId, tile != null && tile.IsRotated);
    }

    public sealed class FarmAuthoritativeCore : MonoBehaviour
    {
        [SerializeField, Min(0)] private int mockLatencyMilliseconds = 250;
        [SerializeField] private string initialSeedItemId = FarmGameState.PumpkinSeedId;
        [SerializeField, Min(1)] private int initialSeedQuantity = 12;
        private IFarmBackend backend;
        private FarmGameState gameState;
        private bool commandInFlight;
        public bool IsInitialized => backend != null && gameState != null;
        public bool IsCommandInFlight => commandInFlight;
        public event Action<bool> CommandPendingChanged;

        public void Initialize(
            FarmGameState state,
            IEnumerable<int> tileIndexes,
            Func<FarmSeason> seasonProvider = null,
            Func<int, bool> greenhouseCoverageProvider = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            gameState = state;
            var mockBackend = new FarmMockBackend(
                tileIndexes,
                initialSeedItemId,
                initialSeedQuantity,
                state.SlotCapacity,
                mockLatencyMilliseconds,
                seasonProvider,
                () => state.GetMasteryLevel(FarmMasterySkill.Harvesting),
                () => state.GetMasteryLevel(FarmMasterySkill.Cultivation),
                () => FarmSessionTime.Now,
                greenhouseCoverageProvider);
            backend = mockBackend;
            mockBackend.SynchronizePrototypeInventory(state.Inventory.Select(stack =>
                new FarmInventoryEntry(stack.ItemId, stack.Quantity, stack.Quality)));
            ApplyConfirmedSnapshot(backend.GetInventoryAsync(CancellationToken.None).GetAwaiter().GetResult());
        }

        public Task<FarmCommandResult> PrepareSoilAsync(int tileIndex) => DispatchAsync(token => backend.PrepareSoilAsync(new FarmPrepareSoilRequest(Guid.NewGuid().ToString("N"), tileIndex), token));
        public Task<FarmCommandResult> PlantSeedAsync(int tileIndex, string seedItemId, string cropId) => DispatchAsync(token => backend.PlantSeedAsync(new FarmPlantSeedRequest(Guid.NewGuid().ToString("N"), tileIndex, seedItemId, cropId), token));
        public Task<FarmCommandResult> SetHotbarAsync(int hotbarIndex, string itemId) => DispatchAsync(token => backend.SetHotbarAsync(new FarmSetHotbarRequest(Guid.NewGuid().ToString("N"), hotbarIndex, itemId), token));
        public Task<FarmCommandResult> SelectHotbarAsync(int hotbarIndex) => DispatchAsync(token => backend.SelectHotbarAsync(new FarmSelectHotbarRequest(Guid.NewGuid().ToString("N"), hotbarIndex), token));
        public Task<FarmCommandResult> SwapHotbarAsync(int sourceIndex, int targetIndex) => DispatchAsync(token => backend.SwapHotbarAsync(new FarmSwapHotbarRequest(Guid.NewGuid().ToString("N"), sourceIndex, targetIndex), token));
        public Task<FarmCommandResult> WaterTileAsync(int tileIndex) => DispatchAsync(token => backend.WaterTileAsync(new FarmWaterTileRequest(Guid.NewGuid().ToString("N"), tileIndex), token));
        public Task<FarmCommandResult> FertilizeTileAsync(int tileIndex, string fertilizerItemId) => DispatchAsync(token => backend.FertilizeTileAsync(new FarmFertilizeTileRequest(Guid.NewGuid().ToString("N"), tileIndex, fertilizerItemId), token));
        public Task<FarmCommandResult> HarvestTileAsync(int tileIndex) => DispatchAsync(token => backend.HarvestTileAsync(new FarmHarvestTileRequest(Guid.NewGuid().ToString("N"), tileIndex), token));

        public void SynchronizePrototypeInventory()
        {
            if (!IsInitialized || backend is not FarmMockBackend mock) return;
            mock.SynchronizePrototypeInventory(gameState.Inventory.Select(stack =>
                new FarmInventoryEntry(stack.ItemId, stack.Quantity, stack.Quality)));
        }

        private async Task<FarmCommandResult> DispatchAsync(Func<CancellationToken, Task<FarmCommandResult>> request)
        {
            if (!IsInitialized) return FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.uninitialized", "Backend has not been initialized."));
            if (commandInFlight) return FarmCommandResult.Fail(FarmCommandFailure.RequestInProgress, FarmLocalization.Get("backend.waiting", "Waiting for server confirmation."));
            commandInFlight = true;
            CommandPendingChanged?.Invoke(true);
            try
            {
                var result = await request(CancellationToken.None);
                if (result != null && result.Succeeded && result.Inventory != null) ApplyConfirmedSnapshot(result.Inventory);
                return result ?? FarmCommandResult.Fail(FarmCommandFailure.InvalidRequest, FarmLocalization.Get("backend.no_response", "The backend did not respond."));
            }
            finally
            {
                commandInFlight = false;
                CommandPendingChanged?.Invoke(false);
            }
        }

        private void ApplyConfirmedSnapshot(FarmInventorySnapshot snapshot)
        {
            if (snapshot == null || gameState == null) return;
            gameState.ApplyAuthoritativeInventorySnapshot(snapshot.Items, snapshot.Hotbar, snapshot.SelectedHotbarIndex);
        }
    }
}
