using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum ItemCategory { Seed, Crop, Tool, Material, Fertilizer, Consumable }

    [CreateAssetMenu(menuName = "Farm/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public ItemCategory Category;
        [Min(1)] public int MaxStack = 99;
        [Min(0)] public int BaseSellPrice;

        public string LocalizedName => FarmLocalization.Get($"item.{Id}.name", DisplayName);
    }
}
