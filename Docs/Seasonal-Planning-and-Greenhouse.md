# Meta 6 - Seasonal Planning & Greenhouse

## Player loop

1. Check a crop's preferred season.
2. Plant it outdoors only in that season.
3. Reach Cultivation Mastery level 2 and craft a **Greenhouse Kit** from Wood,
   Stone, and Refined Stone.
4. Place the greenhouse through the existing Build catalog.
5. Tiles within its radius can plant any crop. While still covered at harvest,
   they receive that crop's preferred-season yield and quality treatment.

## Authority contract

`FarmMockBackend` receives a greenhouse-coverage callback from the host plot.
It rejects an off-season `PlantSeedAsync` request before removing a seed. The
callback resolves only against `FarmGameState.PlacedObjects` and
`FarmBuildableDefinition` data: it does not depend on renderers, physics, or
scene hierarchy. A future Steam host or Supabase function must make the same
query against its authoritative placement snapshot.

Harvest resolves coverage again. Therefore reclaiming or moving the greenhouse
before harvest removes its climate benefit; the crop remains harvestable, but
uses the actual world season. This is deliberate and keeps the placement state
meaningful without adding hidden per-tile state.

## Data assets

| Purpose | Asset |
| --- | --- |
| Placeable item | `Assets/Resources/GameData/Items/GreenhouseKit.asset` |
| Buildable definition | `Assets/Resources/GameData/Buildables/07_greenhouse.asset` |
| Crafting recipe | `Assets/Resources/GameData/Recipes/06_greenhouse.asset` |
| Visual prefab | `Assets/PolygonFarm/Prefabs/Buildings/SM_Bld_Greenhouse_01.prefab` |
| Localized strings | `Assets/Resources/FarmLocalization/en.json` |

## Initial tuning

- Recipe: 12 Wood, 6 Stone, 4 Refined Stone.
- Unlock: Cultivation Mastery level 2.
- Footprint: 8 x 5 x 8 units.
- Coverage: 4.5 units from placed center.

These are early mechanics values, intentionally exposed in assets for tuning.

## Acceptance checks

- An outdoor crop whose preferred season differs from the current season is
  rejected and no seed is consumed.
- The same tile becomes eligible after a greenhouse is placed within coverage.
- Backend-confirmed harvest under greenhouse coverage uses preferred-season
  yield/quality; moving or reclaiming it restores normal seasonal results.
- The kit, recipe, buildable, and English labels load through Resources.
- Time continues while inventory, crafting, building, and diagnostic UIs are
  open; no new system pauses the shared session.
