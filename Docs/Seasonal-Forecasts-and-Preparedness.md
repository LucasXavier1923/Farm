# Seasonal Forecasts and Preparedness Choices

## Purpose

Meta 38 turns the existing next-day weather forecast into a small optional planning decision. Rain, clear skies, and clouds already influence watering, fishing, forage, lighting, pest preparation, and exploration. The new Forecast Plan lets the crew commit to tomorrow's matching exploration route from the Seasonal Planner.

The choice rewards attention without turning weather into punishment: ignoring the plan leaves every normal route and farming activity unchanged.

## Player flow

1. Open the Seasonal Planner with `P`.
2. Review the known weather for tomorrow.
3. Select **Prepare Tomorrow's Route**.
4. On the next day, visit the route during its normal time window.

The plan is shared for the farm and targets the deterministic route already associated with the forecast.

| Tomorrow's weather | Normal route | Prepared benefit |
| --- | --- | --- |
| Rain | Dusk Mushroom route | +1 item and 2 less Energy |
| Clear | Morning Wildflower route | +1 item and 2 less Energy |
| Cloudy | Afternoon Wood route | +1 item and 2 less Energy |

Normal routes give 2 items for 8 Energy. A prepared route gives 3 items for 6 Energy. The route remains optional and its normal time restriction still applies.

## Co-op and authority

- `FarmForecastPlan` is a shared save record with a target day and stable route key.
- The host and solo game use `TryPrepareTomorrowForecastPlan`.
- A peer sends the transport-neutral `ForecastPlan` intent; `FarmHostIntentRouter` performs the same host-side validation and state mutation.
- World pickup construction reads the saved plan only at the intended day and route key. It does not trust a client-provided quantity or Energy cost.
- The plan automatically expires once the target day has passed, preventing stale bonuses after sleep, reconnect, or save/load.

## Persistence

Save version 35 adds the `ForecastPlan` record. Earlier saves load with no plan. The record contains no item or currency grant, only the host-evaluated preparation decision.

## Localization

Forecast and route-planning copy is localized through `forecast.*` keys in `Assets/Resources/FarmLocalization/en.json`. The route keys (`rainy_dusk`, `sunrise_clear`, and `cloudy_afternoon`) are stable gameplay identifiers and are never shown to the player.

## Validation

`META38_FORECAST_SMOKE` created a plan, saved/restored it, advanced to the target day, resolved its matching deterministic route, verified the quantity and Energy changes, then advanced one more day and verified expiry.

```text
passed=True prepared=True route=sunrise_clear weather=Clear active=True quantity=3 energy=6 expired=True
```
