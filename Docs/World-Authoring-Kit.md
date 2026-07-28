# Meta 16 — World Authoring Kit

`FarmWorldAuthoringKit` is a scene-only component for `FarmWorld`. It does not create mechanics, props, or final art. Instead, it reserves named, visible zones and validates that the gameplay bootstrap remains intact.

## Required zones

Create zones with stable IDs, not decorative object names:

- `spawn` — player arrival and camera-readable first impression.
- `fields` — initial crop plot plus future field expansion.
- `animals` — coop and future husbandry area.
- `production` — crafting, processing, and storage workflow.
- `expansion_*` — optional future land/biome reservations.

Select the `Farm_Test_System` object in `FarmWorld` to edit zones. Gizmos are authoring-only and do not appear in builds. Use **Validate World Authoring Setup** in the component menu after major edits; it checks Player, FarmTestPlot, Main Camera, a scene light, valid zone sizes, and duplicate IDs.

## Safe world-building workflow

1. Duplicate FarmWorld before an experimental art pass.
2. Reserve the zones before moving large Synty structures or terrain.
3. Preserve Player, Farm_Test_System, camera, light, and collision-free access around active mechanics.
4. Play-test interaction distance, camera rotation/zoom, nighttime navigation, and stations after every layout pass.
5. Replace temporary presentation only after its gameplay replacement is tested.

The kit intentionally avoids placing scenery automatically. World composition stays in the hands of the game’s authors rather than becoming hidden logic in a prototype script.
