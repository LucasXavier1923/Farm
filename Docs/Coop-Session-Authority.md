# Cooperative Session Authority

## Current solo behavior

Solo play is the simulation authority. `FarmSessionTime` uses Unity's unscaled
clock, so an individual player opening inventory, a shop, the journal, or any
other modal never pauses the farm.

## Steam P2P integration seam

Before a peer joins, a Steam session adapter can call `FarmSessionTime.Configure`
with a monotonic host-synchronized clock and an authority flag.

Each Unity runtime starts in the local solo fallback, even when domain reload is
disabled in the Editor. A valid external session supplies both clock delegates;
an incomplete configuration restores the solo fallback rather than leaving a
stale host/peer flag active between runs.

- The host keeps `IsSimulationAuthority` enabled.
- Remote peers disable it and render host-confirmed world snapshots.
- `FarmDayClock` only advances game time on the authority.
- `FarmWeatherSystem` only applies rain watering on the authority.
- `FarmTestTile` only advances crop growth stages on the authority.
- Sleep readiness may be displayed by every peer, while only the authority can
  advance a completed sleep vote into a new day.

The present project does not include transport, replication, or Steamworks.
This contract deliberately makes those additions an adapter concern instead of
requiring the gameplay loop to be rewritten.

## Remaining network work

The future adapter must replicate authoritative snapshots for clock, weather,
tile state, inventory/ownership, placed objects, and player positions. It must
subscribe to `FarmSessionIntentBus.Requested`, route its idempotent action
requests to the host, and return confirmed results. Soil preparation, planting,
hotbar assignment, watering, and harvesting already use
the mock request/confirmation path with idempotent command IDs. The mock owns
the crop-ready timestamp, season-based yield, harvest quality, inventory-space
validation, and the cultivation-level replant decision; the client applies only
the confirmed inventory and tile result.

## Shared-state command boundary

While transport is still absent, a non-authority client must never mutate
shared farm state optimistically. Core peer requests create a
transport-neutral `FarmSessionIntent` with an id, requester, kind, payload, and
host-clock timestamp. `FarmSessionIntentBus.Requested` exposes those requests
to a future Steam adapter. Cultivation, hotbar selection/assignment, inventory
and storage operations, commerce, and sleep readiness emit intents before
showing host-confirmation feedback; they still never alter the local save.

Crafting, pickup collection, building placement/movement/reclaiming, sprinkler
automation, upgrades, daily-order delivery, journal rewards, and mail
attachments continue to reject peer-side changes until their host handlers are
connected to the same DTO.

Presentation remains local: menus may open, tooltips may render, camera
preferences remain client-owned, and a peer may render snapshots supplied by a
future host. Steam P2P will replace each rejection with a request/response
round trip and an authoritative snapshot application.

The temporary local save is also host/solo-only. Checkpoints, F5 save, F9 load,
and shutdown persistence are ignored on a peer, preventing one client from
overwriting or restoring shared farm state while a transport adapter is absent.

The HUD exposes the current session role (`SOLO`, `HOST`, or `PEER`) beside the
local controls and save status. It is presentation-only and updates from the
same session-time seam used by the future adapter.

## World snapshot contract

`FarmWorldSessionSnapshot` is the transport-agnostic payload for shared farm
state. It carries a protocol version, monotonic host revision, host-time
metadata, a deep copy of `FarmSaveData` (inventory, tiles, clock, weather
inputs, placed objects, progression, mail, and other shared state), and the
participant/readiness list for the sleep vote. The host captures it after
confirmed mutations; a peer accepts only newer valid revisions and rebuilds the
existing 3D presentation from that copy. Camera preferences, menus, and other
local presentation state are intentionally excluded.

`FarmTestPlot.WorldSnapshotReady` is raised by the authority after a shared
state change settles for the frame (at most ten times per second while a
listener is attached). The payload is a fresh `FarmWorldSessionSnapshot`.
Steam P2P later subscribes to this event, serializes that payload, routes action
requests to the host, and delivers the resulting snapshot or more compact
confirmed deltas. The transport is still intentionally absent from this project.

## Local player presentation seam

`FarmPlayerOwnership` defaults to the current solo player. A future spawn
adapter can mark instantiated avatars as remote. Remote avatars then ignore this
client's keyboard, cannot become the third-person camera target, and do not show
the local player's selected tool. The adapter will later drive their transform,
animation, and equipped-tool presentation from received player data; no camera
preferences or local input state needs to be replicated.

For the implementation and verification matrix, see
[Coop-Readiness.md](Coop-Readiness.md).

## Confirmed-action equivalence

The confirmed soil, planting, and watering visuals apply the same local
progress effects as their solo counterparts after acceptance: journal metrics,
tutorial milestones, cultivation mastery, energy cost, and tool feedback. A
rejected command applies none of those effects.

## Area tools

Confirmed hoe and watering-can actions preserve the existing level-one, three
tile line, and 3x3 tool areas. The mock processes each affected tile as its own
idempotent request, then applies a single aggregated energy, mastery, and
feedback result. This keeps the eventual host command stream explicit without
changing the solo tool economy.
