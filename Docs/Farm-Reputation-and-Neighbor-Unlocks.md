# Farm Reputation and Neighbor Unlocks

## Purpose

Meta 37 gives shared Favor a durable gameplay meaning. Daily orders, favorite artisan gifts, and festival participation already improve bonds with Elara, Bram, and Niko. Reaching bond level 2 (5 Favor) now unlocks a practical neighborhood benefit for the whole farm instead of only another payout of gold.

This is a lightweight cozy-game relationship pattern: a neighbor's trust changes how the homestead works, while the player remains free to pursue any activity in any order.

## Persistent benefits

| Neighbor | Bond requirement | Unlock | Connected decisions |
| --- | --- | --- | --- |
| Elara | 5 Favor / bond level 2 | **Seed Share**: every seed pack includes +1 seed. | Makes planning, planting, and seasonal variety more generous. |
| Bram | 5 Favor / bond level 2 | **Pond Tip**: the daily pond limit rises from 3 to 4 catches. | Adds a fishing choice without removing the pond's rest rhythm. |
| Niko | 5 Favor / bond level 2 | **Workshop Shelf**: the processing queue rises from 3 to 4 jobs. | Gives artisan goods and morning automation more scheduling flexibility. |

The benefits are derived directly from the saved Favor record. There is no extra claim button, duplicated reward state, or client-side grant that can desynchronize from the host.

## Player feedback

- The Daily Orders summary shows every active Neighbor Perk.
- The pond prompt uses the current catch limit.
- The workbench prompt and queue confirmation use the effective queue capacity.
- Seed-pack purchases use the effective amount, so the inventory and existing purchase UI show the extra seed through normal confirmed state updates.

## Co-op and authority

Favor is shared farm state. Consequently, these benefits are shared as well:

- Orders, gifts, and festivals already enter through host-validated command paths.
- Unlock eligibility is calculated on the host from `FarmCommunityProgress`.
- A peer never sends a request that claims an unlock; it only uses the ordinary purchase, fishing, or production request, whose authoritative execution observes the current shared bond.
- Because the unlock is derived from Favor, reconnects and old snapshots cannot award it twice.

## Save compatibility

No new independent save field is necessary. `FarmCommunityProgress` has been persisted since save version 24. Any existing save at or above the Favor threshold receives the correct benefit when loaded; lower-bond saves remain unchanged.

## Localization

All neighbor-perk copy uses `community.unlock.*` and `community.perks.active` in `Assets/Resources/FarmLocalization/en.json`. Contact IDs remain stable data identifiers and never appear as untranslated UI text.

## Validation

`META37_NEIGHBOR_UNLOCK_SMOKE` set each neighbor to 5 Favor on a real `FarmGameState`, purchased a seed pack, queued four processing jobs, caught four fish through the host catch boundary, rejected the fifth queue/catch, then saved and restored the Favor-derived unlocks.

```text
passed=True seeds=6 queueCapacity=4 queue4=True queue5Rejected=True catchLimit=4 catch4=True catch5Rejected=True persisted=True
```
