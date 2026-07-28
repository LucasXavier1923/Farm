using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmCommerceCommandType
    {
        BuySeedPack,
        SellAllCrops
    }

    [Serializable]
    public sealed class FarmCommerceRequest
    {
        public string CommandId;
        public string RequestedBy;
        public FarmCommerceCommandType Type;
        public string CropId;

        public static FarmCommerceRequest BuySeedPack(string requestedBy, string cropId) => new()
        {
            CommandId = Guid.NewGuid().ToString("N"),
            RequestedBy = requestedBy,
            Type = FarmCommerceCommandType.BuySeedPack,
            CropId = cropId
        };

        public static FarmCommerceRequest SellAllCrops(string requestedBy) => new()
        {
            CommandId = Guid.NewGuid().ToString("N"),
            RequestedBy = requestedBy,
            Type = FarmCommerceCommandType.SellAllCrops
        };
    }

    public readonly struct FarmCommerceResult
    {
        public readonly bool Succeeded;
        public readonly bool FromCache;
        public readonly string Message;
        public readonly int Quantity;
        public readonly int MoneyDelta;

        public FarmCommerceResult(bool succeeded, bool fromCache, string message, int quantity, int moneyDelta)
        {
            Succeeded = succeeded;
            FromCache = fromCache;
            Message = message;
            Quantity = quantity;
            MoneyDelta = moneyDelta;
        }
    }

    /// <summary>
    /// Local commerce boundary for the shared farm. A future Steam host adapter
    /// can receive the same requests and return the confirmed result, preserving
    /// idempotency through CommandId.
    /// </summary>
    public sealed class FarmSessionCommerce : MonoBehaviour
    {
        private const int CommandCacheLimit = 64;
        private readonly Dictionary<string, FarmCommerceResult> completedCommands = new(StringComparer.Ordinal);
        private FarmGameState state;
        private bool executing;

        public bool IsExecuting => executing;

        public void Initialize(FarmGameState gameState)
        {
            state = gameState;
            completedCommands.Clear();
            executing = false;
        }

        public FarmCommerceResult Execute(FarmCommerceRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CommandId))
                return Failure(FarmLocalization.Get("commerce.request.invalid", "Invalid commerce request."));
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(
                    FarmSessionIntentKind.Commerce,
                    request.RequestedBy,
                    JsonUtility.ToJson(request));
                return Failure(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."));
            }
            if (completedCommands.TryGetValue(request.CommandId, out var cached))
                return new FarmCommerceResult(cached.Succeeded, true, cached.Message, cached.Quantity, cached.MoneyDelta);
            if (state == null)
                return Failure(FarmLocalization.Get("commerce.unavailable", "Commerce unavailable: session is not initialized."));
            if (executing)
                return Failure(FarmLocalization.Get("commerce.busy", "Commerce is already being processed."));

            executing = true;
            try
            {
                var result = request.Type switch
                {
                    FarmCommerceCommandType.BuySeedPack => ExecuteBuySeedPack(request),
                    FarmCommerceCommandType.SellAllCrops => ExecuteSellAllCrops(),
                    _ => Failure(FarmLocalization.Get("commerce.action.unknown", "Unknown commerce action."))
                };
                Cache(request.CommandId, result);
                return result;
            }
            finally { executing = false; }
        }

        private FarmCommerceResult ExecuteBuySeedPack(FarmCommerceRequest request)
        {
            var crop = FarmContentDatabase.GetCrop(request.CropId);
            if (crop == null || crop.SeedItem == null)
                return Failure(FarmLocalization.Get("commerce.seed.unknown", "Selected seed does not exist."));
            if (!state.TryBuySeedPack(crop, out var amount, out var cost))
                return Failure(FarmLocalization.Format("commerce.buy.unavailable", "Purchase unavailable. Cost: ${0}; check funds and space.", FarmEconomyRules.SeedPackPrice(crop)));

            return new FarmCommerceResult(
                true,
                false,
                FarmLocalization.Format("commerce.buy.success", "Bought {0} {1} for ${2}.", amount, crop.SeedItem.LocalizedName, cost),
                amount,
                -cost);
        }

        private FarmCommerceResult ExecuteSellAllCrops()
        {
            if (!state.TrySellAllCrops(out var quantity, out var earned))
                return Failure(FarmLocalization.Get("commerce.sell.none", "There are no harvested products in the inventory to sell."));
            return new FarmCommerceResult(
                true,
                false,
                FarmLocalization.Format("commerce.sell.success", "Sold {0} product(s) for ${1} at today's prices.", quantity, earned),
                quantity,
                earned);
        }

        private static FarmCommerceResult Failure(string message) => new(false, false, message, 0, 0);

        private void Cache(string commandId, FarmCommerceResult result)
        {
            if (completedCommands.Count >= CommandCacheLimit)
            {
                using var enumerator = completedCommands.GetEnumerator();
                if (enumerator.MoveNext()) completedCommands.Remove(enumerator.Current.Key);
            }
            completedCommands[commandId] = result;
        }
    }
}
