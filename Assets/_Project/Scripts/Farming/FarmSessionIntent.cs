using System;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmSessionIntentKind
    {
        ToolAction,
        HotbarSelection,
        HotbarAssignment,
        StorageTransfer,
        InventoryOrganize,
        Commerce,
        Progression,
        SleepReadiness,
        AnimalCare,
        Production,
        Consumption,
        CommunityGift
        ,FestivalContribution
        ,CommunityProject
        ,Fishing
        ,DailyOrder
        ,Stewardship
        ,CollectionMilestone
        ,PlayerRole
        ,ForecastPlan
        ,RestRecovery
    }

    /// <summary>
    /// Transport-neutral request created by a PEER before any shared state changes.
    /// Payload is deliberately opaque to gameplay; the future Steam adapter owns
    /// serialization and forwards it to the host's matching command handler.
    /// </summary>
    [Serializable]
    public sealed class FarmSessionIntent
    {
        public string IntentId;
        public string RequestedBy;
        public FarmSessionIntentKind Kind;
        public string Payload;
        public float RequestedAt;

        public static FarmSessionIntent Create(FarmSessionIntentKind kind, string requestedBy, string payload) => new()
        {
            IntentId = Guid.NewGuid().ToString("N"),
            RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? "local" : requestedBy.Trim(),
            Kind = kind,
            Payload = payload ?? string.Empty,
            RequestedAt = FarmSessionTime.Now
        };
    }

    /// <summary>
    /// The peer-to-host seam. It has no transport implementation: a future Steam
    /// adapter subscribes to <see cref="Requested"/> and sends the intent unchanged.
    /// </summary>
    public static class FarmSessionIntentBus
    {
        public static event Action<FarmSessionIntent> Requested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewRuntimeSession() => Requested = null;

        public static FarmSessionIntent Raise(FarmSessionIntentKind kind, string requestedBy, string payload)
        {
            if (FarmSessionTime.Role != FarmSessionRole.Peer) return null;
            var intent = FarmSessionIntent.Create(kind, requestedBy, payload);
            var listeners = Requested;
            if (listeners == null) return intent;
            foreach (Action<FarmSessionIntent> listener in listeners.GetInvocationList())
            {
                try { listener.Invoke(intent); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
            return intent;
        }
    }
}
