# Meta 7 - Seasonal Planner

`FarmSeasonalPlanner` is a presentation-only `P` panel. It reads the current
`FarmDayClock` and `CropDefinition` list every refresh, so a future host
snapshot correction is reflected without local planner state. It does not pause
`FarmSessionTime`.

The player sees each crop, its preferred season, and either `PLANT OUTDOORS` or
`GREENHOUSE REQUIRED`. The greenhouse message describes the actual current
rule: coverage is evaluated from durable placed-object data at both planting and
harvest.
