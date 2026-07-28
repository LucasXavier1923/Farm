using UnityEngine;

namespace FarmPrototype.Farming
{
    [CreateAssetMenu(menuName = "Farm/Crop Definition", fileName = "CropDefinition")]
    public sealed class CropDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public ItemDefinition SeedItem;
        public ItemDefinition HarvestItem;
        public GameObject SmallModel;
        public GameObject MediumModel;
        public GameObject LargeModel;
        [Min(0.1f)] public float GrowthSeconds = 10f;
        [Min(1)] public int HarvestYield = 1;
        public FarmSeason PreferredSeason = FarmSeason.Spring;
        [Min(0)] public int PreferredSeasonYieldBonus = 1;
        [Min(1)] public int SeedPackAmount = 5;
        [Min(0)] public int SeedPackPrice = 20;

        public string LocalizedName => FarmLocalization.Get($"crop.{Id}.name", DisplayName);
        public bool IsPreferredSeason(FarmSeason season) => season == PreferredSeason;
        public int HarvestYieldForSeason(FarmSeason season) =>
            HarvestYield + (IsPreferredSeason(season) ? Mathf.Max(0, PreferredSeasonYieldBonus) : 0);
        public string AffinityText => FarmLocalization.Format("crop.affinity", FarmDayClock.SeasonName(PreferredSeason), Mathf.Max(0, PreferredSeasonYieldBonus));
    }
}