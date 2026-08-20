using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
using Core.World;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Core.TerrainGenerator
{
    public class ChunkByTesselation : MonoBehaviour, IChunk
    {
        //private static List<Material> _pool = new ();
        private static readonly int SourceHeightmap = Shader.PropertyToID("source_heightmap");
        private static readonly int Map = Shader.PropertyToID("map");
        private static readonly int SlotsCountInv = Shader.PropertyToID("slots_count_inv");
        private static readonly int MapSize = Shader.PropertyToID("map_size");
        private static readonly int HeightScale = Shader.PropertyToID("height_scale");

        private TerrainGenerationSettings _settings;
        private Material _material;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Vector2Int _coord;
        private HeightmapData _heightmapData;
        public bool IsChunkVisible { get; set; }

        public Vector2Int Coord => _coord;

        [ShowInInspector] public Material Material => _material;

        public float ChunkSize => _settings.ChunkMeshSize;

        private void Awake()
        {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        public ChunkByTesselation Init(string name, Vector2Int coord, Transform parent,
            TerrainGenerationSettings settings, HeightmapData heightmapData)
        {
            _heightmapData = heightmapData;
            this.name = name;
            _coord = coord;
            _settings = settings;

            GetOrCreateMaterial();

            //var worker = settings.Settings.OfType<MeshHeightmapChannelSettings>().First().GpuWorker;
            BindMaterialParams();

            transform.localPosition = GetMyWorldPosition();

            if (!_quadMesh)
            {
                CreateMesh(settings.ChunkMeshResolution, settings.ChunkMeshSize, settings.Height);
            }

            _meshFilter.sharedMesh = _quadMesh;
            _meshRenderer.sharedMaterial = _material;

            return this;
        }

        private void GetOrCreateMaterial()
        {
            _material ??= Object.Instantiate(_settings.Material);
        }

        private void BindMaterialParams()
        {
            _material.SetTexture(SourceHeightmap, _heightmapData.HeightmapTex);
            _material.SetBuffer(Map, _heightmapData.GetMapBuffer(out Vector2Int mapMin, out int mapSize));
            float hmPixSize = 1f / _settings.ChunkMeshSize;//((_settings.HeightmapResolution + 2) * _settings.MaxLoadedChunksByOneSide);
            _material.SetVector(PositionToChunkMatrix, new Vector4(hmPixSize, hmPixSize, mapMin.x * _settings.ChunkMeshSize + WorldOffset.Offset.x, mapMin.y * _settings.ChunkMeshSize + WorldOffset.Offset.z));
            _material.SetFloat(MapSize, mapSize);
            _material.SetFloat(HeightScale, _settings.Height);
            _material.SetFloat(WidthScale, _settings.ChunkMeshSize);
            _material.SetFloat(SlotsCountInv, 1f / _settings.MaxLoadedChunksByOneSide);
            _material.SetFloat(HeightmapChunkResolution, _settings.HeightmapResolution + 2); // 2 pix for border
            _material.SetFloat(PixelSizeUVSpace, 1f / (_settings.HeightmapResolution + 2));
        }

        private Vector3 GetMyWorldPosition()
        {
            return new Vector3(_coord.x * ChunkSize, 0, _coord.y * ChunkSize);
        }

        private Vector3 GetMySpacePosition()
        {
            return WorldOffset.WorldToSpace(new Vector3(_coord.x * ChunkSize, 0, _coord.y * ChunkSize));
        }

        public void RefreshPosition()
        {
            Vector3 position = GetMyWorldPosition();
            transform.localPosition = position;
        }

        public void OnChunksRefreshed()
        {
            BindMaterialParams();
        }

        public void SetHeights(RenderTexture heightmap, ComputeBuffer mapBuffer, Vector2Int chunkCoordMapSpace,
            int mapSize)
        {
        }

        public void Enable()
        {
            gameObject.SetActive(true);
            GetOrCreateMaterial();
            BindMaterialParams();
        }

        public void Disable()
        {
            if (Application.isPlaying)
            {
                //Debug.Log("Hide mat " + GetMyWorldPosition());
                //_pool.Add(_material);
            }
            else
            {
                Object.DestroyImmediate(_material);
            }

            gameObject.SetActive(false);
        }

        private static Mesh _quadMesh;
        private static readonly int PixelSizeUVSpace = Shader.PropertyToID("pixel_size_uv_space");
        private static readonly int HeightmapChunkResolution = Shader.PropertyToID("heightmap_chunk_resolution");
        private static readonly int WidthScale = Shader.PropertyToID("width_scale");
        private static readonly int HeightmapOffset = Shader.PropertyToID("heightmap_offset");
        private static readonly int PositionToChunkMatrix = Shader.PropertyToID("position_to_chunk_matrix");
        private static readonly int MapSpaceToChunkSpaceUV = Shader.PropertyToID("map_space_to_chunk_space_uv");

        private static void CreateMesh(int resolution, float width, float height)
        {
            _quadMesh = new Mesh();
            _quadMesh.name = "TerrainQuad";
            int indicesCount = resolution * resolution * 6;
            int verticesPerSide = resolution + 1;
            int vertexCount = verticesPerSide * verticesPerSide;

            SubMeshDescriptor subMeshDescriptor = new SubMeshDescriptor();
            subMeshDescriptor.baseVertex = 0;
            subMeshDescriptor.firstVertex = 0;
            subMeshDescriptor.indexCount = indicesCount;
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
            _quadMesh.SetVertexBufferParams(vertexCount, layout);
            _quadMesh.SetIndexBufferParams(indicesCount, IndexFormat.UInt32);
            _quadMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            _quadMesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
            float step = width / resolution;
            float resInv = 1f / resolution;
            PackedVertex[] vertices = new PackedVertex[vertexCount];

            int[] indices = new int[indicesCount]; 

            int tIndex = 0;

            for (int j = 0; j <= resolution; j++)
            {
                for (int i = 0; i <= resolution; i++)
                {
                    int vIndex = i * verticesPerSide + j;
        
                    PackedVertex vert = new PackedVertex();
                    vert.Position = new float3(j * step, 0, i * step);
                    vert.Normal = new float3(0, 1, 0);
                    vert.UV = new float2(j * resInv, i * resInv);
                    vertices[vIndex] = vert;

                    if (i < resolution && j < resolution)
                    {
                        // Индексы четырех вершин текущего квадрата
                        int bottomLeft = vIndex;
                        int bottomRight = vIndex + 1;
                        int topLeft = vIndex + verticesPerSide;
                        int topRight = vIndex + verticesPerSide + 1;

                        // Первый треугольник (Bottom-Left -> Top-Left -> Bottom-Right)
                        indices[tIndex++] = bottomLeft;
                        indices[tIndex++] = topLeft;
                        indices[tIndex++] = bottomRight;

                        // Второй треугольник (Bottom-Right -> Top-Left -> Top-Right)
                        indices[tIndex++] = bottomRight;
                        indices[tIndex++] = topLeft;
                        indices[tIndex++] = topRight;
                    }
                }
            }

            _quadMesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);
            _quadMesh.SetIndexBufferData(indices, 0, 0, indicesCount,
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices);
            Vector3 boundsSize = new Vector3(width, height, width);
            _quadMesh.bounds = new Bounds(boundsSize * 0.5f, boundsSize);
            _quadMesh.SetSubMeshes(subMeshes,
                MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontResetBoneBounds);
        }
    }
}