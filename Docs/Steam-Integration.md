# Steam Integration (Meta 9)

The project uses the official Steamworks.NET Unity package. `FarmSteamSession`
owns Steam initialization, friends-only lobbies, join requests, presence, and
friend invitations. `FarmSteamP2PTransport` uses reliable
`SteamNetworkingMessages` channel 13 and implements the transport-neutral
`IFarmSessionTransport` interface.

When a lobby is created or joined, the lobby owner is host authority. The host
receives peer intents and broadcasts confirmed snapshots; a peer never performs
shared farm simulation locally. P2P session requests are accepted only from
members currently in that lobby.

## Required live test

Set the real App ID in `Assets/Resources/Steam/FarmSteamSettings.asset`, launch
two Steam accounts through Steam, and verify create/join/invite, field action
replication, sleep voting, reconnect, and host disconnection. App ID `0` is the
safe default for Editor work; do not replace it with a fabricated production ID.

The Steam API is intentionally an edge dependency. Gameplay only knows the
generic co-op protocol and can still use the loopback transport in local tests.
