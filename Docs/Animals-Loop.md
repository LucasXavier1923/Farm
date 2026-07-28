# Meta 13 — Animals & Care

The starter chicken coop is a small but complete daily production loop, designed to remain valid for a shared co-op farm.

## Daily loop

1. Collect the daily `Animal Feed` supply.
2. Feed the chickens once.
3. Collect the resulting `Chicken Egg`.
4. Sell, store, process, or use the egg in a later recipe.

Each daily action has a persistent ID (`starter_coop:feed_supply:{day}`, `starter_coop:fed:{day}`, and `starter_coop:egg:{day}`), so save/load and host reconstruction use the same source of truth.

## Care reward

Consecutive fed days form a care streak.

- Days 1–2: Normal egg.
- Days 3–4: Silver egg.
- Day 5 onward: Gold egg.

The egg is added with its quality metadata, so existing inventory, tooltip, market price, storage, and future global-economy boundaries already receive the correct value without a special animal-only inventory type.

## Co-op authority boundary

`FarmSessionIntentKind.AnimalCare` is a dedicated peer request. The host routes it to `FarmAnimalSystem.ExecuteHostCareAction`, which validates the allowed action, daily state, inventory, capacity, and quality before changing shared state. Local host input uses exactly the same method.

This is deliberately separate from a generic tool action: an animal-care request has no tile index and cannot impersonate a farming tool command.

## Planned expansion points

The system intentionally has room for purchasable animal homes, more species, feed recipes, illness, breeding, and product recipes. Those are future content choices; this meta does not add cosmetic livestock systems without a testable care or production purpose.
