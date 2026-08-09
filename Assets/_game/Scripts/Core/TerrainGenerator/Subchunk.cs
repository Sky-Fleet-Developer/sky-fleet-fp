using System.Collections.Generic;
using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
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
        public struct PackedVertex
        {
            public float3 Position;
            public float3 Normal;
            public float2 UV;
        };
        
        private static Dictionary<int, Queue<View>> _pool
            = new Dictionary<int, Queue<View>>();
        public static void ClearPool() => _pool.Clear();

        private View view;
        private readonly float height;
        private readonly int resolution;
        private readonly float size;
        private readonly int key;
        private readonly Vector2Int localCoordinates;
        private readonly int piecesAmount;
        private Vector2Int minCoverage;
        private Vector2Int maxCoverage;
        private HeightmapGpuWorker _heightmapGpuWorker;
        private GraphicsBuffer _vertexBuffer;
        public Task GenerationTask { get; private set; }
        public GraphicsBuffer VertexBuffer => _vertexBuffer;

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

        public Subchunk(string name, Transform parent, float size, float height, int resolution, Vector2Int localCoordinates, int piecesAmount, Material material, HeightmapGpuWorker heightmapGpuWorker)
        {
            _heightmapGpuWorker = heightmapGpuWorker;
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
            _vertexBuffer = view.Mesh.GetVertexBuffer(0);
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
            int triangleCount = resolution * resolution * 6;
            int verticesPerSide = resolution + 1;
            int vertexCount = verticesPerSide * verticesPerSide;

            SubMeshDescriptor subMeshDescriptor = new SubMeshDescriptor();
            subMeshDescriptor.baseVertex = 0;
            subMeshDescriptor.firstVertex = 0;
            subMeshDescriptor.indexCount = triangleCount;
            subMeshDescriptor.indexStart = 0;
            subMeshDescriptor.topology = MeshTopology.Triangles;
            subMeshDescriptor.vertexCount = vertexCount;
            subMeshDescriptor.bounds = new Bounds(Vector3.zero, Vector3.one);
            List<SubMeshDescriptor> subMeshes = new List<SubMeshDescriptor>(1) {subMeshDescriptor};
            
            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };
            view.Mesh.SetVertexBufferParams(vertexCount, layout);
            view.Mesh.SetIndexBufferParams(triangleCount, IndexFormat.UInt32);
            view.Mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            view.Mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
            int counter = 0;
            float step = size / (resolution);
            float resInv = 1f / resolution;
            Vector2 uvOffset = ((Vector2)localCoordinates) / piecesAmount;
            float uvScale = 1f / piecesAmount;
            PackedVertex[] initVertexData = new PackedVertex[vertexCount];

            int[] triangles = new int[triangleCount]; 

            int tIndex = 0;

            for (int i = 0; i <= resolution; i++)
            {
                for (int j = 0; j <= resolution; j++)
                {
                    int vIndex = i * verticesPerSide + j;
        
                    PackedVertex vert = new PackedVertex();
                    vert.Position = new float3(j * step, 0, i * step);
                    vert.Normal = new float3(0, 1, 0);
                    vert.UV = new float2(j * resInv * uvScale + uvOffset.x, i * resInv * uvScale + uvOffset.y);
                    initVertexData[vIndex] = vert;

                    if (i < resolution && j < resolution)
                    {
                        // Индексы четырех вершин текущего квадрата
                        int bottomLeft = vIndex;
                        int bottomRight = vIndex + 1;
                        int topLeft = vIndex + verticesPerSide;
                        int topRight = vIndex + verticesPerSide + 1;

                        // Первый треугольник (Bottom-Left -> Top-Left -> Bottom-Right)
                        triangles[tIndex++] = bottomLeft;
                        triangles[tIndex++] = topLeft;
                        triangles[tIndex++] = bottomRight;

                        // Второй треугольник (Bottom-Right -> Top-Left -> Top-Right)
                        triangles[tIndex++] = bottomRight;
                        triangles[tIndex++] = topLeft;
                        triangles[tIndex++] = topRight;
                    }
                }
            }

            view.Mesh.SetVertexBufferData(initVertexData, 0, 0, vertexCount, 0,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);

            view.Mesh.SetIndexBufferData(triangles, 0, 0, triangleCount,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);

            _vertexBuffer = view.Mesh.GetVertexBuffer(0);
            view.Mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
            view.Mesh.SetSubMeshes(subMeshes, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds);


            /*vertices = new Vector3[resolution * resolution * 4];
            Vector2[] uvs0 = new Vector2[vertices.Length];
            
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
            */
            //Debug.Log("TIMING: end generate mesh " + (Time.realtimeSinceStartup - debugTime));
        }

        public async Task SetHeights(ComputeBuffer heights)
        {
            /*
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
            view.Mesh.vertices = vertices;
            await Task.Yield();
            view.collider.sharedMesh = view.Mesh;
            view.transform.gameObject.SetActive(true); // if object was in pool it will be turned off until mesh sets
            */
        }

        public void Recalculate()
        {
            //view.Mesh.RecalculateNormals();
            //view.Mesh.RecalculateBounds();
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