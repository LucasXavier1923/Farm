# Community Projects and Market Route

## Purpose

Community projects create a durable co-op objective beyond a single day. The first project, **Market Route**, turns raw and refined materials into a permanent logistics improvement: daily orders can draw crops from shared storage even before the Commerce mastery reaches level 3.

This connects gathering, mining, processing, crafting, storage, farming, and daily orders into one visible group investment.

## Live loop

- Outside the seasonal festival day, the HUD prompt displays Market Route progress.
- Select Wood, Stone, or Refined Stone in the hotbar and press **B** to donate one material.
- Required materials: 15 Wood, 10 Stone, 3 Refined Stone.
- The project persists through every day until complete.
- Completion permanently enables storage-backed daily order delivery for this farm save.

## Co-op authority

Peers submit `CommunityProject` with an item ID only. The host validates the project state, accepted material, inventory removal, material cap, completion, and unlock. `FarmCommunityProjectProgress` persists in `FarmSaveData` version 27.

## Validation

`META22_PROJECT_SMOKE` donated all 28 materials, completed Market Route, delivered a daily order using crops held only in storage while Commerce was below level 3, and restored the completed project from save data. Unity Console audit returned no errors.

## Next expansion

The project state is intentionally a scalable boundary for future town/farm projects. The next justified gameplay meta is deeper animal husbandry: individual animal needs, product variation, and a small breeding or upgrade choice that uses current care, feed, production, and community loops.
