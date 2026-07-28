using System;

namespace FarmPrototype.Farming
{
    /// <summary>Shared overnight comfort state. One Evening Tea can prepare the next day.</summary>
    [Serializable]
    public sealed class FarmHomesteadRestProgress
    {
        public int PreparedOnDay;
        public int ComfortDay;
        public int ComfortCharges;

        public FarmHomesteadRestProgress Clone() => new()
        {
            PreparedOnDay = PreparedOnDay,
            ComfortDay = ComfortDay,
            ComfortCharges = ComfortCharges
        };
    }

    public static class FarmHomesteadRestRules
    {
        public const string EveningTeaItemId = "wildflower_tea";
        public const int ComfortCharges = 3;
        public const int EnergyDiscountPerCharge = 1;
    }
}
