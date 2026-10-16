using UnityEngine;
using UnityEngine.Rendering;

namespace Runtime.Grass
{
    /// <summary>
    /// Plant meshes. Shapes are in shape space (Core.Grass.GrassShape): x across, y up from 0 to 1, z forward. A batch
    /// mesh holds one copy of a shape for every plant of its part of the plant buffer, so all of them are drawn in one
    /// draw without instancing; the blade shader (Grass.hlsl) finds the plant of a vertex from the vertex id. Copy
    /// after copy in the index buffer, so drawing the first n * indices of the shape draws the first n plants.
    /// </summary>
    internal static class GrassBladeMesh
    {
        /// <summary>The vertex id reaches the shader as a float, exact up to 2^24.</summary>
        public const int MaxVertices = 1 << 24;

        /// <summary>The built-in blade: tapering, bent in the shader. 1 segment: a single triangle.</summary>
        public static Mesh CreateBlade(int segments, string name)
        {
            segments = Mathf.Max(1, segments);
            var vertices = new Vector3[segments * 2 + 1];
            for (int k = 0; k < segments; k++)
            {
                float t = (float)k / segments;
                float halfWidth = 0.5f * (1 - t * t);
                vertices[k * 2] = new Vector3(-halfWidth, t, 0);
                vertices[k * 2 + 1] = new Vector3(halfWidth, t, 0);
            }
            int tip = segments * 2;
            vertices[tip] = new Vector3(0, 1, 0);

            var indices = new int[(segments - 1) * 6 + 3];
            int index = 0;
            for (int k = 0; k < segments - 1; k++)
            {
                int left = k * 2;
                indices[index++] = left;
                indices[index++] = left + 2;
                indices[index++] = left + 1;
                indices[index++] = left + 1;
                indices[index++] = left + 2;
                indices[index++] = left + 3;
            }
            indices[index++] = tip - 2;
            indices[index++] = tip;
            indices[index] = tip - 1;

            // Rounded across the blade, facing back against the lean
            var normals = new Vector3[vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                normals[v] = new Vector3(vertices[v].x * 2, 0.5f, -1).normalized;
            }

            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(indices, 0);
            return mesh;
        }

        /// <summary>Plants a batch mesh of this shape can hold.</summary>
        public static int FitPlants(Mesh shape, int plants)
        {
            return Mathf.Clamp(plants, 1, MaxVertices / Mathf.Max(1, shape.vertexCount));
        }

        public static int IndicesPerPlant(Mesh shape)
        {
            int count = 0;
            for (int s = 0; s < shape.subMeshCount; s++)
            {
                count += (int)shape.GetIndexCount(s);
            }
            return count;
        }

        /// <summary>
        /// A copy of the shape, all its submeshes in one, for every plant. The vertex color keeps the rgb of the shape
        /// (white without colors); its alpha is the submesh of the vertex, the part of the shape a grass layer may give
        /// its own color. The shape must be readable.
        /// </summary>
        public static Mesh CreateBatch(Mesh shape, int plants, string name)
        {
            Vector3[] shapeVertices = shape.vertices;
            Vector3[] shapeNormals = shape.normals;
            Color32[] shapeColors = shape.colors32;
            int verticesPerPlant = shapeVertices.Length;
            bool hasNormals = shapeNormals.Length == verticesPerPlant;
            bool hasColors = shapeColors.Length == verticesPerPlant;

            var shapeIndices = new int[IndicesPerPlant(shape)];
            var parts = new byte[verticesPerPlant];
            int filled = 0;
            for (int s = 0; s < shape.subMeshCount; s++)
            {
                int[] submesh = shape.GetIndices(s);
                submesh.CopyTo(shapeIndices, filled);
                filled += submesh.Length;
                foreach (int vertex in submesh)
                {
                    parts[vertex] = (byte)Mathf.Min(s, 255);
                }
            }

            var vertices = new Vector3[verticesPerPlant * plants];
            var normals = new Vector3[vertices.Length];
            var colors = new Color32[vertices.Length];
            var indices = new int[shapeIndices.Length * plants];
            for (int plant = 0; plant < plants; plant++)
            {
                int firstVertex = plant * verticesPerPlant;
                for (int v = 0; v < verticesPerPlant; v++)
                {
                    vertices[firstVertex + v] = shapeVertices[v];
                    normals[firstVertex + v] = hasNormals ? shapeNormals[v] : Vector3.up;
                    Color32 color = hasColors ? shapeColors[v] : new Color32(255, 255, 255, 255);
                    color.a = parts[v];
                    colors[firstVertex + v] = color;
                }

                int firstIndex = plant * shapeIndices.Length;
                for (int i = 0; i < shapeIndices.Length; i++)
                {
                    indices[firstIndex + i] = firstVertex + shapeIndices[i];
                }
            }

            var mesh = new Mesh
            {
                name = name,
                indexFormat = IndexFormat.UInt32,
                hideFlags = HideFlags.DontSave,
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
            // The plants are placed by the shader, the mesh is never culled by its own bounds
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e5f);
            return mesh;
        }
    }
}
