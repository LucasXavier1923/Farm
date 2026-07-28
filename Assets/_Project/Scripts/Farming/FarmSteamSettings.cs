using Steamworks;
using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Per-build Steam configuration. App ID zero intentionally keeps the Steam
    /// integration offline, which makes editor and non-Steam runs safe.
    /// </summary>
    [CreateAssetMenu(menuName = "Farm/Steam Settings", fileName = "FarmSteamSettings")]
    public sealed class FarmSteamSettings : ScriptableObject
    {
        [Tooltip("Set this to the Steamworks App ID assigned to this game. Zero keeps Steam offline.")]
        public uint AppId;
        [Range(1, 4)] public int MaxMembers = 4;
        public ELobbyType LobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;
    }
}
