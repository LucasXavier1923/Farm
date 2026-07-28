using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Compact, transport-neutral envelope. Only this type crosses a network
    /// boundary; Steam is an implementation detail rather than gameplay state.
    /// </summary>
    [Serializable]
    public sealed class FarmSessionEnvelope
    {
        public const int ProtocolVersion = 1;

        public int Protocol = ProtocolVersion;
        public string MessageId;
        public string SenderId;
        public string Channel;
        public string Payload;

        public static FarmSessionEnvelope ForIntent(string senderId, FarmSessionIntent intent) => new()
        {
            MessageId = intent?.IntentId ?? Guid.NewGuid().ToString("N"),
            SenderId = senderId ?? string.Empty,
            Channel = "intent",
            Payload = intent == null ? string.Empty : JsonUtility.ToJson(intent)
        };

        public static FarmSessionEnvelope ForSnapshot(string senderId, FarmWorldSessionSnapshot snapshot) => new()
        {
            MessageId = $"snapshot-{snapshot?.Revision ?? 0}",
            SenderId = senderId ?? string.Empty,
            Channel = "snapshot",
            Payload = snapshot == null ? string.Empty : JsonUtility.ToJson(snapshot)
        };

        public bool TryReadIntent(out FarmSessionIntent intent)
        {
            intent = null;
            if (Protocol != ProtocolVersion || !string.Equals(Channel, "intent", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(Payload)) return false;
            intent = JsonUtility.FromJson<FarmSessionIntent>(Payload);
            return intent != null && !string.IsNullOrWhiteSpace(intent.IntentId) && !string.IsNullOrWhiteSpace(intent.RequestedBy);
        }

        public bool TryReadSnapshot(out FarmWorldSessionSnapshot snapshot)
        {
            snapshot = null;
            if (Protocol != ProtocolVersion || !string.Equals(Channel, "snapshot", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(Payload)) return false;
            snapshot = JsonUtility.FromJson<FarmWorldSessionSnapshot>(Payload);
            return snapshot != null && snapshot.IsValid;
        }
    }

    /// <summary>Implemented by loopback and Steam P2P transports.</summary>
    public interface IFarmSessionTransport
    {
        string LocalPlayerId { get; }
        bool IsConnected { get; }
        event Action<FarmSessionEnvelope> ReceivedByHost;
        event Action<FarmSessionEnvelope> ReceivedByPeer;
        void SendIntentToHost(FarmSessionIntent intent);
        void BroadcastSnapshot(FarmWorldSessionSnapshot snapshot);
    }

    /// <summary>
    /// In-memory 1-4 player harness. It serializes every payload before delivery,
    /// so tests catch missing [Serializable] members without Steam or two builds.
    /// It is intentionally deterministic and must never ship as production P2P.
    /// </summary>
    public sealed class FarmLoopbackSessionTransport : IFarmSessionTransport
    {
        private readonly List<FarmLoopbackSessionTransport> peers = new();
        private FarmLoopbackSessionTransport host;

        public string LocalPlayerId { get; }
        public bool IsConnected => host != null || peers.Count > 0;
        public event Action<FarmSessionEnvelope> ReceivedByHost;
        public event Action<FarmSessionEnvelope> ReceivedByPeer;

        private FarmLoopbackSessionTransport(string localPlayerId) => LocalPlayerId = localPlayerId;

        public static FarmLoopbackSessionTransport CreateHost(string playerId = "host") =>
            new(string.IsNullOrWhiteSpace(playerId) ? "host" : playerId.Trim());

        public FarmLoopbackSessionTransport AddPeer(string playerId)
        {
            if (host != null) throw new InvalidOperationException("Only the host may add loopback peers.");
            if (peers.Count >= 3) throw new InvalidOperationException("A farm session supports at most four players.");
            var peer = new FarmLoopbackSessionTransport(string.IsNullOrWhiteSpace(playerId) ? $"peer-{peers.Count + 1}" : playerId.Trim()) { host = this };
            peers.Add(peer);
            return peer;
        }

        public void SendIntentToHost(FarmSessionIntent intent)
        {
            if (intent == null) return;
            var destination = host ?? this;
            destination.ReceivedByHost?.Invoke(Clone(FarmSessionEnvelope.ForIntent(LocalPlayerId, intent)));
        }

        public void BroadcastSnapshot(FarmWorldSessionSnapshot snapshot)
        {
            if (snapshot == null || host != null) return;
            var envelope = FarmSessionEnvelope.ForSnapshot(LocalPlayerId, snapshot);
            foreach (var peer in peers) peer.ReceivedByPeer?.Invoke(Clone(envelope));
        }

        private static FarmSessionEnvelope Clone(FarmSessionEnvelope envelope) =>
            JsonUtility.FromJson<FarmSessionEnvelope>(JsonUtility.ToJson(envelope));
    }
}
