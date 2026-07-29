# First Farm Week

This document is the implementation contract for the active content vertical
slice. It sits between the broad product direction in `GDD.md` and the future
network architecture documents.

## Player promise

Within one short play session or a few relaxed in-game days, a new farm should
move from a small hand-watered plot to a larger, protected field with its first
automated morning task. The player always has a clear next aspiration, but is
never prevented from farming, exploring, selling, or playing with friends.

## Seven milestones

| # | Milestone | Completion data | Why it exists |
| --- | --- | --- | --- |
| 1 | First Harvest | Six tutorial actions | Teaches the complete farm loop. |
| 2 | Village Favor | One delivered order | Gives crops a destination beyond direct selling. |
| 3 | Crop Variety | Two harvested crop IDs | Introduces seed choice and the market. |
| 4 | Keep the Crows Away | Placed Scarecrow Kit | Teaches gathering, crafting, placement, and crop protection. |
| 5 | Make Room to Grow | Land level 2 | Makes farm growth physically visible. |
| 6 | Work Smarter | One tool upgrade | Demonstrates higher-throughput farming. |
| 7 | Morning Automation | Placed Sprinkler Kit | Pays off crafting with automatic 06:00 watering. |

## Runtime contract

- `FarmFirstWeekProgress` derives the current milestone entirely from
  `FarmGameState`; no separate local-only quest save is created.
- Order completion is recorded permanently as `Journal.OrdersDelivered` so a
  player does not lose first-week credit when daily orders rotate.
- Placed Scarecrow and Sprinkler kits are already persisted as placed objects.
- The HUD always presents the first unfinished milestone. Completed milestones
  remain completed even if the player sells items, sleeps, opens menus, or
  loads a save.
- In future co-op, the host snapshot is the only state the display needs. All
  players therefore see the same farm-week path.
- A milestone is guidance, not a gate. It does not pause time, lock UI, change
  prices, or prevent optional activity.

## Content boundaries

The First Farm Week intentionally uses the systems already present: four crop
types, daily orders, market prices, world pickups, a workbench, recipes,
building placement, plot expansion, tool upgrades, weather, crows, and morning
automation. It does not add player-to-player trade or final economy assumptions.

The initial workbench area supplies 12 Wood and 8 Stone in total. That is
enough for the intended Scarecrow (6 Wood, 2 Stone) followed by the Sprinkler
(4 Wood, 6 Stone), while a proper renewable gathering loop is still deferred
to mining.

Mining, fishing, and animals become the following content layers. Their rewards
should plug into this path as materials, food, or care goals only after the
farm loop has been playtested end-to-end.

## Acceptance checks

1. Start a fresh farm and see milestone 1 with the actual next tutorial action.
2. Finish the tutorial, then deliver an order; the HUD advances to crop variety.
3. Harvest a second crop type; the HUD advances to the Scarecrow.
4. Place a Scarecrow, buy land level 2, upgrade a tool, and place a Sprinkler;
   each action advances exactly one relevant milestone.
5. Save, reload, and confirm the HUD keeps the correct completed milestone.
6. Open any individual UI while the farm clock continues to advance.
7. Run the Unity Console check with no current project errors before handoff.
