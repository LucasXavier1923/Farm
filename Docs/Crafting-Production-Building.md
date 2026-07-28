# Meta 14 — Crafting, Production & Building

The shared-farm production chain is intentionally mechanical:

`renewable materials → recipe or timed processor → kit/output → validated building placement or market/storage use`.

## Authority

Crafting and the workbench processor now use `FarmSessionIntentKind.Production` for peer requests. The host validates recipe IDs, ingredients, mastery locks, processor job state, inventory capacity, and the final collection. Local host interaction calls the same public host methods.

Building remains a host-management action under `FarmPermissionPolicy`. Placement, movement, reclaiming, footprint checks, fence snapping, and persistent object writes are therefore never mutated by a peer. This is deliberate shared-farm governance rather than missing co-op support.

## Persistence and timing

- `FarmGameState.TryCraft` performs the authoritative ingredient/output transaction.
- `FarmGameState.TryStartProcessing` records a game-time completion timestamp after consuming input.
- `FarmGameState.TryCollectProcessing` grants output exactly once and retains a full-inventory job for later collection.
- `FarmPlacedObjectSaveData` preserves placed kits, positions, rotations, and building functions across save/load and future shared snapshots.

## Test content

The current proof chain is Stone → Refined Stone → Farm Shed Kit / utility buildable. It is intentionally generic; future recipes and final world placement should extend data assets, not fork the transaction code.
