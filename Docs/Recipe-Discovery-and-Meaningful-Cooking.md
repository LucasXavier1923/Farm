# Recipe Discovery and Meaningful Cooking

## Purpose

Meta 27 makes food a shared farm-planning layer instead of a list of recipes visible from the first minute. A recipe is learned when the farm first discovers one of its meaningful ingredients. The discovery is stored in the shared save and checked by the same host-owned crafting and consumption boundaries used by every other player.

## Discovery rules

- A `CraftingRecipe` can declare `RequiresDiscovery`.
- When an item is first added to the farm's discovered-item collection, matching recipes become discovered.
- Old saves migrate safely: known ingredients are scanned after restore and unlock their matching recipes.
- Recipe IDs are persisted in save version 30.
- A recipe remains locked to both UI and `TryCraft` until discovery succeeds; a client cannot craft it by sending a raw recipe ID.

## Food sources and day-planning choices

| Source loop | Recipe | Cost | Energy |
| --- | --- | --- | --- |
| Foraging | Wildflower Tea | Wildflower + Wood | 18 |
| Foraging | Mushroom Stew | 2 Forest Mushroom + Wood | 35 |
| Fishing | Fish Skewer | Pond Fish + Wood | 28 |
| Farming | Pumpkin Roast | Pumpkin + Wood | 26 |
| Animal care + foraging | Farm Omelet | Chicken Egg + Wildflower | 42 |

This gives a co-op day genuine decisions. A Wildflower can become tea, an omelet ingredient, a chicken treat, or a community gift. Pond Fish can be sold or prepared. An Egg can be delivered to the request board or converted into the strongest current meal.

## Authority and localization

Crafting still routes through `Production`, and consuming a meal still routes through `Consumption`; this meta does not introduce a client-side inventory mutation. Player-facing labels, locked messages, and descriptions are in `Assets/Resources/FarmLocalization/en.json` under `crafting.*`, `state.crafting.*`, `item.*`, and `recipe.*`.

## Verification

`META27_RECIPE_SMOKE` passed in Unity. It rejected an undiscovered recipe, discovered meals through crop/forage/fish/egg sources, crafted Pumpkin Roast, restored exactly 26 Energy, and preserved discovery through save/restore.
