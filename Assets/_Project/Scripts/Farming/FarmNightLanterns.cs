using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Disposable testbed lighting. It verifies that the shared farm clock can
    /// drive practical night visibility without adding collision or gameplay
    /// state. Final-world artists may replace these visuals freely.
    /// </summary>
    public sealed class FarmNightLanterns : MonoBehaviour
    {
        private readonly List<Light> lights = new();
        private readonly List<Renderer> flames = new();
        private Material postMaterial;
        private Material flameMaterial;
        private FarmDayClock clock;
        private bool initialized;
        private float appliedIntensity = -1f;

        public int LanternCount => lights.Count;
        public bool AreLit { get; private set; }

        public void Initialize(FarmTestPlot owner)
        {
            if (initialized || owner == null) return;
            initialized = true;
            clock = owner.DayClock;
            var field = owner.PlotCenter;
            var forward = owner.PlotForward.sqrMagnitude > 0.01f ? owner.PlotForward.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward).normalized;

            // These are deliberately sparse visibility probes, not a designed
            // environment layout. They have no colliders and no save data.
            CreateLantern(field + right * 5f + forward * 4f);
            CreateLantern(field - right * 5f + forward * 4f);
            CreateLantern(field + right * 5f - forward * 4f);
            CreateLantern(field - right * 5f - forward * 4f);
            RefreshLighting();
        }

        private void Update() => RefreshLighting();

        private void CreateLantern(Vector3 position)
        {
            var lantern = new GameObject("Night_Lighting_Test_Lantern").transform;
            lantern.SetParent(transform, true);
            lantern.position = new Vector3(position.x, 0.02f, position.z);

            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Light_Post";
            post.transform.SetParent(lantern, false);
            post.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            post.transform.localScale = new Vector3(0.09f, 0.55f, 0.09f);
            Destroy(post.GetComponent<Collider>());
            post.GetComponent<Renderer>().sharedMaterial = GetPostMaterial();

            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Light_Source";
            flame.transform.SetParent(lantern, false);
            flame.transform.localPosition = new Vector3(0f, 1.12f, 0f);
            flame.transform.localScale = Vector3.one * 0.16f;
            Destroy(flame.GetComponent<Collider>());
            var renderer = flame.GetComponent<Renderer>();
            renderer.sharedMaterial = GetFlameMaterial();
            flames.Add(renderer);

            var light = lantern.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.68f, 0.28f);
            light.range = 8.5f;
            light.shadows = LightShadows.None;
            lights.Add(light);
        }

        private void RefreshLighting()
        {
            if (clock == null || lights.Count == 0) return;
            var intensity = clock.Phase switch
            {
                FarmDayPhase.Night => 3.1f,
                FarmDayPhase.Dusk => 1.85f,
                FarmDayPhase.Dawn => 1.25f,
                _ => 0f
            };
            var lit = intensity > 0f;
            if (AreLit == lit && Mathf.Approximately(appliedIntensity, intensity)) return;
            AreLit = lit;
            appliedIntensity = intensity;
            foreach (var light in lights)
                if (light != null) light.intensity = intensity;
            foreach (var flame in flames)
                if (flame != null) flame.enabled = lit;
        }

        private Material GetPostMaterial()
        {
            if (postMaterial != null) return postMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            postMaterial = new Material(shader) { name = "NightLightingPost_Runtime" };
            SetMaterialColor(postMaterial, new Color(0.18f, 0.10f, 0.05f));
            return postMaterial;
        }

        private Material GetFlameMaterial()
        {
            if (flameMaterial != null) return flameMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            flameMaterial = new Material(shader) { name = "NightLightingFlame_Runtime" };
            SetMaterialColor(flameMaterial, new Color(1f, 0.48f, 0.10f));
            if (flameMaterial.HasProperty("_EmissionColor")) flameMaterial.SetColor("_EmissionColor", new Color(1f, 0.22f, 0.03f));
            return flameMaterial;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
        }

        private void OnDestroy()
        {
            if (postMaterial != null) Destroy(postMaterial);
            if (flameMaterial != null) Destroy(flameMaterial);
        }
    }
}
