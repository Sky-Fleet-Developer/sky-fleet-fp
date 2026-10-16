using UnityEngine;
using UnityEngine.Rendering;

namespace Runtime.Grass
{
    /// <summary>
    /// Blade meshes holding one blade for every slot of a blade buffer, so all blades are drawn in one draw without
    /// instancing. Positions are in blade space: x across (-0.5..0.5, tapering to the tip), y along the blade (0 root,
    /// 1 tip), z the slot. The blade shader (Grass.hlsl) reads the blade of the slot and places the vertex.
    /// Slot after slot in the index buffer, so drawing the first n * <see cref="IndicesPerBlade"/> indices draws the
    /// first n blades.
    /// </summary>
    internal static class GrassBladeMesh
    {
        public static int IndicesPerBlade(int segments) => (Mathf.Max(1, segments) - 1) * 6 + 3;

        /// <param name="segments">1: a single triangle</param>
        public static Mesh Create(int segments, int blades, string name)
        {
            segments = Mathf.Max(1, segments);
            int verticesPerBlade = segments * 2 + 1;
            int indicesPerBlade = IndicesPerBlade(segments);

            // One blade, repeated for every slot
            var blade = new Vector2[verticesPerBlade];
            for (int k = 0; k < segments; k++)
            {
                float t = (float)k / segments;
                float halfWidth = 0.5f * (1 - t * t);
                blade[k * 2] = new Vector2(-halfWidth, t);
                blade[k * 2 + 1] = new Vector2(halfWidth, t);
            }
            int tip = segments * 2;
            blade[tip] = new Vector2(0, 1);

            var bladeIndices = new int[indicesPerBlade];
            int index = 0;
            for (int k = 0; k < segments - 1; k++)
            {
                int left = k * 2;
                bladeIndices[index++] = left;
                bladeIndices[index++] = left + 2;
                bladeIndices[index++] = left + 1;
                bladeIndices[index++] = left + 1;
                bladeIndices[index++] = left + 2;
                bladeIndices[index++] = left + 3;
            }
            bladeIndices[index++] = tip - 2;
            bladeIndices[index++] = tip;
            bladeIndices[index] = tip - 1;

            var vertices = new Vector3[verticesPerBlade * blades];
            var indices = new int[indicesPerBlade * blades];
            for (int slot = 0; slot < blades; slot++)
            {
                int firstVertex = slot * verticesPerBlade;
                for (int v = 0; v < verticesPerBlade; v++)
                {
                    vertices[firstVertex + v] = new Vector3(blade[v].x, blade[v].y, slot);
                }

                int firstIndex = slot * indicesPerBlade;
                for (int i = 0; i < indicesPerBlade; i++)
                {
                    indices[firstIndex + i] = firstVertex + bladeIndices[i];
                }
            }

            var mesh = new Mesh
            {
                name = name,
                indexFormat = IndexFormat.UInt32,
                hideFlags = HideFlags.DontSave,
            };
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
            // The blades are placed by the shader, the mesh is never culled by its own bounds
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e5f);
            return mesh;
        }
    }
}
