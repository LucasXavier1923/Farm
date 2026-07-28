# Meals and Home Comfort

## Purpose

Meals give low-intensity farm resources a personal payoff. A Wildflower found on a route is no longer only a sale value: it can become energy for one more meaningful choice in the same day. This is the first layer of home comfort, intentionally small before rooms, furniture bonuses, and a real kitchen space are authored.

## Live loop

1. Find Wildflower or Forest Mushroom through the seasonal forage route.
2. Combine it with wood at the existing shared production bench.
3. Drag the resulting meal to any hotbar slot.
4. Select it and press **R** outside of a menu to consume it.
5. The host validates the item, removes one serving, and restores Energy.

## Current meals

| Recipe | Ingredients | Energy | Sell value |
| --- | --- | ---: | ---: |
| Wildflower Tea | 1 Wildflower, 1 Wood | 18 | 22 |
| Mushroom Stew | 2 Forest Mushroom, 1 Wood | 35 | 38 |

The UI tooltip is localized and explains the `R` action. Meals deliberately use normal inventory and hotbar paths, so drag/drop, stack depletion, save/load, and co-op inventory authority remain consistent.

## Co-op boundary

Consumption is a dedicated `Consumption` session intent. A peer submits only the selected item ID; the host validates category, ownership, and energy capacity before applying the result. The transport is still a local seam until real Steam validation, but the gameplay mutation does not originate on a peer.

## Validation

`META19_MEAL_SMOKE` crafted Wildflower Tea from inventory ingredients, spent 40 Energy, consumed the tea for exactly 18 Energy, confirmed the serving left inventory, and rejected a second consumption attempt.

## Deferred extension: gifts

Gifts need a clear recipient choice, preference data, response UI, and per-player relationship authority. They are therefore intentionally deferred to the next social-content meta rather than represented as an invisible automatic bonus. The two meal IDs are ready to become valid gift content later.
