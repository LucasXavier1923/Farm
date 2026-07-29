# Meta 12 — Meaningful Farming

The farming loop is intentionally based on choices rather than repeated clicks. The existing systems make the season, greenhouse coverage, crop affinity, watering, pests, quality, and tool/mastery progression visible in a small test plot.

## Compost decision

`Compost` is a stackable fertilizer item. A fresh farm starts with two units so the feature can be tested immediately.

- Drag Compost from the inventory to an empty hotbar slot, then select it.
- Use it on prepared soil or on a planted crop before it is ready.
- One unit is consumed only after the simulated authoritative backend confirms the request.
- A plot can be enriched once per harvest cycle. Compost grants `+1` yield and one additional quality score.
- Compost is consumed at harvest; it does not carry into automatic replanting.

The visual soil tint and the item tooltip communicate the active state without relying on hard-coded UI text. All visible text is resolved through `FarmLocalization`, with English entries in `Assets/Resources/FarmLocalization/en.json`.

## Authority boundary

`FarmFertilizeTileRequest` follows the same asynchronous path as planting, watering, and harvesting:

1. Unity requests a fertilizer action from `IFarmBackend`.
2. The mock backend validates tile state, item category, inventory, idempotency, and readiness.
3. Only a confirmed response changes the visual tile and applies its inventory snapshot.

The host applies this request/result contract directly to the shared local save. Guests cannot alter the farm state without the host session processing the action.

## Save compatibility

Tile saves now contain `Fertilized`. Older saves deserialize it as `false`, so they keep their previous behavior. The current save version is 23.
