using System;
using Steamworks;
using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Steam-only session edge. Gameplay never references Steam types: it keeps
    /// using FarmSessionIntent and FarmWorldSessionSnapshot through a transport
    /// adapter that will be added after lobby validation.
    /// </summary>
    public sealed class FarmSteamSession : MonoBehaviour
    {
        private FarmSteamSettings settings;
        private CallResult<LobbyCreated_t> lobbyCreated;
        private CallResult<LobbyEnter_t> lobbyEntered;
        private Callback<GameLobbyJoinRequested_t> joinRequested;
        private bool initialized;

        public bool IsAvailable => initialized;
        public string Status { get; private set; } = "Steam not configured.";
        public CSteamID ActiveLobby { get; private set; } = CSteamID.Nil;
        public event Action<CSteamID> LobbyJoined;
        public event Action<string> SessionError;

        private void Awake()
        {
            settings = Resources.Load<FarmSteamSettings>("Steam/FarmSteamSettings");
            lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            lobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
            joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        }

        private void Start()
        {
            if (settings == null || settings.AppId == 0)
            {
                Status = "Steam App ID not configured.";
                return;
            }
            try
            {
                initialized = SteamAPI.Init();
                Status = initialized ? "Steam online." : "Steam API initialization failed.";
                if (!initialized) SessionError?.Invoke(Status);
            }
            catch (Exception exception)
            {
                initialized = false;
                Status = "Steam API initialization failed.";
                Debug.LogWarning($"Steam initialization unavailable: {exception.Message}");
                SessionError?.Invoke(Status);
            }
        }

        private void Update()
        {
            if (initialized) SteamAPI.RunCallbacks();
        }

        private void OnDestroy()
        {
            if (initialized) SteamAPI.Shutdown();
        }

        public bool CreateFriendsLobby()
        {
            if (!initialized) return Fail("Steam is unavailable. Configure a valid App ID and launch through Steam.");
            lobbyCreated.Set(SteamMatchmaking.CreateLobby(settings.LobbyType, Mathf.Clamp(settings.MaxMembers, 1, 4)));
            Status = "Creating Steam lobby...";
            return true;
        }

        public bool JoinLobby(CSteamID lobby)
        {
            if (!initialized) return Fail("Steam is unavailable. Configure a valid App ID and launch through Steam.");
            if (lobby == CSteamID.Nil) return Fail("Invalid Steam lobby.");
            lobbyEntered.Set(SteamMatchmaking.JoinLobby(lobby));
            Status = "Joining Steam lobby...";
            return true;
        }

        public void LeaveLobby()
        {
            if (ActiveLobby != CSteamID.Nil) SteamMatchmaking.LeaveLobby(ActiveLobby);
            ActiveLobby = CSteamID.Nil;
            if (initialized)
            {
                SteamFriends.SetRichPresence("steam_player_group", string.Empty);
                SteamFriends.SetRichPresence("status", "In menus");
            }
            Status = initialized ? "Steam online." : "Steam offline.";
        }

        public bool InviteFriend(CSteamID friend)
        {
            if (!initialized || ActiveLobby == CSteamID.Nil) return Fail("Create or join a Steam lobby before inviting a friend.");
            return SteamFriends.InviteUserToGame(friend, $"+connect_lobby {ActiveLobby.m_SteamID}");
        }

        private void OnLobbyCreated(LobbyCreated_t callback, bool ioFailure)
        {
            if (ioFailure || callback.m_eResult != EResult.k_EResultOK)
            {
                Fail("Steam could not create the lobby.");
                return;
            }
            ActiveLobby = new CSteamID(callback.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyData(ActiveLobby, "farm_protocol", FarmWorldSessionSnapshot.ProtocolVersion.ToString());
            SteamMatchmaking.SetLobbyJoinable(ActiveLobby, true);
            SteamFriends.SetRichPresence("steam_player_group", ActiveLobby.m_SteamID.ToString());
            SteamFriends.SetRichPresence("status", "Hosting a farm");
            Status = "Steam lobby created.";
            LobbyJoined?.Invoke(ActiveLobby);
        }

        private void OnLobbyEntered(LobbyEnter_t callback, bool ioFailure)
        {
            if (ioFailure || callback.m_EChatRoomEnterResponse != 1)
            {
                Fail("Steam could not join the lobby.");
                return;
            }
            ActiveLobby = new CSteamID(callback.m_ulSteamIDLobby);
            SteamFriends.SetRichPresence("steam_player_group", ActiveLobby.m_SteamID.ToString());
            SteamFriends.SetRichPresence("status", "Working on a farm");
            Status = "Steam lobby joined.";
            LobbyJoined?.Invoke(ActiveLobby);
        }

        private void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback) => JoinLobby(callback.m_steamIDLobby);

        private bool Fail(string error)
        {
            Status = error;
            SessionError?.Invoke(error);
            return false;
        }
    }
}
