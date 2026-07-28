# Co-op Transport Foundation (Meta 7)

`FarmSessionEnvelope` is the sole wire payload: it carries either a player
intent or a host snapshot and is serialized before delivery. `FarmSessionCoordinator`
binds gameplay to `IFarmSessionTransport`; gameplay therefore does not know
whether the transport is local loopback or Steam P2P.

The host routes peer intents through `FarmHostIntentRouter`. It rejects invalid
or duplicate commands, validates tool payloads, and asks the existing mock
authoritative core to apply field actions. The Steam transport overwrites the
envelope sender with the actual Steam peer identity; the coordinator rejects an
intent whose embedded requester does not match it. Peers render snapshots only
from the lobby owner. The shared farm inventory remains host-owned; visual
hotbar choice is deliberately local per player and is not treated as a global
mutation. Sleep participants are added by the authenticated transport path and
their existing votes are preserved when another player joins.

`FarmLoopbackSessionTransport` supports one host plus up to three peers and
serializes messages in memory. It is a deterministic editor harness, not a
shipping transport. The smoke check verifies a peer intent arrives at the host
as an independent deserialized object.

Open item before Meta 7 can be signed off: exercise this path in two launched
game instances once the Steam P2P adapter is attached, including reconnect and
host-disconnect behavior.
