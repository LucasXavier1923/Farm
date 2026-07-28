# Fishing Depth and Pond Ecology

## Purpose

Meta 25 upgrades the starter pond from a fixed pickup into a calm, predictable ecology loop. The player can read conditions, choose when to spend a limited daily catch, then decide whether to sell the catch or turn common fish into Energy at the workbench.

## Authoritative catch flow

1. A player starts fishing at the pond during Morning or Dusk.
2. A local host shows the bobber briefly, then calls `ExecuteHostCatch`.
3. A peer sends the transport-neutral `Fishing` intent with `action=catch`.
4. `FarmHostIntentRouter` validates the intent and calls the same host method.
5. The host derives species, quantity, and quality from shared seed/day/catch index/time/weather; only then does it add the persistent pickup and item.

The client never chooses a fish ID, quality, or quantity. Catch IDs are `starter_pond:{day}:{index}`, so duplicate/replayed requests and the daily three-catch limit are rejected consistently after save/load.

## Pond ecology

| Conditions | Likely result | Economy role |
| --- | --- | --- |
| Rain, Morning or Dusk | Rain Carp, at least Silver quality | Weather-timed premium sale route. |
| Spring/Autumn Morning | Dawn Trout | Seasonal morning sale route. |
| Summer/Autumn Dusk | Sunset Perch | Higher-value dusk sale route. |
| Other valid conditions | Pond Fish | Common catch that can be sold or cooked. |

The prompt exposes a condition hint rather than an exact roll. This makes weather and the clock meaningful without forcing a precision minigame or a grind: the pond rests after three catches each farm day.

## Cooking decision

`Fish Skewer` is a localized workbench recipe: one Pond Fish plus one Wood. It restores 28 Energy through the existing host-routed meal boundary. This creates a deliberate split:

- Sell seasonal or rain catches for money.
- Keep a common Pond Fish to recover Energy and continue another shared farm activity.

## Verification

`META25_FISHING_SMOKE` passed in Unity. It validated three host catches, persistent daily-limit rejection, deterministic Rain Carp selection, item catalog loading, the Fish Skewer recipe, and its exact 28-Energy recovery.

## Localization

All player-facing copy is in `Assets/Resources/FarmLocalization/en.json` under `fishing.*`, `item.*`, and `recipe.09_fish_skewer.*`. No translation requires C# changes.
