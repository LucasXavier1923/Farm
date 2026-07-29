# Developer Console

Updated: 29 July 2026.

## Purpose

The developer console accelerates local feature tests while the farm world is
still a prototype. It is not a player feature and it is intentionally not a
network, economy, or persistence authority. Every action changes the same local
host state used by the farm.

## Availability

- Toggle: `H`.
- Close: `Esc` or the **Close** button.
- Available only in the Unity Editor and Development Builds.
- It is excluded from release builds through compile-time guards.
- Opening it does **not** pause the farm clock. This preserves the host-led
  co-op rule that menus never stop shared time.

## Controls

| Area | Controls | Test purpose |
| --- | --- | --- |
| Time & day | +1 hour, fixed 00:00/06:00/12:00/18:00, next day, x1/x10/stop clock | Lighting, morning automation, night behavior and daily loops. |
| Season | Spring, Summer, Autumn, Winter | Seasonal crops, planning and presentation. |
| Weather | Sunny, Cloudy, Rain, return to automatic daily weather | Rain effect, lighting, automatic watering and forecast behavior. |
| Pests | Force on, force off, automatic calendar, trigger now | Crop protection and scarecrow behavior without waiting for a calendar day. |
| Player & crops | Add $100/$1000, restore energy, water all, grow +30 seconds/+5 minutes | Progression gates, energy limits and crop-state transitions. |
| Item spawner | Previous/next through every `ItemDefinition`, add 1 or 10 | Inventory, hotbar, crafting, buildings and all registered content. |

## Implementation boundaries

- `FarmDeveloperPanel` owns only development UI and invokes explicit debug
  APIs; it has no production gameplay authority.
- `FarmWeatherSystem` exposes a temporary local weather override. Returning to
  **Auto Weather** restores deterministic weather from the current world seed
  and day.
- `FarmPestRules` exposes a temporary local visit override. Returning to
  **Auto Pests** restores the regular calendar.
- Item spawning uses `FarmGameState.AddItem`, so normal inventory capacity
  still applies. The console clearly reports an item when no room remains.
- Time, season and morning actions use `FarmDayClock` and the normal farm
  automation path, rather than modifying visual UI alone.

## Localization

All visible console text is retrieved through `FarmLocalization.Get(key,
fallback)`. English is the current source language; adding a future locale
requires translation data rather than code edits.

## Validation checklist

1. Enter Play Mode and press `H`.
2. Verify the day clock continues moving with the panel open.
3. Force rain, verify rain/lighting, then choose **Auto Weather**.
4. Force pests and use **Trigger Now** on a planted crop.
5. Advance to the next day and verify morning automation.
6. Spawn a seed or material, open Inventory, then drag it to the hotbar.
7. Close with `Esc`; release builds must not expose the panel.
