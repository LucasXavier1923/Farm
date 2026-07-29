using UnityEngine;

namespace FarmPrototype.Farming
{
    public static class FarmPestRules
    {
        public const int FirstVisitDay = 3;
        public const int VisitIntervalDays = 3;
        public const float GrowthDelaySeconds = 2f;
        public const float DefaultScarecrowRadius = 6.5f;
        private static bool? developerVisitOverride;

        public static bool HasDeveloperVisitOverride => developerVisitOverride.HasValue;
        public static bool? DeveloperVisitOverride => developerVisitOverride;

        public static bool IsVisitDay(int day) =>
            developerVisitOverride ?? (day >= FirstVisitDay && (day - FirstVisitDay) % VisitIntervalDays == 0);

        /// <summary>Overrides the normal pest calendar for local development runs.</summary>
        public static void SetDeveloperVisitOverride(bool? enabled) => developerVisitOverride = enabled;

        public static string ForecastText(int day)
        {
            day = Mathf.Max(1, day);
            if (IsVisitDay(day)) return FarmLocalization.Get("pests.today", "CROWS TODAY  ?  scarecrows protect crops");
            if (IsVisitDay(day + 1)) return FarmLocalization.Get("pests.tomorrow", "CROWS TOMORROW  ?  prepare scarecrows");
            return FarmLocalization.Get("pests.calm", "PESTS CALM  ?  no visit tomorrow");
        }
    }
}
