using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmBuildableFunction { Decorative, Sprinkler, Scarecrow, Fence, StorageShed, Greenhouse }
    public enum FarmBuildableCategory { Farming, Automation, Fences, Decoration, Utility }

    [CreateAssetMenu(menuName = "Farm/Buildable Definition", fileName = "BuildableDefinition")]
    public sealed class FarmBuildableDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public FarmBuildableCategory Category;
        [TextArea] public string Description;
        public ItemDefinition KitItem;
        public GameObject Prefab;
        public Vector3 Footprint = Vector3.one;
        public Vector3 PlacedScale = Vector3.one;
        public float GroundOffset;
        public FarmBuildableFunction Function;
        [Min(0f)] public float EffectRadius;
        [Min(1f)] public float RotationStep = 45f;

        public string LocalizedName => FarmLocalization.Get($"buildable.{Id}.name", DisplayName);
        public string LocalizedDescription => FarmLocalization.Get($"buildable.{Id}.description", Description);
    }

    public static class FarmBuildableDatabase
    {
        private static List<FarmBuildableDefinition> definitions;

        public static IReadOnlyList<FarmBuildableDefinition> Definitions
        {
            get
            {
                if (definitions == null)
                {
                    definitions = new List<FarmBuildableDefinition>(
                        Resources.LoadAll<FarmBuildableDefinition>("GameData/Buildables"));
                    definitions.RemoveAll(definition =>
                        definition == null || string.IsNullOrWhiteSpace(definition.Id) ||
                        definition.KitItem == null || definition.Prefab == null);
                    definitions.Sort((left, right) =>
                        string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase));
                }
                return definitions;
            }
        }

        public static FarmBuildableDefinition GetByItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return null;
            foreach (var definition in Definitions)
                if (string.Equals(definition.KitItem.Id, itemId, StringComparison.OrdinalIgnoreCase))
                    return definition;
            return null;
        }

        public static void Reload() => definitions = null;
    }
}
