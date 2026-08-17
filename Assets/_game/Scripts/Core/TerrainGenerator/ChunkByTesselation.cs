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
        //private MeshFilter _meshFilter;
        //private MeshRenderer _meshRenderer;
        private Vector2Int _coord;
        private HeightmapData _heightmapData;
        public bool IsChunkVisible { get; set; }
        
        public Vector2Int Coord => _coord;

        [ShowInInspector] public Material Material => _material;


        public float ChunkSize => _settings.ChunkSize;
        public float Height => _settings.Height;
        public int Resolution => _settings.HeightmapResolution;
        public int ColorMapResolution => _settings.AlphamapResolution;

        //private void Awake()
        //{
        //    _meshFilter = gameObject.AddComponent<MeshFilter>();
        //    _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        //}

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
            _material.SetFloat(SlotsCountInv, 1f / _settings.MaxLoadedChunksByOneSide);
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
    }
}