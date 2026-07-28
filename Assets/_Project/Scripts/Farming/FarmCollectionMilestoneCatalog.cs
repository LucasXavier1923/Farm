namespace FarmPrototype.Farming
{
    public readonly struct FarmCollectionMilestone
    {
        public readonly int Threshold; public readonly string RewardItemId; public readonly int RewardAmount;
        public FarmCollectionMilestone(int threshold, string rewardItemId, int rewardAmount) { Threshold = threshold; RewardItemId = rewardItemId; RewardAmount = rewardAmount; }
    }
    public static class FarmCollectionMilestoneCatalog
    {
        private static readonly FarmCollectionMilestone[] Milestones = { new(8, "compost", 2), new(16, "animal_feed", 3), new(24, "refined_stone", 2) };
        public static int Count => Milestones.Length;
        public static FarmCollectionMilestone Get(int index) => index >= 0 && index < Milestones.Length ? Milestones[index] : default;
    }
}
