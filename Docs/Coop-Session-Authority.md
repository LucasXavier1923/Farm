# Host-led Co-op Session

Farm supports one host and up to three guests. The host owns the local shared
save, advances time, simulates weather and crops, and broadcasts farm snapshots.
Guests submit gameplay intents; their UI, camera and menus remain local.

`FarmSessionCoordinator`, `FarmSessionIntent`, and
`FarmWorldSessionSnapshot` form the transport-neutral co-op seam. The included
loopback transport validates host/guest routing without online services. A
future friend-connection adapter may bind to this seam, but it must not add a
backend, global persistence, auction house, or remote economy.

## Required session rules

- Maximum four connected players.
- The world clock never pauses for one player's interface.
- Only the host writes the local farm save.
- Host snapshots own shared inventory, tiles, weather, buildings and sleep
  readiness; guests own camera and UI preferences.
- A host ending a session ends that session; host migration is out of scope.
