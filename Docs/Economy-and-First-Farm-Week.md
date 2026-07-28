# Meta 5 - Economy & First Farm Week

## Purpose

Meta 5 creates a tunable local economy before any global market exists. The
first-week vertical should let a player buy seeds, grow crops, sell produce,
complete daily orders, and decide whether the next goal is a better tool, more
land, or a production project.

The current scene is only a 3D mechanics harness. The economic rules must not
depend on visual layout, prop placement, or authored environment art.

## Source of truth

`FarmEconomyBalance` is a `Resources/GameData/Economy` ScriptableObject. It
owns the live tuning values:

- seed-pack amount and cost per crop;
- normal crop sale value per crop;
- tool-level and land-level costs;
- daily-order reward multiplier, minimum bonus, rounding, and board bonus;
- deterministic market multipliers and their frequency bands.

`FarmEconomyRules` is the only gameplay-facing accessor. Missing or malformed
entries fall back to the legacy `CropDefinition`/`ItemDefinition` values and
safe defaults, allowing the catalog to grow without making the farm unusable.
During Play Mode it reads a non-persistent runtime copy of the asset, so a
debug experiment cannot leak into the next farm session or overwrite design
data.

## Economy rules

1. The market quote is deterministic from the shared world seed, day, item,
   and balance asset. The same host snapshot therefore produces the same price
   for every player.
2. Crop sale values are base value times quality multiplier times daily market
   multiplier. Non-crop values keep their base value until later systems need
   a market rule for them.
3. A daily order pays a configured multiplier over the crop's normal base
   value, rounded to a configured increment and protected by a minimum bonus.
   Completing the board awards a configured additional bonus.
4. Tool/land purchases use the shared rules at state-validation time; the shop
   is presentation only and cannot quote a different number.
5. Menus do not pause `FarmSessionTime`. A peer keeps requesting shared actions
   through the existing intent boundary and never changes money or inventory
   locally.

## First-week diagnostic ledger

The runtime ledger is a development aid, not a final player feature. It shows
for each crop:

- seed pack cost and amount;
- normal and preferred-season yield;
- base sale value;
- expected revenue and profit for spending one full seed pack;
- the current market quote;
- current tool and land investment targets.

This catches obvious balance issues early: a crop that never repays its pack,
one that dominates every other crop, or upgrades that require an unreasonable
number of harvest cycles. Final pacing decisions remain design work after real
playtests.

## Acceptance checks

1. Changing an economy asset value changes buy/sell/order/upgrade behavior
   without editing C#.
2. A missing balance entry uses legacy asset values safely.
3. Selling, seed buying, order reward calculation, tool costs, and land costs
   agree with the same balance data.
4. A deterministic quote repeats for identical world seed/day/item inputs and
   changes only according to configured bands.
5. The ledger reports all crops and first-week investment targets.
6. Runtime tests prove the critical state mutations and no Unity Console errors
   are present after the test run.
