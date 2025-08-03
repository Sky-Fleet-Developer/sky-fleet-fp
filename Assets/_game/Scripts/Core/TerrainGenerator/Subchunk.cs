using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.TerrainGenerator
{
    public class Subchunk
    {
        private class View
        {
            public Mesh Mesh
            {
                get => meshFilter?.sharedMesh;
                set
                {
                    meshFilter.sharedMesh = value;
                    collider.sharedMesh = value;
                }
            }
            public Transform transform;
            public MeshRenderer renderer;
            public MeshCollider collider;
            public MeshFilter meshFilter;

            public View(string name, Material material)
            {
                transform = new GameObject(name).transform;
                renderer = transform.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                meshFilter = transform.gameObject.AddComponent<MeshFilter>();
                collider = transform.gameObject.AddComponent<MeshCollider>();
            }
        }
        
        private static Dictionary<int, Queue<View>> _pool
            = new Dictionary<int, Queue<View>>();

        private View view;
        private readonly float height;
        private readonly int resolution;
        private readonly float size;
        private readonly int key;
        private readonly Vector2Int localCoordinates;
        private readonly int piecesAmount;
        private Vector3[] vertices;
        private Vector2Int minCoverage;
        private Vector2Int maxCoverage;
        public Task GenerationTask { get; private set; }

        public Vector3 Position
        {
            get => view.transform.position;
            set => view.transform.position = value + new Vector3(localCoordinates.x * size, 0, localCoordinates.y * size);
        }

        public void SetMinMaxCoverage(Vector2Int min, Vector2Int max)
        {
            minCoverage = min;
            maxCoverage = max;
        }

        public Subchunk(string name, Transform parent, float size, float height, int resolution, Vector2Int localCoordinates, int piecesAmount, Material material)
        {
            this.height = height;
            this.resolution = resolution;
            this.size = size;
            this.localCoordinates = localCoordinates;
            this.piecesAmount = piecesAmount;
            key = resolution * (int) size;

            if (!TryAssignViewFromPool(name, material))
            {
                MakeNewView(name, material);
            }

            view.transform.gameObject.layer = parent.gameObject.layer;
            view.transform.SetParent(parent);
        }

        private bool TryAssignViewFromPool(string name, Material material)
        {
            if (!_pool.TryGetValue(key, out Queue<View> views)) return false;
            if (views.Count <= 0) return false;
            view = views.Dequeue();
            view.transform.name = name;
            view.Mesh.name = name;
            view.renderer.material = material;
            vertices = view.Mesh.vertices;
            GenerationTask = Task.CompletedTask;
            return true;
        }

        private void MakeNewView(string name, Material material)
        {
            view = new View(name, material);
            GenerationTask = GenerateMesh();
        }

        private const int MaxTicksForFrame = 100;
        private async Task GenerateMesh()
        {
            //float debugTime = Time.realtimeSinceStartup;
            //Debug.Log("TIMING: begin generate mesh " + view.transform.name);
            view.Mesh = new Mesh {name = view.transform.name};
            view.Mesh.MarkDynamic();
            vertices = new Vector3[resolution * resolution  * 4];
            int[] triangles = new int[resolution * resolution * 6];
            Vector2[] uvs0 = new Vector2[vertices.Length];
            float step = size / (resolution);
            Vector2 uvOffset = ((Vector2)localCoordinates) / piecesAmount;
            float uvScale = 1f / piecesAmount;
            int counter = 0;
            
            int t = System.Environment.TickCount + MaxTicksForFrame;
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    int idx4 = counter * 4;
                    int idx6 = counter++ * 6;
                    vertices[idx4].Set(j * step, vertices[idx4].y, i * step);
                    vertices[idx4+1].Set(j * step,  vertices[idx4+1].y, i * step + step);
                    vertices[idx4+2].Set(j * step + step,  vertices[idx4+2].y, i * step);
                    vertices[idx4+3].Set(j * step + step,  vertices[idx4+3].y, i * step + step);
                    uvs0[idx4] = new Vector2(j / (float)resolution, i / (float)resolution) * uvScale + uvOffset;
                    uvs0[idx4+1] = new Vector2(j / (float)resolution, (i + 1) / (float)resolution) * uvScale + uvOffset;
                    uvs0[idx4+2] = new Vector2((j + 1) / (float)resolution, i / (float)resolution) * uvScale + uvOffset;
                    uvs0[idx4+3] = new Vector2((j + 1) / (float)resolution, (i + 1) / (float)resolution) * uvScale + uvOffset;

                    triangles[idx6] = idx4;
                    triangles[idx6+1] = idx4 + 1;
                    triangles[idx6+2] = idx4 + 2;
                    triangles[idx6+3] = idx4 + 1;
                    triangles[idx6+4] = idx4 + 3;
                    triangles[idx6+5] = idx4 + 2;
                }

                if (System.Environment.TickCount > t)
                {
                    t = System.Environment.TickCount + MaxTicksForFrame;
                    await Task.Yield();
                }
            }

            view.Mesh.SetVertices(vertices);
            view.Mesh.SetTriangles(triangles, 0);
            view.Mesh.SetUVs(0, uvs0);
            Recalculate();
            view.Mesh.RecalculateTangents();
            //Debug.Log("TIMING: end generate mesh " + (Time.realtimeSinceStartup - debugTime));
        }

        public async Task SetHeights(float[,] heights)
        {
           // float debugTime = Time.realtimeSinceStartup;
            //Debug.Log("TIMING: begin set heights " + view.transform.name);
            int xOffset = localCoordinates.x * resolution;
            int yOffset = localCoordinates.y * resolution;
            
            int t = System.Environment.TickCount + MaxTicksForFrame;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int vertexIdx = (y * resolution + x) * 4;
                    SetVertexHeight(heights[y + yOffset, x + xOffset] * height, vertexIdx);
                    SetVertexHeight(heights[y + yOffset + 1, x + xOffset] * height, vertexIdx + 1);
                    SetVertexHeight(heights[y + yOffset, x + xOffset + 1] * height, vertexIdx + 2);
                    SetVertexHeight(heights[y + yOffset + 1, x + xOffset + 1] * height, vertexIdx + 3);
                }
                if (System.Environment.TickCount > t)
                {
                    t = System.Environment.TickCount + MaxTicksForFrame;
                    await Task.Yield();
                }
            }
            await GenerationTask;
            //Debug.Log("TIMING: end set heights to array " + (Time.realtimeSinceStartup - debugTime));
            view.Mesh.vertices = vertices;
            //Debug.Log("TIMING: end set heights to mesh " + (Time.realtimeSinceStartup - debugTime));
            await Task.Yield();
            view.collider.sharedMesh = view.Mesh;
            //Debug.Log("TIMING: end set heights to collider " + (Time.realtimeSinceStartup - debugTime));
            view.transform.gameObject.SetActive(true); // if object was in pool it will be turned off until mesh sets
            //Debug.Log("UnHide " + view.transform.name);
        }

        private void SetVertexHeight(float value, int vertexIdx)
        {
            Vector3 temp = vertices[vertexIdx];
            temp.y = value;
            vertices[vertexIdx] = temp;
        }

        public void Recalculate()
        {
            view.Mesh.RecalculateNormals();
            view.Mesh.RecalculateBounds();
        }

        public void SetNeighbors(Subchunk top, Subchunk bottom, Subchunk left, Subchunk right)
        {
            
        }
        
        public void Destroy()
        {
            if (Application.isPlaying)
            {
                if (!_pool.ContainsKey(key))
                {
                    _pool.Add(key, new Queue<View>());
                }
                Debug.Log("Hide " + view.transform.name);

                view.transform.gameObject.SetActive(false);
                _pool[key].Enqueue(view);
            }
            else
            {
                Object.DestroyImmediate(view.Mesh);
                Object.DestroyImmediate(view.transform.gameObject);
            }
        }
    }
}