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
        private static readonly int ChunkCoordX = Shader.PropertyToID("chunk_coord_x");
        private static readonly int ChunkCoordY = Shader.PropertyToID("chunk_coord_y");
        private static readonly int SlotsCountInv = Shader.PropertyToID("slots_count_inv");
        private static readonly int MapSize = Shader.PropertyToID("map_size");
        private static readonly int HeightScale = Shader.PropertyToID("height_scale");

        public static void Clear()
        {
            //foreach (var material in _pool)
            //{
            //    if (Application.isPlaying)
            //    {
            //        Object.Destroy(material);
            //    }
            //    else
            //    {
            //        Object.DestroyImmediate(material);
            //    }
            //}
            //_pool.Clear();

        }

#if UNITY_EDITOR
        static ChunkByTesselation()
        {
            UnityEditor.EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Clear();
            };
        }
#endif
        
        private TerrainGenerationSettings _settings;
        private Material _material;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Vector2Int _coord;
        private HeightmapData _heightmapData;
        public bool IsChunkVisible { get; set; }
        
        public Vector2Int Coord => _coord;

        [ShowInInspector] public Material Material => _material;


        public float ChunkSize => _settings.ChunkSize;
        public float Height => _settings.Height;
        public int Resolution => _settings.HeightmapResolution;
        public int ColorMapResolution => _settings.AlphamapResolution;

        private void Awake()
        {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        public ChunkByTesselation Init(string name, Vector2Int coord, Transform parent, TerrainGenerationSettings settings, HeightmapData heightmapData)
        {
            _heightmapData = heightmapData;
            this.name = name;
            _coord = coord;
            _settings = settings;

            GetOrCreateMaterial();
            
            //var worker = settings.Settings.OfType<MeshHeightmapChannelSettings>().First().GpuWorker;
            BindMaterialParams();

            transform.localPosition = GetMyWorldPosition();
            
            
            //if (!_quadMesh)
            //{
            //    CreateMesh(settings.useQuadsInsteadOfTriangles, settings.ChunkSize, settings.Height);
            //}

            //_meshFilter.sharedMesh = _quadMesh;
            //_meshRenderer.sharedMaterial = _material;
            
            return this;
        }

        private void GetOrCreateMaterial()
        {
            //if (_pool.Count == 0)
            //{
                _material ??= Object.Instantiate(_settings.Material);
            //}
            //else
            //{
            //    _material = _pool[^1];
            //    _pool.RemoveAt(_pool.Count - 1);
            //}
        }

        private void BindMaterialParams()
        {
            _material.SetTexture(SourceHeightmap, _heightmapData.Texture);
            _material.SetBuffer(Map, _heightmapData.GetMapBuffer(out Vector2Int mapMin, out int mapSize));
            var mapCoord = _coord - mapMin;
            _material.SetFloat(ChunkCoordX, mapCoord.x);
            _material.SetFloat(ChunkCoordY, mapCoord.y);
            _material.SetFloat(MapSize, mapSize);
            _material.SetFloat(HeightScale, _settings.Height);
            _material.SetFloat(WidthScale, _settings.ChunkSize);
            _material.SetFloat(SlotsCountInv, 1f / _settings.MaxLoadedChunksByOneSide);
            _material.SetFloat(HeightmapChunkResolution, _settings.HeightmapResolution + 2);
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

        public void SetHeights(RenderTexture heightmap, ComputeBuffer mapBuffer, Vector2Int chunkCoordMapSpace, int mapSize)
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

        private static void CreateMesh(bool useQuadsInsteadOfTriangles, float width, float height)
    {
        _quadMesh = new Mesh();
        _quadMesh.name = "TerrainQuad";
        int indexCount = useQuadsInsteadOfTriangles ? 4 : 6;
        SubMeshDescriptor subMeshDescriptor = new SubMeshDescriptor();
        subMeshDescriptor.baseVertex = 0;
        subMeshDescriptor.firstVertex = 0;
        subMeshDescriptor.indexCount = indexCount;
        subMeshDescriptor.indexStart = 0;
        subMeshDescriptor.topology = useQuadsInsteadOfTriangles ? MeshTopology.Quads : MeshTopology.Triangles;
        subMeshDescriptor.vertexCount = 4;
        subMeshDescriptor.bounds = new Bounds(Vector3.zero, Vector3.one);
        List<SubMeshDescriptor> subMeshes = new List<SubMeshDescriptor>(1) { subMeshDescriptor };

        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };
        _quadMesh.SetVertexBufferParams(4, layout);
        _quadMesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
        PackedVertex[] initVertexData = new PackedVertex[]
        {
            new(new float3(0, 0, 0), new float3(0, 1, 0), new float2(0, 0)),
            new(new float3(0, 0, width), new float3(0, 1, 0), new float2(0, 1)),
            new(new float3(width, 0, width), new float3(0, 1, 0), new float2(1, 1)),
            new(new float3(width, 0, 0), new float3(0, 1, 0), new float2(1, 0)),
        };
        int[] indices = useQuadsInsteadOfTriangles ? new int[] { 0, 1, 2, 3 } : new int[] { 0, 1, 2, 2, 3, 0 };

        _quadMesh.SetVertexBufferData(initVertexData, 0, 0, 4, 0,
            MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds |
            MeshUpdateFlags.DontValidateIndices);
        _quadMesh.SetIndexBufferData(indices, 0, 0, indexCount,
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