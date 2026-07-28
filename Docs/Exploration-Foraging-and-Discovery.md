# Exploration, Foraging and Discovery

## Design goal

Foraging creates a low-pressure reason to leave the crop grid each in-game day. It is a compact loop for a cozy farm: walk a short route, notice what the weather and season changed, collect something useful, then decide whether to sell it, save it, craft with it, or use it in a later social/cooking system.

It deliberately uses the playable farm instance rather than a shared open world. That keeps discovery compatible with host-authoritative co-op and avoids creating a second networking model before Steam transport is live.

## Live behavior

- Three deterministic forage opportunities are generated per farm day from world seed, day, season, and weather.
- Rain strongly favors `Forest Mushroom`; Spring favors `Wildflower`; Autumn and Winter shift toward mushrooms, wood, and stone.
- Each opportunity has a day-specific persistent pickup ID. A collected opportunity cannot be claimed twice, but the next day presents a fresh route.
- The existing host-only pickup validation grants the item. Peers wait for host authority rather than modifying inventory locally.
- Every pickup now records its item in the shared discovery collection, which is already saved and visible through the collection system.
- New foraged resources are `Wildflower` and `Forest Mushroom`; both are localized item data, not hard-coded display strings.

## Why this is a foundation rather than decoration

The loop adds activity (daily route), reward (materials and collection progress), choice (sell or preserve resources), and links to current systems (inventory, collection, weather, calendar, co-op authority). Meta 19 will give these materials more expressive downstream uses through cooking, gifts, and home comfort.

## Validation

`META18_FORAGE_SMOKE` generated rain and clear Spring routes, verified the rain route's mushroom bias and stable pickup IDs, collected all three items, rejected a duplicate claim, recorded the mushroom discovery, and restored both discovery and collection state from save data.

## Authoring extension

`FarmForageCatalog` is the temporary daily-content boundary. Later content can add biome zones, rare finds, tool requirements, visual prefab sets, and player-profile discovery without changing stable pickup IDs or the host collection path.
