using System;
using UnityEngine;

namespace FarmPrototype.Farming
{
    [Serializable]
    public sealed class FarmForecastPlan
    {
        public int TargetDay;
        public string RouteKey;

        public FarmForecastPlan Clone() => new() { TargetDay = TargetDay, RouteKey = RouteKey };
    }

    /// <summary>
    /// A single optional shared commitment made from the known next-day forecast.
    /// It improves an existing route; it never removes the normal route or weather reward.
    /// </summary>
    public static class FarmForecastPlanRules
    {
        public const int RouteQuantityBonus = 1;
        public const int RouteEnergyDiscount = 2;

        public static string RouteKeyForWeather(FarmWeather weather) => weather switch
        {
            FarmWeather.Rain => "rainy_dusk",
            FarmWeather.Clear => "sunrise_clear",
            FarmWeather.Cloudy => "cloudy_afternoon",
            _ => string.Empty
        };

        public static string Description(string routeKey) => routeKey switch
        {
            "rainy_dusk" => FarmLocalization.Get("forecast.plan.rain", "Rainy Forage: tomorrow's dusk mushroom route gains +1 item and costs 2 less Energy."),
            "sunrise_clear" => FarmLocalization.Get("forecast.plan.clear", "Sunrise Walk: tomorrow's clear-morning wildflower route gains +1 item and costs 2 less Energy."),
            "cloudy_afternoon" => FarmLocalization.Get("forecast.plan.cloudy", "Cloudy Timber Walk: tomorrow's afternoon wood route gains +1 item and costs 2 less Energy."),
            _ => FarmLocalization.Get("forecast.plan.unavailable", "No route preparation is available for that forecast.")
        };
    }
}
