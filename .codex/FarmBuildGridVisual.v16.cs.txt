using UnityEngine;

namespace FarmPrototype.Farming
{
    public sealed class FarmBuildGridVisual : MonoBehaviour
    {
        private const int HalfCells = 10;
        private Mesh gridMesh;
        private Material gridMaterial;
        private MeshRenderer meshRenderer;
        private float cellSize;

        public bool IsVisible => meshRenderer != null && meshRenderer.enabled;
        public int LineSegmentCount => gridMesh != null ? gridMesh.vertexCount / 2 : 0;

        public void Initialize(float size)
        {
            if (gridMesh != null) return;
            cellSize = Mathf.Max(0.1f, size);
            var filter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            gridMesh = new Mesh { name = "FarmBuildGrid_Runtime" };

            var lineCountPerAxis = HalfCells * 2 + 1;
            var vertices = new Vector3[lineCountPerAxis * 4];
            var indices = new int[vertices.Length];
            var halfExtent = HalfCells * cellSize;
            var cursor = 0;
            for (var offset = -HalfCells; offset <= HalfCells; offset++)
            {
                var coordinate = offset * cellSize;
                vertices[cursor] = new Vector3(-halfExtent, 0f, coordinate);
                indices[cursor] = cursor++;
                vertices[cursor] = new Vector3(halfExtent, 0f, coordinate);
                indices[cursor] = cursor++;
                vertices[cursor] = new Vector3(coordinate, 0f, -halfExtent);
                indices[cursor] = cursor++;
                vertices[cursor] = new Vector3(coordinate, 0f, halfExtent);
                indices[cursor] = cursor++;
            }
            gridMesh.SetVertices(vertices);
            gridMesh.SetIndices(indices, MeshTopology.Lines, 0);
            gridMesh.RecalculateBounds();
            filter.sharedMesh = gridMesh;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader != null)
            {
                gridMaterial = new Material(shader) { name = "FarmBuildGrid_Material_Runtime" };
                var color = new Color(0.58f, 0.92f, 0.48f, 0.20f);
                if (gridMaterial.HasProperty("_BaseColor")) gridMaterial.SetColor("_BaseColor", color);
                if (gridMaterial.HasProperty("_Color")) gridMaterial.color = color;
                if (gridMaterial.HasProperty("_Surface")) gridMaterial.SetFloat("_Surface", 1f);
                if (gridMaterial.HasProperty("_SrcBlend"))
                    gridMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (gridMaterial.HasProperty("_DstBlend"))
                    gridMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (gridMaterial.HasProperty("_ZWrite")) gridMaterial.SetFloat("_ZWrite", 0f);
                gridMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                gridMaterial.renderQueue = 3000;
                meshRenderer.sharedMaterial = gridMaterial;
            }
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = false;
        }

        public void SetVisible(bool visible)
        {
            if (meshRenderer != null) meshRenderer.enabled = visible;
        }

        public void SetCenter(Vector3 position)
        {
            if (cellSize <= 0f) return;
            transform.position = new Vector3(
                Mathf.Round(position.x / cellSize) * cellSize,
                position.y + 0.035f,
                Mathf.Round(position.z / cellSize) * cellSize);
        }

        private void OnDestroy()
        {
            if (gridMesh != null) Destroy(gridMesh);
            if (gridMaterial != null) Destroy(gridMaterial);
        }
    }
}
