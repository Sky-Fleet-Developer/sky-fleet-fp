using System;
using UnityEngine;

namespace Scripts
{
    [RequireComponent(typeof(MeshFilter)), ExecuteAlways]
    public class QuadMeshCreator : MonoBehaviour
    {
        private void OnValidate()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (!meshFilter.sharedMesh)
            {
                var mesh = new Mesh();
                Vector3[] vertices = { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
                Vector2 [] uv = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                int[] quads = { 0, 1, 2, 3 };
                mesh.vertices = vertices;
                mesh.SetIndices(quads, MeshTopology.Quads, 0);
                mesh.uv = uv;
                mesh.RecalculateNormals();
                meshFilter.sharedMesh = mesh;
            }
        }
    }
}
