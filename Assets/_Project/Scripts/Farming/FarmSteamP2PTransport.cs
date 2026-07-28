using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Steamworks;
using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Reliable SteamNetworkingMessages adapter for the shared farm protocol.
    /// Lobby management lives in FarmSteamSession; this class only transports
    /// serialized intent/snapshot envelopes once a lobby is joined.
    /// </summary>
    public sealed class FarmSteamP2PTransport : MonoBehaviour, IFarmSessionTransport
    {
        private const int Channel = 13;
        private readonly IntPtr[] receiveBuffer = new IntPtr[16];
        private FarmSteamSession steamSession;
        private FarmSessionCoordinator coordinator;
        private Callback<SteamNetworkingMessagesSessionRequest_t> sessionRequest;
        private bool isHost;

        public string LocalPlayerId => steamSession != null && steamSession.IsAvailable
            ? SteamUser.GetSteamID().m_SteamID.ToString() : string.Empty;
        public bool IsConnected => steamSession != null && steamSession.IsAvailable && steamSession.ActiveLobby != CSteamID.Nil;
        public event Action<FarmSessionEnvelope> ReceivedByHost;
        public event Action<FarmSessionEnvelope> ReceivedByPeer;

        private void Awake()
        {
            steamSession = GetComponent<FarmSteamSession>();
            coordinator = GetComponent<FarmSessionCoordinator>();
            sessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        }

        private void Start()
        {
            if (steamSession == null) return;
            steamSession.LobbyJoined += ConfigureJoinedLobby;
            if (steamSession.ActiveLobby != CSteamID.Nil) ConfigureJoinedLobby(steamSession.ActiveLobby);
        }

        private void OnDestroy()
        {
            if (steamSession != null) steamSession.LobbyJoined -= ConfigureJoinedLobby;
            coordinator?.Unbind();
        }

        private void Update()
        {
            if (IsConnected) ReceivePendingMessages();
        }

        public void SendIntentToHost(FarmSessionIntent intent)
        {
            if (!IsConnected || intent == null || isHost) return;
            SendTo(SteamMatchmaking.GetLobbyOwner(steamSession.ActiveLobby), FarmSessionEnvelope.ForIntent(LocalPlayerId, intent));
        }

        public void BroadcastSnapshot(FarmWorldSessionSnapshot snapshot)
        {
            if (!IsConnected || !isHost || snapshot == null) return;
            var envelope = FarmSessionEnvelope.ForSnapshot(LocalPlayerId, snapshot);
            var members = SteamMatchmaking.GetNumLobbyMembers(steamSession.ActiveLobby);
            var localSteamId = SteamUser.GetSteamID();
            for (var index = 0; index < members; index++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(steamSession.ActiveLobby, index);
                if (member != localSteamId) SendTo(member, envelope);
            }
        }

        private void ConfigureJoinedLobby(CSteamID lobby)
        {
            if (lobby == CSteamID.Nil || steamSession == null || !steamSession.IsAvailable) return;
            isHost = SteamMatchmaking.GetLobbyOwner(lobby) == SteamUser.GetSteamID();
            FarmSessionTime.Configure(() => Time.unscaledTime, () => Time.unscaledDeltaTime, isHost);
            coordinator?.Bind(this);
        }

        private void SendTo(CSteamID recipient, FarmSessionEnvelope envelope)
        {
            if (recipient == CSteamID.Nil || envelope == null) return;
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope));
            var pointer = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, pointer, bytes.Length);
                var identity = new SteamNetworkingIdentity();
                identity.SetSteamID(recipient);
                var result = SteamNetworkingMessages.SendMessageToUser(
                    ref identity, pointer, (uint)bytes.Length,
                    Constants.k_nSteamNetworkingSend_Reliable | Constants.k_nSteamNetworkingSend_AutoRestartBrokenSession,
                    Channel);
                if (result != EResult.k_EResultOK) Debug.LogWarning($"Steam P2P send failed: {result}.");
            }
            finally { Marshal.FreeHGlobal(pointer); }
        }

        private void ReceivePendingMessages()
        {
            var count = SteamNetworkingMessages.ReceiveMessagesOnChannel(Channel, receiveBuffer, receiveBuffer.Length);
            for (var index = 0; index < count; index++)
            {
                var pointer = receiveBuffer[index];
                try
                {
                    var message = SteamNetworkingMessage_t.FromIntPtr(pointer);
                    if (message.m_cbSize <= 0 || message.m_cbSize > 262144) continue;
                    var actualSender = message.m_identityPeer.GetSteamID();
                    if (!IsLobbyMember(actualSender)) continue;
                    var bytes = new byte[message.m_cbSize];
                    Marshal.Copy(message.m_pData, bytes, 0, bytes.Length);
                    var envelope = JsonUtility.FromJson<FarmSessionEnvelope>(Encoding.UTF8.GetString(bytes));
                    if (envelope == null || envelope.Protocol != FarmSessionEnvelope.ProtocolVersion) continue;
                    envelope.SenderId = actualSender.m_SteamID.ToString();
                    if (isHost) ReceivedByHost?.Invoke(envelope);
                    else if (actualSender == SteamMatchmaking.GetLobbyOwner(steamSession.ActiveLobby)) ReceivedByPeer?.Invoke(envelope);
                }
                catch (Exception exception) { Debug.LogWarning($"Steam P2P message rejected: {exception.Message}"); }
                finally
                {
                    if (pointer != IntPtr.Zero) SteamNetworkingMessage_t.Release(pointer);
                    receiveBuffer[index] = IntPtr.Zero;
                }
            }
        }

        private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t callback)
        {
            if (!IsConnected || !IsLobbyMember(callback.m_identityRemote.GetSteamID())) return;
            var identity = callback.m_identityRemote;
            SteamNetworkingMessages.AcceptSessionWithUser(ref identity);
        }

        private bool IsLobbyMember(CSteamID steamId)
        {
            var members = SteamMatchmaking.GetNumLobbyMembers(steamSession.ActiveLobby);
            for (var index = 0; index < members; index++)
                if (SteamMatchmaking.GetLobbyMemberByIndex(steamSession.ActiveLobby, index) == steamId) return true;
            return false;
        }
    }
}
