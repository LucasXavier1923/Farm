# Seasonal Goals, Collections, and Long-Term Discovery

## Purpose

Meta 35 turns discovery into a gentle, shared long-term objective. The Collection Book rewards players for trying different farm activities instead of asking them to grind one optimal item. It uses the items already produced by farming, animals, fishing, foraging, artisan production, projects, and exploration.

The design follows the cozy-game pattern of optional completion books: a player always has a readable next step, but no milestone blocks planting, selling, sleeping, or the current season's routine.

## Player flow

1. Discover an item by finding, harvesting, collecting, crafting, or receiving it through an existing authoritative system.
2. Press `L` (or use the Collection button) to open the Collection Book. Opening it never pauses farm time.
3. Browse all items, seeds, crops, materials, and projects. Unknown cards stay hidden, while known cards show ownership, stack size, and base value/use.
4. The header shows discovered count, the next breadth milestone, its reward, and whether `C` can claim it.
5. Press `C` while the book is open after reaching the threshold. The reward goes to the shared inventory only when there is capacity.

The header is intentionally two lines tall so the actionable next reward is visible rather than truncated behind the subtitle.

## Milestones

| Discovered items | Shared reward | Why it matters |
| --- | --- | --- |
| 8 | 2 Compost | Encourages the first crop-care quality decision. |
| 16 | 3 Animal Feed | Supports the starter coop and daily care rhythm. |
| 24 | 2 Refined Stone | Connects discovery to construction and workshop progression. |

These are breadth rewards, not collection taxes. Missing or unwanted items never stop the player from using any other system.

## Co-op and authority contract

- Discovery and the milestone bitmask belong to the shared `FarmGameState`, not a local player preference.
- A solo game and the host can call `TryClaimNextCollectionMilestone`.
- A peer uses the transport-neutral `CollectionMilestone` intent with `action=claim`; `FarmHostIntentRouter` validates and awards it on the host.
- The command path rejects locked thresholds, duplicate claims, and rewards that cannot fit in inventory.
- The currently installed local session seam reports that a peer is awaiting host confirmation. The existing Steam transport will forward the same intent without a gameplay rewrite.

## Persistence and migration

`FarmSaveData` version 33 stores `CollectionMilestoneMask`. Saves older than version 33 restore with no claimed milestones, while their previously stored discoveries remain valid. The bitmask prevents a reward from being paid twice after save/load or after a reconnect snapshot.

## Localization

All new player text is in `Assets/Resources/FarmLocalization/en.json` under `collection.progress_milestone`, `collection.progress_complete`, and `collection.milestone.*`. No UI or state logic embeds an English-only reward sentence. The localization loader also normalizes externally double-escaped text, while the English table uses native JSON escapes.

## Validation

`META35_COLLECTION_SMOKE` created a real `FarmGameState` component, registered eight distinct discoveries, opened the Collection Book, verified the visible next-step copy, claimed the first milestone, saved/restored the state, and verified that the duplicate claim was rejected.

The smoke result was:

```text
passed=True showedNext=True claimed=True milestone=8 persisted=True duplicateRejected=True
```

## Deferred work

Collections reward activity breadth. Future local contracts and seasonal events can add non-exclusive collection tracks, while keeping claims in the shared host session and avoiding a mandatory checklist.
