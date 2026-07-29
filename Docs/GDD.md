# GDD - Farm

## Vision

Farm is a 3D cozy farming game for one to four friends. Players restore a small
property together: prepare soil, plant, water, harvest, fulfill town requests,
build useful structures and make the farm visibly their own.

## Technical promise

The game is **host-led co-op**, not an MMO and not a backend-driven economy.
The host owns the local save and shared simulation. Guests join that session and
their actions are synchronized through the co-op transport. Solo play uses the
same host simulation.

The project explicitly excludes Supabase, auction houses, global asynchronous
trade, dedicated servers and external anti-cheat validation.

## Design pillars

1. Cozy cooperation: friends help naturally, but all activities remain viable
   solo.
2. Visible homestead growth: fields, production and buildings show progress.
3. Meaningful daily choices: time, energy, inventory and shared priorities.
4. Production-friendly scope: mechanics first, then the world artist composes
   the final farm with stable tools.

## Core loop

Plan the day -> prepare soil -> plant -> care -> harvest -> sell locally or
fulfill town requests -> improve the farm -> rest.

## First playable co-op target

- One hosted farm with up to four players.
- Shared time that never pauses because one player opens UI.
- Synced movement, interaction, planting, watering, harvesting and pickups.
- Shared farm storage, local host save, inventory and quickbar.
- Local shop prices and town requests; no player-to-player market.
- Clear join/leave feedback and a reliable recovery path when the host ends a
  session.
