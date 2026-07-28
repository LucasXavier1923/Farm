using UnityEngine;

namespace FarmPrototype.Farming
{
    public readonly struct FarmExplorationRoute
    {
        public readonly string PickupId;
        public readonly string ItemId;
        public readonly int Quantity;
        public readonly int EnergyCost;
        public readonly string RouteKey;
        public FarmExplorationRoute(string pickupId, string itemId, int quantity, int energyCost, string routeKey)
        {
            PickupId = pickupId; ItemId = itemId; Quantity = quantity; EnergyCost = energyCost; RouteKey = routeKey;
        }

        public FarmExplorationRoute WithForecastPreparation() => new(PickupId, ItemId, Quantity + FarmForecastPlanRules.RouteQuantityBonus, Mathf.Max(0, EnergyCost - FarmForecastPlanRules.RouteEnergyDiscount), RouteKey);
    }

    /// <summary>One deterministic, time-window route per day at most.</summary>
    public static class FarmExplorationRouteCatalog
    {
        public static bool TryGetRoute(int worldSeed, int day, FarmSeason season, FarmWeather weather, FarmDayPhase phase, out FarmExplorationRoute route)
        {
            day = Mathf.Max(1, day);
            var routeKey = weather == FarmWeather.Rain && phase == FarmDayPhase.Dusk ? "rainy_dusk" :
                weather == FarmWeather.Clear && phase == FarmDayPhase.Morning ? "sunrise_clear" :
                weather == FarmWeather.Cloudy && phase == FarmDayPhase.Afternoon ? "cloudy_afternoon" : string.Empty;
            if (string.IsNullOrEmpty(routeKey)) { route = default; return false; }
            var itemId = routeKey == "rainy_dusk" ? "forest_mushroom" : routeKey == "sunrise_clear" ? "wildflower" : "wood";
            var quantity = routeKey == "rainy_dusk" ? 2 : 2;
            route = new FarmExplorationRoute($"route:{day}:{routeKey}:{itemId}", itemId, quantity, 8, routeKey);
            return true;
        }
    }
}
