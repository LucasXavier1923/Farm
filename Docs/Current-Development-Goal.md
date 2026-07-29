# Current Development Goal - Developer Console

Updated: 29 July 2026.

## Scope decision

Farm remains a 3D cozy farming game for one to four players. It uses a local
host-led co-op model: one player's game owns the farm save and simulates the
shared world; invited players send gameplay input and receive synchronized
state. No Supabase, global market, auction house, dedicated server, or remote
anti-cheat service belongs to this project.

This is not couch split-screen. It is a small hosted co-op session designed to
work solo first and later connect friends through the selected transport.

## Current meta

Create a complete development-only console for validating the farm simulation
without modifying scene data or waiting through the normal loop. It must open
with `H`, keep shared time running, and manipulate local host state through
explicit APIs.

1. Manipulate time, day, season and clock speed.
2. Force or restore deterministic weather and pest schedules.
3. Trigger rain, crop growth, watering and pest outcomes for focused tests.
4. Grant local money/energy and spawn every registered item.
5. Keep visible text localization-ready and document the test checklist.
6. Exclude the console from release builds and validate it in the Unity Editor.

## Next milestones

- **Co-op foundation:** host, join, leave, shared state, ownership and save.
- **Four-player playtest:** movement, item use, inventory and world
  interactions under latency simulation.
- **Content production kit:** stable authoring rules for the world artist.
- **Presentation pass:** turn the validated mechanics into the first farm.
