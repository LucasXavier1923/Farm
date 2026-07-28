using UnityEngine;

namespace FarmPrototype.Farming
{
    public sealed class FarmActionFeedback : MonoBehaviour
    {
        private ParticleSystem hoeBurst;
        private ParticleSystem seedBurst;
        private ParticleSystem waterBurst;
        private ParticleSystem harvestBurst;
        private ParticleSystem rewardBurst;
        private ParticleSystem pestBurst;

        public int ToolBurstCount { get; private set; }
        public int RewardBurstCount { get; private set; }
        public int PestBurstCount { get; private set; }
        public FarmTool LastTool { get; private set; }

        public void Initialize()
        {
            if (hoeBurst != null) return;
            var root = new GameObject("Farm_Action_Feedback").transform;
            root.SetParent(transform, false);
            hoeBurst = CreateBurst(root, "Hoe_Dirt", new Color(0.46f, 0.24f, 0.10f), 0.72f, 1.8f, 0.12f, 0.65f);
            seedBurst = CreateBurst(root, "Seed_Spark", new Color(0.66f, 0.94f, 0.24f), 0.62f, 1.4f, 0.09f, 0.30f);
            waterBurst = CreateBurst(root, "Water_Splash", new Color(0.22f, 0.72f, 1f), 0.58f, 2.0f, 0.08f, 0.85f);
            harvestBurst = CreateBurst(root, "Harvest_Leaves", new Color(1f, 0.70f, 0.16f), 0.82f, 2.2f, 0.13f, 0.35f);
            rewardBurst = CreateBurst(root, "Reward_Gold", new Color(1f, 0.84f, 0.18f), 1.0f, 2.6f, 0.15f, 0.12f);
            pestBurst = CreateBurst(root, "Pest_Feathers", new Color(0.16f, 0.12f, 0.20f), 1.15f, 1.8f, 0.13f, 0.08f);
        }

        public void PlayTool(FarmTool tool, Vector3 position, int changedTiles)
        {
            Initialize();
            var system = Resolve(tool);
            if (system == null || changedTiles <= 0) return;
            LastTool = tool;
            ToolBurstCount++;
            system.transform.position = position + Vector3.up * 0.34f;
            var shape = system.shape;
            shape.radius = Mathf.Lerp(0.18f, 1.15f, Mathf.InverseLerp(1f, 9f, changedTiles));
            system.Emit(Mathf.Clamp(5 + changedTiles * 2, 7, 24));
        }

        public void PlayReward(Vector3 position, int intensity = 1)
        {
            Initialize();
            RewardBurstCount++;
            rewardBurst.transform.position = position + Vector3.up * 1.1f;
            rewardBurst.Emit(Mathf.Clamp(10 + intensity * 3, 10, 28));
        }

        public void PlayPest(Vector3 position)
        {
            Initialize();
            PestBurstCount++;
            pestBurst.transform.position = position + Vector3.up * 0.8f;
            pestBurst.Emit(14);
        }

        private ParticleSystem Resolve(FarmTool tool) => tool switch
        {
            FarmTool.Hoe => hoeBurst,
            FarmTool.Seeds => seedBurst,
            FarmTool.WateringCan => waterBurst,
            FarmTool.Harvest => harvestBurst,
            _ => null
        };

        private static ParticleSystem CreateBurst(Transform parent, string objectName, Color color, float lifetime, float speed, float size, float gravity)
        {
            var item = new GameObject(objectName);
            item.transform.SetParent(parent, false);
            var system = item.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.78f, lifetime * 1.22f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.65f, speed * 1.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.65f, size * 1.35f);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = 160;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.22f;
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.80f, 0.55f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;
            var renderer = item.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                renderer.material = new Material(shader) { name = objectName + "_Runtime_Material" };
            }
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return system;
        }
    }
}