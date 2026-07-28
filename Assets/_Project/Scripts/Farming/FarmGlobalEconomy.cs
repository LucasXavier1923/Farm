using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmPrototype.Farming
{
    [Serializable]
    public sealed class FarmAccountSnapshot
    {
        public string AccountId;
        public int Revision;
        public int Wallet;
        public List<InventoryStack> Inventory = new();
    }

    [Serializable]
    public sealed class FarmMarketOrder
    {
        public string OrderId;
        public string SellerAccountId;
        public string ItemId;
        public int RemainingQuantity;
        public int UnitPrice;
        public string Status;
    }

    [Serializable]
    public sealed class FarmCreateMarketOrderRequest
    {
        public string RequestId;
        public string ItemId;
        public int Quantity;
        public int UnitPrice;
    }

    [Serializable]
    public sealed class FarmBuyMarketOrderRequest
    {
        public string RequestId;
        public string OrderId;
        public int Quantity;
    }

    [Serializable]
    public sealed class FarmEconomyResponse
    {
        public bool Succeeded;
        public string Message;
        public FarmAccountSnapshot Account;
        public FarmMarketOrder Order;
    }

    /// <summary>
    /// The only boundary UI/gameplay may use for sensitive global-economy data.
    /// Implementations must treat every request as a server-validated command.
    /// </summary>
    public interface IFarmGlobalEconomyService
    {
        Task<FarmEconomyResponse> GetAccountAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FarmMarketOrder>> GetActiveOrdersAsync(CancellationToken cancellationToken = default);
        Task<FarmEconomyResponse> CreateOrderAsync(FarmCreateMarketOrderRequest request, CancellationToken cancellationToken = default);
        Task<FarmEconomyResponse> BuyOrderAsync(FarmBuyMarketOrderRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>Development-only server simulation with latency and idempotent request IDs.</summary>
    public sealed class FarmMockGlobalEconomyService : IFarmGlobalEconomyService
    {
        private readonly Dictionary<string, FarmEconomyResponse> completed = new(StringComparer.Ordinal);
        private readonly List<FarmMarketOrder> orders = new();
        private readonly FarmAccountSnapshot account = new()
        {
            AccountId = "mock-local", Wallet = 500, Revision = 1,
            Inventory = new List<InventoryStack> { new("pumpkin", 10) }
        };
        private readonly SemaphoreSlim gate = new(1, 1);

        public FarmMockGlobalEconomyService()
        {
            orders.Add(new FarmMarketOrder
            {
                OrderId = "mock-seed-order", SellerAccountId = "mock-farmer", ItemId = "corn_seed",
                RemainingQuantity = 5, UnitPrice = 20, Status = "active"
            });
        }

        public async Task<FarmEconomyResponse> GetAccountAsync(CancellationToken cancellationToken = default)
        {
            await SimulateNetworkAsync(cancellationToken);
            return Success("Account loaded.", account: CloneAccount());
        }

        public async Task<IReadOnlyList<FarmMarketOrder>> GetActiveOrdersAsync(CancellationToken cancellationToken = default)
        {
            await SimulateNetworkAsync(cancellationToken);
            var copy = new List<FarmMarketOrder>();
            foreach (var order in orders)
                if (string.Equals(order.Status, "active", StringComparison.Ordinal)) copy.Add(CloneOrder(order));
            return copy;
        }

        public async Task<FarmEconomyResponse> CreateOrderAsync(FarmCreateMarketOrderRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsValid(request?.RequestId) || !IsValid(request.ItemId) || request.Quantity < 1 || request.UnitPrice < 1)
                return Failure("Invalid market listing request.");
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (completed.TryGetValue(request.RequestId, out var cached)) return CloneResponse(cached);
                await SimulateNetworkAsync(cancellationToken);
                var inventory = account.Inventory.Find(stack => string.Equals(stack.ItemId, request.ItemId, StringComparison.Ordinal));
                if (inventory == null || inventory.Quantity < request.Quantity) return Failure("Insufficient inventory for this listing.");
                inventory.Quantity -= request.Quantity;
                account.Revision++;
                var order = new FarmMarketOrder
                {
                    OrderId = Guid.NewGuid().ToString("N"), SellerAccountId = account.AccountId,
                    ItemId = request.ItemId, RemainingQuantity = request.Quantity,
                    UnitPrice = request.UnitPrice, Status = "active"
                };
                orders.Add(order);
                var result = Success("Market listing created.", order: CloneOrder(order), account: CloneAccount());
                completed[request.RequestId] = result;
                return CloneResponse(result);
            }
            finally { gate.Release(); }
        }

        public async Task<FarmEconomyResponse> BuyOrderAsync(FarmBuyMarketOrderRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsValid(request?.RequestId) || !IsValid(request.OrderId) || request.Quantity < 1) return Failure("Invalid market purchase request.");
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (completed.TryGetValue(request.RequestId, out var cached)) return CloneResponse(cached);
                await SimulateNetworkAsync(cancellationToken);
                var order = orders.Find(entry => entry.OrderId == request.OrderId && entry.Status == "active");
                if (order == null || order.RemainingQuantity < request.Quantity) return Failure("This market order is no longer available.");
                if (string.Equals(order.SellerAccountId, account.AccountId, StringComparison.Ordinal)) return Failure("You cannot buy your own listing.");
                var cost = checked(order.UnitPrice * request.Quantity);
                if (account.Wallet < cost) return Failure("Insufficient funds.");
                account.Wallet -= cost;
                account.Revision++;
                var inventory = account.Inventory.Find(stack => string.Equals(stack.ItemId, order.ItemId, StringComparison.Ordinal));
                if (inventory == null) account.Inventory.Add(new InventoryStack(order.ItemId, request.Quantity));
                else inventory.Quantity += request.Quantity;
                order.RemainingQuantity -= request.Quantity;
                if (order.RemainingQuantity == 0) order.Status = "fulfilled";
                var result = Success("Market purchase confirmed.", CloneOrder(order), CloneAccount());
                completed[request.RequestId] = result;
                return CloneResponse(result);
            }
            finally { gate.Release(); }
        }

        private static async Task SimulateNetworkAsync(CancellationToken cancellationToken) => await Task.Delay(90, cancellationToken);
        private static bool IsValid(string value) => !string.IsNullOrWhiteSpace(value);
        private static FarmEconomyResponse Success(string message, FarmMarketOrder order = null, FarmAccountSnapshot account = null) => new() { Succeeded = true, Message = message, Order = order, Account = account };
        private static FarmEconomyResponse Failure(string message) => new() { Succeeded = false, Message = message };
        private FarmAccountSnapshot CloneAccount() => new() { AccountId = account.AccountId, Wallet = account.Wallet, Revision = account.Revision, Inventory = new List<InventoryStack>(account.Inventory) };
        private static FarmMarketOrder CloneOrder(FarmMarketOrder source) => source == null ? null : new() { OrderId = source.OrderId, SellerAccountId = source.SellerAccountId, ItemId = source.ItemId, RemainingQuantity = source.RemainingQuantity, UnitPrice = source.UnitPrice, Status = source.Status };
        private static FarmEconomyResponse CloneResponse(FarmEconomyResponse source) => new() { Succeeded = source.Succeeded, Message = source.Message, Order = CloneOrder(source.Order), Account = source.Account == null ? null : new() { AccountId = source.Account.AccountId, Wallet = source.Account.Wallet, Revision = source.Account.Revision, Inventory = new List<InventoryStack>(source.Account.Inventory) } };
    }

    [CreateAssetMenu(menuName = "Farm/Supabase Settings", fileName = "FarmSupabaseSettings")]
    public sealed class FarmSupabaseSettings : ScriptableObject
    {
        [Tooltip("Example: https://project-ref.supabase.co. This is safe to include in a client build.")]
        public string ProjectUrl;
        [Tooltip("Supabase publishable/anon key only. Never enter a secret or service_role key here.")]
        public string PublishableKey;
        public string EconomyFunction = "farm-economy";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ProjectUrl) && !string.IsNullOrWhiteSpace(PublishableKey) && !string.IsNullOrWhiteSpace(EconomyFunction);
    }

    /// <summary>Application-owned auth seam. A Steam ticket-to-Supabase login flow plugs in here later.</summary>
    public interface IFarmAccessTokenProvider { Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default); }

    /// <summary>
    /// Production client. It only calls the Edge Function using a signed user JWT;
    /// it never receives a Supabase secret key and cannot write database rows directly.
    /// </summary>
    public sealed class FarmSupabaseEconomyService : IFarmGlobalEconomyService
    {
        [Serializable] private sealed class OperationRequest { public string Operation; public string Payload; }
        [Serializable] private sealed class OrdersResponse { public List<FarmMarketOrder> Orders = new(); }
        private readonly FarmSupabaseSettings settings;
        private readonly IFarmAccessTokenProvider tokens;

        public FarmSupabaseEconomyService(FarmSupabaseSettings settings, IFarmAccessTokenProvider tokens) { this.settings = settings; this.tokens = tokens; }
        public Task<FarmEconomyResponse> GetAccountAsync(CancellationToken cancellationToken = default) => SendAsync<FarmEconomyResponse>("account.get", string.Empty, cancellationToken);
        public async Task<IReadOnlyList<FarmMarketOrder>> GetActiveOrdersAsync(CancellationToken cancellationToken = default) => (await SendAsync<OrdersResponse>("market.list", string.Empty, cancellationToken)).Orders ?? new List<FarmMarketOrder>();
        public Task<FarmEconomyResponse> CreateOrderAsync(FarmCreateMarketOrderRequest request, CancellationToken cancellationToken = default) => SendAsync<FarmEconomyResponse>("market.create", JsonUtility.ToJson(request), cancellationToken);
        public Task<FarmEconomyResponse> BuyOrderAsync(FarmBuyMarketOrderRequest request, CancellationToken cancellationToken = default) => SendAsync<FarmEconomyResponse>("market.buy", JsonUtility.ToJson(request), cancellationToken);

        private async Task<T> SendAsync<T>(string operation, string payload, CancellationToken cancellationToken) where T : class, new()
        {
            if (settings == null || !settings.IsConfigured) throw new InvalidOperationException("Supabase settings are not configured.");
            if (tokens == null) throw new InvalidOperationException("A signed-in Supabase access token is required.");
            var token = await tokens.GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("A signed-in Supabase access token is required.");
            var url = settings.ProjectUrl.TrimEnd('/') + "/functions/v1/" + settings.EconomyFunction.Trim();
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new OperationRequest { Operation = operation, Payload = payload ?? string.Empty })));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", settings.PublishableKey.Trim());
            request.SetRequestHeader("Authorization", "Bearer " + token.Trim());
            var operationHandle = request.SendWebRequest();
            while (!operationHandle.isDone) { cancellationToken.ThrowIfCancellationRequested(); await Task.Yield(); }
            if (request.result != UnityWebRequest.Result.Success) throw new InvalidOperationException($"Economy request failed ({request.responseCode}): {request.error}");
            var result = JsonUtility.FromJson<T>(request.downloadHandler.text);
            if (result == null) throw new InvalidOperationException("Economy response was invalid.");
            return result;
        }
    }
}
