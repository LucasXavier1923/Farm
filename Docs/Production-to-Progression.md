# Meta 4 - From Production to Progression

## Purpose

Meta 4 connects normal farm work to durable advancement. It deliberately
builds mechanics, not a final environment: the current 3D scene is a
throwaway test harness and no rule depends on prop placement or scenery.

The core loop is:

`work or produce -> earn mastery XP -> unlock recipe/tool capability -> choose
focus -> coordinate a stronger shared farm.`

## Mastery tracks

| Track | Normal sources | Existing high-level perks | Natural co-op role |
| --- | --- | --- | --- |
| Cultivation | Hoe, seed, water actions | Free first cultivation action; replant cycle | Field grower |
| Harvesting | Crop harvest, world pickups, resource nodes | Longer pickup reach; overflow to storage | Gatherer / producer |
| Commerce | Selling, orders, seed buying | Tomorrow's requests; orders use storage | Trader / planner |

Experience thresholds are intentionally compact test values: level 1 starts at
0 XP, level 2 at 12 XP, and level 3 at 36 XP. These values are content tuning
inputs, not final economy balance.

## Data-driven unlock contract

Each `CraftingRecipe` owns these fields:

- `RequiredMastery`: the mastery track that controls access;
- `RequiredMasteryLevel`: a value from 1 to 3.

`CraftingRecipe.IsUnlocked(state)` is the canonical presentation check.
`FarmGameState.TryCraft` repeats that check before consuming ingredients, so a
UI bypass cannot craft a locked item. The crafting panel shows the required
track and level instead of reporting a confusing generic materials failure.

The first proof item is **Farm Shed Kit**. It requires Harvesting mastery level
2 and consumes processed Refined Stone, connecting Meta 3 production to this
progression step.

## Tool upgrade contract

Tool upgrades remain paid with money but now have a mastery requirement:

| Upgrade target | Required level |
| --- | --- |
| Tool level 1 -> 2 | Related mastery level 1 |
| Tool level 2 -> 3 | Related mastery level 2 |

Hoe and Watering Can use Cultivation. Harvest uses Harvesting. The shop text
and interaction feedback state the missing mastery requirement; the underlying
`TryUpgradeTool` validation remains authoritative for future UI/transport
calls.

## Soft specialization for co-op

`FarmSpecialization` contains `None`, `Cultivation`, `Harvesting`, and
`Commerce`. A player may choose one only after reaching level 2 in that same
track. The Mastery panel (`K`) provides the selection with keys `1`, `2`, and
`3` and clearly labels unavailable options.

A focus grants **25% additional XP** to that one track, rounded up per XP
award. It is deliberately *not* a class lock:

- all tools and actions remain usable;
- a focus may be changed later from the same panel;
- teams can naturally coordinate field, gathering, and commerce advancement;
- a future hard skill cap/respec system can replace this policy without
  changing recipe or tool requirement data.

The menu checks current simulation authority before requesting the state
mutation. A peer emits a transport-neutral `Progression` intent with its
requested specialization and does not mutate local shared state.
`FarmGameState.TrySetSpecialization` is deterministic and ready to be called
from the future host command/backend path.

## Persistence and shared authority

`FarmMasteryProgress` carries XP, the daily cultivation perk marker, and the
chosen specialization. `FarmSaveData` version 22 serializes a clone of that
object, so the value is also naturally present in the future shared world
snapshot. Older saves deserialize with `None` specialization.

No menu pauses `FarmSessionTime`. A future Steam adapter must forward the
specialization choice to the host and apply the confirmed state snapshot rather
than grant XP or mutate specialization on a peer.

## Verification performed

Play Mode state checks proved the following:

1. Harvest tool L1 -> L2 succeeds at mastery L1; L2 -> L3 is blocked before
   Harvesting L2 and succeeds after it.
2. Farm Shed Kit rejects a direct craft attempt at Harvesting L1.
3. Selecting Harvesting focus is rejected before level 2, then accepted at
   level 2.
4. Four focused Harvesting XP award five XP after the 25% bonus.
5. A v22 save/restore preserves specialization, XP, and the upgraded tool.

The Unity Console must still be cleared and checked at each later handoff.
