# Farm World Presentation (Meta 10)

`Assets/_Project/Scenes/FarmWorld.unity` is the editable world scene. It was
created as a safe copy of `FarmPrototype.unity`, so the mechanics harness stays
available for regression work. The copy retains the Polygon Farm environment,
player, camera, lighting, and the `Farm_Test_System` gameplay bootstrap.

## Authoring boundary

The visual world is intentionally separate from the systems in
`Assets/_Project/Scripts`. World construction may move or replace scenery and
interactive presentation objects, but it must preserve:

- `Player` and its controller/camera target;
- `Farm_Test_System` (or its future scene bootstrap replacement);
- a clear, collision-free area around the initial plot and stations;
- a main camera and directional light;
- the world ground below the player spawn and test plot.

Do not edit shared economy, co-op, or gameplay scripts while composing the
world. Use a scene copy for risky art experiments.

## Current validation

FarmWorld starts without a FarmSteamSettings missing-script warning after the
settings asset repair. Import warnings about Synty mesh triangles and copied
avatar-bone length are source-asset warnings, not FarmWorld runtime failures.

## Next world pass

1. Pick a deliberate farmhouse/start plot location in FarmWorld.
2. Move the mechanical test area next to that location, then play-test camera,
   interaction distance, and nighttime visibility.
3. Reserve clear expansion zones for crop fields, animals, production, and
   future biomes. Keep the test systems until their replacements are verified.
4. Only then replace Polygon Farm demo-only dressing with authored content.
