# Animal Bonds and Product Quality

## Purpose

The coop is now a connected care-and-product loop rather than a single daily egg pickup. Individual chickens have meaningful traits, favorite items, persistent Affection, and an optional shared reward for a fully cared-for flock.

## Daily Loop

1. Collect and place the daily feed, then care for a chicken.
2. After care, optionally select that chicken's favorite item in the hotbar and press **F** near them.
3. A favorite gift consumes the selected item, grants +2 Affection, and can be given once per chicken per day.
4. When every active chicken has received a favorite gift, **Coop Harmony** improves that day's egg quality by one tier.
5. Collect eggs. Pip's care trait improves quality at 2 Affection; Clover's care trait creates an extra egg at 3 Affection; expanded-coop Maple can create a Speckled Egg at 2 Affection.

Favorite gifts are optional. A farm may specialize in gathering, tea, cooking, requests, or market goods without being forced into an animal checklist; choosing to spend those goods on the flock has a clear product-quality payoff.

## Shared Authority and Persistence

- All care and favorite-gift mutations run through `FarmAnimalSystem.ExecuteHostCareAction` and the existing `AnimalCare` peer intent.
- Feed, care, and favorite actions use unique persistent day IDs. The host rejects duplicate gifts before mutating inventory or Affection.
- Animal records and action IDs are already included in `FarmSaveData`, so Affection and active Harmony survive save/load and future host snapshots.
- The trait calculation happens at egg collection, the shared reward boundary. It fixes the prior mismatch where Pip's documented quality trait was not actually applied.

## Connected Content

- Pip values Wildflowers, linking forage and animal care.
- Clover values Forest Mushrooms, creating a meaningful choice against stew and other forage uses.
- Maple values Wildflower Tea after the coop expansion, linking recipes, expansion, and animal care.
- Better eggs support the marketplace, animal orders, Farm Omelets, gifts, and future artisan processing.

## Validation

`META31_ANIMAL_SMOKE` passed. It verified host care, favorite-item consumption, duplicate rejection, Coop Harmony, Gold egg quality, Clover's two-egg trait output, and save/load persistence of action IDs and Affection.
