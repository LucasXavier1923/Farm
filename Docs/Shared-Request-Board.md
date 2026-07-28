# Shared Request Board

## Purpose

Meta 26 turns daily orders into a shared co-op work planner instead of a crop-only selling shortcut. The board now pulls on activities already available in the farm: growing, fishing, animal care, and workshop production.

## Deterministic board composition

The board always has three daily slots, derived from the shared world seed and day.

| Slot | Activity | Requested goods |
| --- | --- | --- |
| 1 | Farming | A harvested crop. |
| 2 | Animal Care on odd days / Fishing on even days | Chicken Egg, or a fish species. |
| 3 | Workshop | Refined Stone or Fish Skewer. |

This gives a four-person farm natural roles without hard-locking players into classes: one person can tend crops while another fishes, cares for animals, or runs the workbench. Every fulfilled order still grants Gold, shared Favor with its requester, Commerce XP, and the existing full-board bonus.

## Authority and storage

`FarmDailyOrder` now identifies a typed `ItemId`, not a crop-only ID. `FarmGameState.TryCompleteDailyOrder` validates the current day, completion state, item definition, inventory/shared-storage availability, item removal, reward, Favor, and persistence in one host-owned transition.

Peers request delivery with the `DailyOrder` session intent. `FarmHostIntentRouter` regenerates the authoritative board from its own shared state and never accepts a client-supplied item, quantity, price, or reward.

Market Route and Commerce Level 3 still permit delivery from shared storage. The board therefore rewards logistical co-op rather than forcing the active player to carry every item.

## Player loop

1. Open the board near the order station.
2. Read each activity label and item requirement.
3. Choose which role to pursue before the pond rests, the day ends, or scarce materials are consumed elsewhere.
4. Deliver through the board for Gold, Favor, and Commerce progress.
5. Complete all three for the daily board bonus.

## Verification

`META26_ORDER_SMOKE` passed in Unity. It generated Crop/Animal/Production requests on day one and a Fishing request on day two, delivered all requirements through the authoritative state method, verified the board bonus, rejected duplicate delivery, and verified day-change save/load behavior.

## Localization

Activity labels and generic request text are in `Assets/Resources/FarmLocalization/en.json` under `orders.type.*`, `orders.item`, and `state.order.*`. Item names use their existing localized item keys.
