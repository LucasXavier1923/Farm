# Cooperative Foundation Readiness

Updated: 26 July 2026

This document records the current non-network foundation for a future Steam P2P
farm session. It does not implement matchmaking, transport, player spawning, or
replication. Those remain a later integration layer.

## Verified foundations

| Area | Current boundary | Runtime evidence |
| --- | --- | --- |
| Shared time | `FarmSessionTime` uses unscaled solo time or an injected host clock. Only the simulation authority advances day, weather, crop stages, and morning automation. | Inventory stayed open while the day clock advanced from 490.838 to 495.756 game minutes; `Time.timeScale` remained 1. |
| Individual UI | `FarmHudController.IsModalOpen` blocks only local input. | All 12 HUD panels entered modal state without changing `Time.timeScale`. |
| Farming loop | Prepare, plant, water, harvest, and hotbar assignment use the async mock confirmation seam. Peer tool actions emit a transport-neutral intent and reject local mutation. | Mock test rejects early harvest, confirms mature harvest, preserves idempotency, inventory, yield, and quality. |
| Commerce and progression | Core peer actions emit `FarmSessionIntent` before rejection; unconnected systems still reject peer-side mutation. | The PEER audit rejected commerce, storage transfer, hotbar assignment/selection, building placement, pickup collection, sleep readiness, and order delivery with money, inventory, placed objects, and votes unchanged. The intent audit verified IDs for cultivation, hotbar, storage, inventory organization, commerce, and sleep. |
| World state | `FarmWorldSessionSnapshot` carries a deep copied shared farm save, host time metadata, revision, and sleep vote state. `FarmTestPlot.WorldSnapshotReady` emits it after host-side shared mutations when a transport subscribes. | Host capture/peer apply passed in Play Mode; repeated revisions and peer-side capture were rejected. A host mutation emitted one current, valid snapshot with an inventory matching the host. |
| Sleep | `FarmSleepSessionSnapshot` includes participants and ready voters; only authority advances a completed vote into a new day. | Participant and readiness restoration passed with independent snapshot copies. |
| Local presentation | `FarmPlayerOwnership` separates local input/camera/equipment from future remote avatars. | A remotely marked avatar was rejected as the local third-person camera target. |

## Shared versus local data

Shared world data is represented by `FarmWorldSessionSnapshot`: inventory,
storage, crops, land, clock, deterministic weather inputs, buildings, progress,
mail state, and sleep votes.

Local-only data must not enter that snapshot: camera sensitivity and zoom,
accessibility preferences, cursor/UI state, currently open menus, and local
camera target/input ownership.

## Steam P2P adapter responsibilities

When networking begins, the adapter should:

1. configure `FarmSessionTime` with a monotonic host clock and role;
2. assign `FarmPlayerOwnership` while spawning local and remote avatars;
3. subscribe to `FarmSessionIntentBus.Requested` and forward its idempotent
   player intents to the host;
4. call `CaptureWorldSessionSnapshot` on join/recovery, subscribe to
   `WorldSnapshotReady` for normal confirmed mutations, and send compact deltas
   when that is more efficient;
5. drive remote transform, animation, and equipment presentation separately
   from local camera/input;
6. apply host sleep participants/readiness through `FarmSleepSessionSnapshot`;
7. never restore the temporary local save on a peer.

## Explicitly deferred

- Steamworks initialization, lobbies, invites, join/leave, and reconnect UI;
- packet serialization, reliability, compression, interpolation, and bandwidth
  tuning;
- remote player transform/animation packets and avatar spawning;
- authoritative backend, global market, authentication, and persistence.

See [Coop-Session-Authority.md](Coop-Session-Authority.md) for the detailed
authority rules and snapshot contract.

## Latest authority audit

In Play Mode, the runtime was configured with a monotonic external clock and
the `PEER` role. The audit then attempted shared mutations through the same
public paths used by the game UI: sell-all commerce, storage transfer, hotbar
selection and assignment, build placement, world-pickup collection, bed
readiness, and daily-order delivery. Every attempt was rejected before the
shared state changed. The test restored the local solo clock afterward.

This is intentionally a transport-free proof of the client-side invariant: a
future Steam adapter may replace rejection with a host request/response, but
it must not enable local optimistic mutation of the shared farm.
