# Seasonal Festival Contributions

## Purpose

The seasonal calendar now creates a cooperative activity, not only a visual date. On day 6 of each 7-day season, the farm can join the **Harvest Share**: a short shared contribution target that turns stored crops into a valley reward.

This gives the group an intentional preparation question: sell crops, fulfill daily orders, preserve meals and gifts, or reserve some harvest for the seasonal contribution. The activity is small enough for an instanced farm and does not require an open multiplayer town map.

## Live loop

- Harvest Share is active on day 6 of every season.
- The shared farm target is six harvested crop items.
- Select a crop in the hotbar and press **V** to contribute one item.
- The live prompt shows `current/6`, stays active while the event is unfinished, and confirms completion.
- Completing the target once grants $150 and +1 shared Favor with each of Elara, Bram, and Niko.
- The target cannot be completed twice in the same seasonal event.

## Co-op and state

`FestivalContribution` is a host-routed session intent. The host checks the active event, selected crop category, inventory, event completion, reward, and shared Favor result. `FarmFestivalProgress` records the event ID, contribution count, and completion in `FarmSaveData` version 26.

## Validation

`META21_FESTIVAL_SMOKE` activated day 6, contributed six pumpkins, awarded exactly $150, applied +1 Favor to all three neighbors, rejected a seventh contribution, and restored both festival and community state from save data. Unity Console audit returned no errors.

## Next expansion

Future content can use the same event boundary for different seasonal goals, contribution categories, rewards, community projects, and festival scenes. The next meta should deepen long-term farm purpose through projects and unlocks rather than adding cosmetic festival props.
