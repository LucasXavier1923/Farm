# Crop Care, Soil Health, and Seasonal Mastery

## Purpose

Crop care now rewards planning instead of adding a mandatory daily maintenance chore. Compost remains an optional high-value input; rotation adds a second, passive choice: plant a different crop after the previous harvest in the same plot to improve the next crop's quality.

## Rotation Rule

- Each plot remembers the crop most recently harvested there.
- Planting a *different* crop marks the new crop as **rotated**.
- A rotated crop adds one quality point at harvest. It can combine with the preferred season, Harvesting mastery, and Compost.
- Repeating the same crop is always valid; it simply does not receive the rotation point.
- The effect is visible in the active plot status as `rotated soil`, then is consumed by harvest.

This is intentionally a quality bonus rather than an extra-yield multiplier. The player can still specialize in one crop when that supports a request, recipe, or market plan, while diverse farms earn better sale and crafting inputs.

## Authority and Save Safety

`FarmSoilRules` is the single rule source used by both the local tile presentation and `FarmMockBackend`, the current asynchronous authoritative boundary. The backend creates `FarmHarvestSnapshot.WasRotated` and evaluates quality only after validating seed, soil, time, inventory space, and climate.

`FarmTileSaveData` now persists the previous harvest ID and active rotation flag. A ready crop retains both Compost and rotation benefits across save/load until it is harvested. Existing saves receive empty/default fields and remain valid.

## Connected Systems

- **Seasons and greenhouse:** preferred season contributes independently to crop quality.
- **Compost:** adds yield and quality; it remains optional and is also useful for rainy quarry stewardship.
- **Harvesting mastery:** combines with rotation without requiring a single correct build.
- **Market, orders, recipes, and gifts:** higher-quality crops receive higher market value and become more valuable farm inputs.

## Validation

`META30_ROTATION_SMOKE` passed against the async mock backend. It verified: no bonus on the first crop, a rotated Carrot after Pumpkin, Silver quality from rotation outside the preferred season, no rotation bonus when Carrot follows Carrot, and JSON persistence of the soil fields.
