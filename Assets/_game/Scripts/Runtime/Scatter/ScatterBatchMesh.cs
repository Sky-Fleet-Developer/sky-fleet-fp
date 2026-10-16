using UnityEngine;
using UnityEngine.Rendering;

namespace Runtime.Scatter
{
    /// <summary>
    /// Batch meshes of scattered details. A batch mesh holds one copy of a shape for every instance of its part of the
    /// instance buffer, so all of them are drawn in one draw without instancing; the shader of the kind finds the
    /// instance of a vertex from the vertex id. Copy after copy in the index buffer, so drawing the first n * indices of
    /// the shape draws the first n instances.
    /// (Procedural instancing doesn't work from an HDRP shader graph: its pragma comes after the instancing library.)
    /// </summary>
    public static class ScatterBatchMesh
    {
        /// <summary>The vertex id reaches the shader as a float, exact up to 2^24.</summary>
        public const int MaxVertices = 1 << 24;

        /// <summary>Instances a batch mesh of this shape can hold.</summary>
        public static int FitInstances(Mesh shape, int instances)
        {
            return Mathf.Clamp(instances, 1, MaxVertices / Mathf.Max(1, shape.vertexCount));
        }

        public static int IndicesPerInstance(Mesh shape)
        {
            int count = 0;
            for (int s = 0; s < shape.subMeshCount; s++)
            {
                count += (int)shape.GetIndexCount(s);
            }
            return count;
        }

        /// <summary>
        /// A copy of the shape, all its submeshes in one, for every instance. The vertex color keeps the rgb of the
        /// shape (white without colors); its alpha is the submesh of the vertex, a part of the shape the kind may color
        /// on its own. With <paramref name="withUVs"/>, UV0 and tangents are kept when the shape has UV0 (tangents are
        /// computed when it has none). The shape must be readable.
        /// </summary>
        public static Mesh CreateBatch(Mesh shape, int instances, string name, bool withUVs)
        {
            Vector3[] shapeVertices = shape.vertices;
            Vector3[] shapeNormals = shape.normals;
            Color32[] shapeColors = shape.colors32;
            Vector2[] shapeUVs = withUVs ? shape.uv : System.Array.Empty<Vector2>();
            Vector4[] shapeTangents = withUVs ? shape.tangents : System.Array.Empty<Vector4>();
            int verticesPerInstance = shapeVertices.Length;
            bool hasNormals = shapeNormals.Length == verticesPerInstance;
            bool hasColors = shapeColors.Length == verticesPerInstance;
            bool hasUVs = shapeUVs.Length == verticesPerInstance;
            bool hasTangents = hasUVs && shapeTangents.Length == verticesPerInstance;

            var shapeIndices = new int[IndicesPerInstance(shape)];
            var parts = new byte[verticesPerInstance];
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

            var vertices = new Vector3[verticesPerInstance * instances];
            var normals = new Vector3[vertices.Length];
            var colors = new Color32[vertices.Length];
            Vector2[] uvs = hasUVs ? new Vector2[vertices.Length] : null;
            Vector4[] tangents = hasTangents ? new Vector4[vertices.Length] : null;
            var indices = new int[shapeIndices.Length * instances];
            for (int instance = 0; instance < instances; instance++)
            {
                int firstVertex = instance * verticesPerInstance;
                for (int v = 0; v < verticesPerInstance; v++)
                {
                    vertices[firstVertex + v] = shapeVertices[v];
                    normals[firstVertex + v] = hasNormals ? shapeNormals[v] : Vector3.up;
                    Color32 color = hasColors ? shapeColors[v] : new Color32(255, 255, 255, 255);
                    color.a = parts[v];
                    colors[firstVertex + v] = color;
                    if (hasUVs)
                    {
                        uvs[firstVertex + v] = shapeUVs[v];
                    }
                    if (hasTangents)
                    {
                        tangents[firstVertex + v] = shapeTangents[v];
                    }
                }

                int firstIndex = instance * shapeIndices.Length;
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
            if (hasUVs)
            {
                mesh.SetUVs(0, uvs);
                if (hasTangents)
                {
                    mesh.SetTangents(tangents);
                }
                else
                {
                    mesh.RecalculateTangents();
                }
            }
            // The instances are placed by the shader, the mesh is never culled by its own bounds
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e5f);
            return mesh;
        }
    }
}
