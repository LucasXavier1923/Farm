using System;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Small, data-neutral soil rule set. Rotation rewards a different crop in the
    /// same plot without penalizing a player who chooses to repeat a crop.
    /// </summary>
    public static class FarmSoilRules
    {
        public static bool IsRotation(string previousHarvestCropId, string nextCropId) =>
            !string.IsNullOrWhiteSpace(previousHarvestCropId) &&
            !string.IsNullOrWhiteSpace(nextCropId) &&
            !string.Equals(previousHarvestCropId, nextCropId, StringComparison.OrdinalIgnoreCase);
    }
}
