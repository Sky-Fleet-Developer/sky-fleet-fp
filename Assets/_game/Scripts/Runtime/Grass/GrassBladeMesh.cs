using UnityEngine;

namespace Runtime.Grass
{
    /// <summary>
    /// The built-in grass blade, in shape space (Core.Grass.GrassShape): x across, y up from 0 to 1, z forward.
    /// Batch meshes of it are made by Runtime.Scatter.ScatterBatchMesh.
    /// </summary>
    internal static class GrassBladeMesh
    {
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
    }
}
