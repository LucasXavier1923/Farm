# Exploration Routes, Risk, and Reward

## Purpose

Exploration now has a compact planning layer without requiring an open-world map, combat, or decorative travel. A daily route can appear at a specific weather-and-time window, at the far edge of the playable farm, and trades 8 Energy for a useful deterministic reward.

## Route Windows

| Weather | Time window | Reward | Connection |
| --- | --- | --- | --- |
| Rain | Dusk | 2 Forest Mushrooms | stew, Clover's favorite, forage economy |
| Clear | Morning | 2 Wildflowers | tea, Pip's favorite, animal care |
| Cloudy | Afternoon | 2 Wood | building, coop expansion, projects |

The route refreshes when the shared day phase changes. If it is not collected before the window closes, that opportunity is gone for the day; if collected, its persistent pickup ID prevents duplication across rebuilds, save/load, and peer snapshots.

## Cost and Authority

- A route pickup checks inventory capacity first, then requires 8 shared Energy before granting its reward.
- `FarmWorldPickup` remains host-authoritative; peers see the opportunity but wait for host confirmation rather than receiving the item locally.
- `FarmExplorationRouteCatalog` is deterministic from day, weather, and phase. It contains no local random state, so all peers can render the same route.
- Standard forage remains free and broadly available. Routes are optional high-value choices for players with energy and room to plan around the clock.

## Validation

`META34_EXPLORATION_SMOKE` passed. It found a deterministic rainy-day seed, confirmed the Dusk Mushroom route, rejected the same route outside its time window, verified repeat generation, consumed exactly 8 Energy, and rejected insufficient Energy.
