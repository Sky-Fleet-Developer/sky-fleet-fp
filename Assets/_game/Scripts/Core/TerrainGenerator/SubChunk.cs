using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Core.TerrainGenerator
{
    public struct SubChunkId : IEquatable<SubChunkId>
    {
        private int _id;
        public SubChunkId(int x, int y)
        {
            _id = x << 16 | y;
        }

        public bool Equals(SubChunkId other)
        {
            return _id.Equals(other._id);
        }

        public override bool Equals(object obj)
        {
            return obj is SubChunkId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public override string ToString()
        {
            return _id.ToString();
        }
    }
    
    public class SubChunk
    {
        private class View
        {
            public Mesh Mesh
            {
                get => MeshFilter?.sharedMesh;
                set
                {
                    MeshFilter.sharedMesh = value;
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
                Collider = null;
            }
        }
        public struct PackedVertex
        {
            public float3 Position;
            public float3 Normal;
            public float2 UV;
        };
        
        private static List<View> _pool = new ();
        public static void ClearPool()
        {
            foreach (var view in _pool)
            {
                
            }

            _pool.Clear();
        }
#if UNITY_EDITOR
        static SubChunk()
        {
            UnityEditor.EditorApplication.playModeStateChanged += (state) => ClearPool();
        }
        #endif

        private View _view;
        private readonly float _height;
        private readonly int _subChunkResolution;
        private readonly float _size;
        private readonly Vector2Int _chunkCoords;
        private readonly int _piecesAmount;
        private Vector2Int _minCoverage;
        private Vector2Int _maxCoverage;
        private HeightmapGpuWorker _heightmapGpuWorker;
        private GraphicsBuffer _vertexBuffer;
        private int _heightmapResolution;
        private SubChunkId _id;
        private Vector2Int _coordsInChunk;
        public GraphicsBuffer VertexBuffer => _vertexBuffer;
        public SubChunkId Id => _id;
        
        public void SetMinMaxCoverage(Vector2Int min, Vector2Int max)
        {
            _minCoverage = min;
            _maxCoverage = max;
        }
        
        public void SetChunkPosition(Vector3 position)
        {
            _view.Transform.position = position + new Vector3(_coordsInChunk.x * _size, 0, _coordsInChunk.y * _size);
        }
        
        public Vector3 SelfCenter => _view.Transform.position + new Vector3(_size * 0.5f, 0, _size * 0.5f);
        public int Resolution => _subChunkResolution;
        public float Size => _size;

        public MeshCollider GetOrCreateColliderComponent()
        {
            return _view.Collider ?? (_view.Collider = _view.Transform.gameObject.AddComponent<MeshCollider>());
        }
        
        public SubChunk(string name, Transform parent, Vector3 chunkPosition, float size, float height, int subChunkResolution, int heightmapResolution, Vector2Int chunkCoords, Vector2Int coordsInChunk, int piecesAmount, Material material, HeightmapGpuWorker heightmapGpuWorker)
        {
            _coordsInChunk = coordsInChunk;
            _heightmapGpuWorker = heightmapGpuWorker;
            _height = height;
            _subChunkResolution = subChunkResolution;
            _heightmapResolution = heightmapResolution;
            _size = size;
            _chunkCoords = chunkCoords;
            _piecesAmount = piecesAmount;
            _id = new SubChunkId(chunkCoords.x * _piecesAmount + _coordsInChunk.x, chunkCoords.y * _piecesAmount + _coordsInChunk.y);

            if (!TryAssignViewFromPool(name, material))
            {
                MakeNewView(name, material);
            }
            _vertexBuffer = _view.Mesh.GetVertexBuffer(0);
            _view.Transform.gameObject.layer = parent.gameObject.layer;
            _view.Transform.SetParent(parent);
            SetChunkPosition(chunkPosition);
        }

        private bool TryAssignViewFromPool(string name, Material material)
        {
            if (_pool.Count == 0) return false;
            _view = _pool[^1];
            _pool.RemoveAt(_pool.Count - 1);
            _view.Transform.name = name;
            _view.Transform.gameObject.SetActive(true);
            _view.Mesh.name = name;
            _view.Renderer.material = material;
            return true;
        }

        private void MakeNewView(string name, Material material)
        {
            _view = new View(name, material);
            GenerateMesh();
        }

        private const int MaxTicksForFrame = 100;
        private void GenerateMesh()
        {
            //float debugTime = Time.realtimeSinceStartup;
            //Debug.Log("TIMING: begin generate  " + view.transform.name);
            _view.Mesh = new Mesh {name = _view.Transform.name};
            int triangleCount = _subChunkResolution * _subChunkResolution * 6;
            int verticesPerSide = _subChunkResolution + 1;
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
            float step = _size / (_subChunkResolution);
            float resInv = 1f / _subChunkResolution;
            Vector2 uvOffset = ((Vector2)_coordsInChunk) / _piecesAmount;
            float uvScale = 1f / _piecesAmount;
            PackedVertex[] initVertexData = new PackedVertex[vertexCount];

            int[] triangles = new int[triangleCount]; 

            int tIndex = 0;

            for (int j = 0; j <= _subChunkResolution; j++)
            {
                for (int i = 0; i <= _subChunkResolution; i++)
                {
                    int vIndex = i * verticesPerSide + j;
        
                    PackedVertex vert = new PackedVertex();
                    vert.Position = new float3(j * step, 0, i * step);
                    vert.Normal = new float3(0, 1, 0);
                    vert.UV = new float2(j * resInv * uvScale + uvOffset.x, i * resInv * uvScale + uvOffset.y);
                    initVertexData[vIndex] = vert;

                    if (i < _subChunkResolution && j < _subChunkResolution)
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

            Vector3 boundsSize = new Vector3(_size, _height, _size);
            _view.Mesh.bounds = new Bounds(boundsSize * 0.5f, boundsSize);
            _view.Mesh.SetSubMeshes(subMeshes, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds);
        }

        public void SetHeights(ComputeBuffer heights)
        {
            _heightmapGpuWorker.AlignVerticesToHeightmap(_vertexBuffer, heights, _subChunkResolution, _heightmapResolution, _size, _height, _minCoverage, _maxCoverage);
        }

        public void Recalculate()
        {
            //view.Mesh.RecalculateNormals();
            //view.Mesh.RecalculateBounds();
        }

        public void Destroy()
        {
            if (Application.isPlaying)
            {
                //Debug.Log("Hide " + _view.Transform.name);

                _view.Transform.gameObject.SetActive(false);
                _pool.Add(_view);
            }
            else
            {
                Object.DestroyImmediate(_view.Mesh);
                Object.DestroyImmediate(_view.Transform.gameObject);
            }
        }

        public Mesh GetVisualMesh()
        {
            return _view.Mesh;
        }
    }
}