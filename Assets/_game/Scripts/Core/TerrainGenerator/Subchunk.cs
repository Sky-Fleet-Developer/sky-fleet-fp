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
                get => MeshFilter?.sharedMesh;
                set
                {
                    MeshFilter.sharedMesh = value;
                    Collider.sharedMesh = value;
                }
            }
            public Transform Transform;
            public MeshRenderer Renderer;
            public MeshCollider Collider;
            public MeshFilter MeshFilter;

            public View(string name, Material material)
            {
                Transform = new GameObject(name).transform;
                Renderer = Transform.gameObject.AddComponent<MeshRenderer>();
                Renderer.sharedMaterial = material;
                MeshFilter = Transform.gameObject.AddComponent<MeshFilter>();
                Collider = Transform.gameObject.AddComponent<MeshCollider>();
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

        private View _view;
        private readonly float _height;
        private readonly int _subchunkResolution;
        private readonly float _size;
        private readonly int _key;
        private readonly Vector2Int _localCoordinates;
        private readonly int _piecesAmount;
        private Vector2Int _minCoverage;
        private Vector2Int _maxCoverage;
        private HeightmapGpuWorker _heightmapGpuWorker;
        private GraphicsBuffer _vertexBuffer;
        private int _heightmapResolution;
        public Task GenerationTask { get; private set; }
        public GraphicsBuffer VertexBuffer => _vertexBuffer;

        public Vector3 Position
        {
            get => _view.Transform.position;
            set => _view.Transform.position = value + new Vector3(_localCoordinates.x * _size, 0, _localCoordinates.y * _size);
        }

        public void SetMinMaxCoverage(Vector2Int min, Vector2Int max)
        {
            _minCoverage = min;
            _maxCoverage = max;
        }

        public Subchunk(string name, Transform parent, float size, float height, int subchunkResolution, int heightmapResolution, Vector2Int localCoordinates, int piecesAmount, Material material, HeightmapGpuWorker heightmapGpuWorker)
        {
            _heightmapGpuWorker = heightmapGpuWorker;
            _height = height;
            _subchunkResolution = subchunkResolution;
            _heightmapResolution = heightmapResolution;
            _size = size;
            _localCoordinates = localCoordinates;
            _piecesAmount = piecesAmount;
            _key = subchunkResolution * (int) size;

            if (!TryAssignViewFromPool(name, material))
            {
                MakeNewView(name, material);
            }

            _view.Transform.gameObject.layer = parent.gameObject.layer;
            _view.Transform.SetParent(parent);
        }

        private bool TryAssignViewFromPool(string name, Material material)
        {
            if (!_pool.TryGetValue(_key, out Queue<View> views)) return false;
            if (views.Count <= 0) return false;
            _view = views.Dequeue();
            _view.Transform.name = name;
            _view.Mesh.name = name;
            _view.Renderer.material = material;
            _vertexBuffer = _view.Mesh.GetVertexBuffer(0);
            GenerationTask = Task.CompletedTask;
            return true;
        }

        private void MakeNewView(string name, Material material)
        {
            _view = new View(name, material);
            GenerationTask = GenerateMesh();
        }

        private const int MaxTicksForFrame = 100;
        private async Task GenerateMesh()
        {
            //float debugTime = Time.realtimeSinceStartup;
            //Debug.Log("TIMING: begin generate mesh " + view.transform.name);
            _view.Mesh = new Mesh {name = _view.Transform.name};
            int triangleCount = _subchunkResolution * _subchunkResolution * 6;
            int verticesPerSide = _subchunkResolution + 1;
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
            _view.Mesh.SetVertexBufferParams(vertexCount, layout);
            _view.Mesh.SetIndexBufferParams(triangleCount, IndexFormat.UInt32);
            _view.Mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            _view.Mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
            int counter = 0;
            float step = _size / (_subchunkResolution);
            float resInv = 1f / _subchunkResolution;
            Vector2 uvOffset = ((Vector2)_localCoordinates) / _piecesAmount;
            float uvScale = 1f / _piecesAmount;
            PackedVertex[] initVertexData = new PackedVertex[vertexCount];

            int[] triangles = new int[triangleCount]; 

            int tIndex = 0;

            for (int j = 0; j <= _subchunkResolution; j++)
            {
                for (int i = 0; i <= _subchunkResolution; i++)
                {
                    int vIndex = i * verticesPerSide + j;
        
                    PackedVertex vert = new PackedVertex();
                    vert.Position = new float3(j * step, 0, i * step);
                    vert.Normal = new float3(0, 1, 0);
                    vert.UV = new float2(j * resInv * uvScale + uvOffset.x, i * resInv * uvScale + uvOffset.y);
                    initVertexData[vIndex] = vert;

                    if (i < _subchunkResolution && j < _subchunkResolution)
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

            _view.Mesh.SetVertexBufferData(initVertexData, 0, 0, vertexCount, 0,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);

            _view.Mesh.SetIndexBufferData(triangles, 0, 0, triangleCount,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);

            _vertexBuffer = _view.Mesh.GetVertexBuffer(0);
            Vector3 boundsSize = new Vector3(_size, _height, _size);
            _view.Mesh.bounds = new Bounds(boundsSize * 0.5f, boundsSize);
            _view.Mesh.SetSubMeshes(subMeshes, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds);


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

        public void SetHeights(ComputeBuffer heights)
        {
            _heightmapGpuWorker.AlignVerticesToHeightmap(_vertexBuffer, heights, _subchunkResolution, _heightmapResolution, _size, _height, _minCoverage, _maxCoverage);
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
                if (!_pool.ContainsKey(_key))
                {
                    _pool.Add(_key, new Queue<View>());
                }
                Debug.Log("Hide " + _view.Transform.name);

                _view.Transform.gameObject.SetActive(false);
                _pool[_key].Enqueue(_view);
            }
            else
            {
                Object.DestroyImmediate(_view.Mesh);
                Object.DestroyImmediate(_view.Transform.gameObject);
            }
        }
    }
}