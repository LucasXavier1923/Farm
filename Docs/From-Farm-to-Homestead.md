# Meta 2 - From Farm to Homestead

This is the implementation contract for the second content vertical slice.
It follows the completed First Farm Week and turns its working systems into a
coherent, shared home property.

## Player promise

After Meta 2, the player should no longer feel as though they are testing a
field beside disconnected props. They should recognize a home, know where each
daily activity happens, and see useful additions remain in the world as proof
of progress.

The slice must feel equally natural with one player or with four friends
sharing responsibilities. It must never require multiplayer transport to be
fun in local testing.

## Design principles

1. **Useful before decorative.** A construction must solve a meaningful need
   before the catalog grows with visual variants.
2. **Visible progress.** New capacity, care, work, and organization should be
   obvious from a normal camera distance.
3. **Soft specialization.** One player can farm while another gathers,
   crafts, fishes, or handles animals, but no player is locked into a class.
4. **Cozy legibility.** Paths, lights, fences, prompts, and zones should make
   the farm easy to read at night and from the elevated camera.
5. **No hidden pause.** Opening a panel, inventory, storage, or build catalog
   never stops the farm clock.

## Content boundaries

> **Scope correction — 26 July 2026:** the current 3D scene is a disposable
> mechanics testbed. This vertical must not add authored scenery, paths,
> fences, signs, landscaping, or decorative lighting to it. Mechanics and
> data may be proven here, but final-world composition happens later.

> **Lighting exception:** sparse runtime lantern probes are allowed only to
> validate the day/night visibility mechanic. They are non-colliding,
> non-persistent, and are not authored scene decoration.

### Included

- A storage/workbench zone with capacity and organization feedback.
- A starter chicken feed-and-egg loop without a visual-yard dependency.
- One new buildable utility structure, selected for its gameplay value.
- Generic, data-driven placement support for future utility/decor content.
- A short homestead guidance path that points to real daily routines.

### Not included

- Steam networking, shared remote object replication, or lobby work.
- External services or player-to-player trading.
- Dozens of furniture items, free-form interior simulation, or building
  destruction systems.
- Additional animal species until the chicken loop is pleasant and useful.
- Machines with complex input/output graphs or real-time production timers.

## Candidate first utility structure

The first utility structure is a **Farm Shed**: a compact buildable structure
placed near the workbench. Its single current benefit is **+15 shared storage
slots while placed**. Reclaiming it is blocked when doing so would leave the
existing storage contents above the reduced capacity. Recipe unlocks,
processing, and production timers remain future work.

## Acceptance checks

1. A fresh player can identify the house, field, storage, workbench, animal
   yard, sale point, and sleep point without opening a menu.
2. The storage and workbench area has a practical reason to visit during a
   normal farming day.
3. The coop has a clear feed-to-egg route and enough physical separation from
   the field, shop, and crafting area for testing.
4. The first utility building has one understandable benefit, persists, and
   follows existing placement/authority rules.
5. Paths, fences, and lighting remain readable at night without blocking the
player or the camera.
6. The clock continues while every homestead UI is open.
7. All new player-facing text comes from `Assets/Resources/FarmLocalization`.
8. Play Mode checks and the Unity Console show no current errors before
handoff.

### First expression layer

`Garden Bench` and `Garden Planter` are the first player-placed decorations.
They are intentionally cosmetic: they use the shared crafting, catalog,
placement, movement, and reclaim rules but add no money, capacity, automation,
or combat advantage. This establishes player expression without opening an
unbounded furniture-simulation scope.

The HUD also exposes a small non-modal **Homestead** routine. It gives the next
useful shared task without interrupting simulation: feed chickens, collect an
egg, use expanded shed storage, or continue decorating. It is presentation-only
and derives its text from the same state that a future host snapshot will
replicate.

### Validation record (26 July 2026)

- Farm Shed: placement changes shared storage from 30 to 45 slots; reclaim is
  denied when it would invalidate stored contents.
- Decor: the live recipe list crafts `Garden Bench Kit`; bench and planter
  placement persist through the normal buildable path and do not change storage
  capacity.
- HUD: the non-modal Homestead prompt showed the expected Chicken Yard feeding
  task in Play Mode.
- The project currently has no Unity Test Runner tests registered, so this
  slice was verified through targeted Play Mode checks. The Console was cleared
  and reported no current errors after the test pass.

### Farm Shed implementation

The Farm Shed is a Synty barn prefab wrapped as a buildable utility structure.
It is crafted from 8 Wood and 8 Stone, uses the existing build catalog and
placement persistence, and contributes 15 storage slots for each placed Shed.
`FarmGameState` derives that capacity from placed-object data, so loading or
rebuilding a host state produces the same capacity without a duplicated bonus.
It cannot be reclaimed while the remaining storage capacity would be exceeded.
