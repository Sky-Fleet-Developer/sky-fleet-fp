using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Core.Utilities;
using System.Threading.Tasks;
using Core.Explorer;
using Core.Misc;
using Core.TerrainGenerator.Biomes;
using Core.TerrainGenerator.Filters;
using Core.TerrainGenerator.LOD;
using Core.TerrainGenerator.Settings;
using Core.TerrainGenerator.Surfaces;
using Core.TerrainGenerator.Utility;
using Sirenix.OdinInspector;
using Core.World;
using Cysharp.Threading.Tasks;
using Runtime.Character;
using Unity.Collections;
using Unity.Mathematics;
using Zenject;
using ITickable = Core.Misc.ITickable;

namespace Core.TerrainGenerator
{
    
    /// <summary>
    /// runtime generating terrain chunks by TerrainGenerationSettings
    /// </summary>
    public class TerrainProvider : MonoBehaviour, ILoadAtStart, IMyInstaller, TerrainProvider.ITerrainProviderHandler, ITickable,
        ITerrainGpuData, ITerrainFarTint
    {
        public interface ITerrainProviderHandler
        {
            public TerrainGenerationSettings Settings { get; }
            void Initialize(bool force = false);
            void LoadPropsForCurrentPosition();
            void Unload();
            bool Enabled { get; }
            public TerrainData GetHeightmapData();
            TerrainViewMode ViewMode { get; set; }
        }
        
        public static readonly LateEvent<TerrainProvider> OnInitialize = new ();
        private static readonly int SourceAlphamap = Shader.PropertyToID("source_alphamap");
        [SerializeField] private TerrainGenerationSettings _settings;
        [Inject] private TickService _tickService;
        [Inject(Id = "Player")] private IDynamicPositionProvider _playerTracker;
        [Inject(Optional = true)] private IBiomeSet _biomes;

        [Title("View")]
        [SerializeField] private TerrainViewMode viewMode = TerrainViewMode.Biomes;
        [SerializeField, ShowIf(nameof(viewMode), TerrainViewMode.Heightmap)]
        private Gradient heightGradient = new ()
        {
            colorKeys = new[]
            {
                new GradientColorKey(new Color(0.1f, 0.2f, 0.6f), 0f),
                new GradientColorKey(new Color(0.2f, 0.6f, 0.25f), 0.3f),
                new GradientColorKey(new Color(0.9f, 0.8f, 0.3f), 0.55f),
                new GradientColorKey(new Color(0.5f, 0.3f, 0.15f), 0.8f),
                new GradientColorKey(Color.white, 1f),
            },
        };
        [PropertyTooltip("Normalized heights mapped to the ends of the gradient")]
        [SerializeField, MinMaxSlider(-0.5f, 1.5f, true), ShowIf(nameof(viewMode), TerrainViewMode.Heightmap)]
        private Vector2 heightViewRange = new (0, 1);
        [PropertyTooltip("Grid of MaxLoadedChunksByOneSide x MaxLoadedChunksByOneSide cells, one per texture slot. Generated when empty")]
        [SerializeField, ShowIf(nameof(viewMode), TerrainViewMode.Uv)]
        private Texture2D slotTexture;
        private Texture2D _heightGradientTex;
        private Texture2D _generatedSlotTex;
        private TerrainViewMode? _appliedViewMode;

        [ShowInInspector]
        private readonly HashSet<Vector2Int> _visibleChunks = new ();
        private List<IPatch> _inactivePatches = new ();
        private Dictionary<RectInt, IPatch> _activePatches = new ();
        private readonly List<RectInt> _leaves = new ();
        private readonly HashSet<RectInt> _leavesSet = new ();
        private readonly List<RectInt> _toRemovePatchesCache = new ();
        private Dictionary<Vector2Int, HashSet<IDeformer>> _deformersByChunk = new ();
        //private List<IDeformer> _deformersQueue = new ();
        [ShowInInspector] private TerrainData _terrainData;
        [ShowInInspector] RenderTexture HeightmapTexture => _terrainData?.HeightmapTex;
        [ShowInInspector] RenderTexture AlphamapTexture => _terrainData?.AlphamapTex;
        [ShowInInspector] Dictionary<Vector2Int, int2> ActiveChunks => _terrainData?._activeChunks;
        public TerrainGenerationSettings Settings => _settings;
        public int TickRate => 60;
        private readonly List<IHeightmapFilterSource> _filterSources = new ();
        private readonly List<IHeightmapFilter> _filters = new ();
        private readonly HeightmapBaker _heightmapBaker = new ();
        private readonly List<IHeightmapFilterSource> _biomeFilterSources = new ();
        private bool _biomeFiltersChanged;
        private TerrainSurfaces _surfaces;
        private BiomeDebugBaker _biomeDebugBaker;
        private BiomeComposer _composer;
        private SettingsBiome _settingsBiome;
        private readonly List<IBiome> _fallbackBiomes = new ();
        private bool _bakeScheduled;
        private bool _heightmapDirty;
        private bool _biomesDirty;
        private TerrainQuadTree _quadTree;
        private readonly PatchLodMap _lodMap = new ();
        private readonly float[] _morphEnds = new float[8];
        // The same for compute shaders, where every array element takes a float4
        private readonly float[] _morphEndsPadded = new float[8 * 4];
        private ComputeBuffer _mapBuffer;
        private Vector4 _positionToChunk;
        private Vector2 _mapCorner;
        private int _mapSize;
        private GraphicsBuffer _farTint;
        private GraphicsBuffer _noFarTint;
        private Vector3 _farTintCenter;
        private Vector2 _farTintRange;
        private Vector2 _lodViewPosition;
        
        public TerrainData GetHeightmapData()
        {
            return _terrainData;
        }

        public TerrainViewMode ViewMode
        {
            get => viewMode;
            set
            {
                viewMode = value;
                ApplyViewMode();
            }
        }

        /*public IEnumerable<(Vector2Int, HeightChannel)> EnumerateActiveSurfaceChannels()
        {
            foreach (var activeChunkChannel in _activeChunkChannels)
            {
                yield return (activeChunkChannel.Key, activeChunkChannel.Value[_heightChannelIndex] as HeightChannel);
            }
        }*/

        bool ILoadAtStart.enabled
        {
            get => enabled && gameObject.activeInHierarchy;
        }

        Task ILoadAtStart.Load()
        {
            Initialize();
            return Task.CompletedTask;
        }

        void ITerrainProviderHandler.Initialize(bool force)
        {
            if (force)
            {
                _initialized = false;
            }
            Initialize();
        }
        private bool _initialized;
        private void Initialize()
        {
            if(_initialized)
            {
                BindMaterialParams();
                return;
            }
            _initialized = true;
            CollectFilters();
            
            if (_terrainData == null)
            {
                _terrainData = new TerrainData(Settings.MaxLoadedChunksByOneSide, Settings.HeightmapResolution);
                _terrainData.MapChanged += BindMapToMaterial;
                _terrainData.RawDataChanged += ScheduleBake;
                _settings.Material.SetTexture(SourceAlphamap, AlphamapTexture);
            }
            if (_composer == null)
            {
                _composer = new BiomeComposer(Settings.BiomeComposeShader, Settings.GpuWorker, Settings.HeightmapResolution,
                    Settings.BiomeTilePoolSize);
                _composer.TilesLoaded += ScheduleBakeBeforeRendering;
                _settingsBiome = new SettingsBiome(Settings, transform);
                _fallbackBiomes.Add(_settingsBiome);
            }
            if (_surfaces == null)
            {
                _surfaces = new TerrainSurfaces();
                _surfaces.Changed += OnSurfaceTypeChanged;
            }
            BindMaterialParams();
            BindMapToMaterial();
            ScheduleBake();

            if (_biomes != null)
            {
                _biomes.Changed -= OnBiomesChanged;
                _biomes.Changed += OnBiomesChanged;
            }
            CollectBiomeFilters();

            WorldOffset.OnWorldOffsetChange -= OnWorldOffsetChange;
            if (Application.isPlaying)
            {
                WorldOffset.OnWorldOffsetChange += OnWorldOffsetChange;
            }

            _settings.Changed -= OnSettingsChanged;
            _settings.Changed += OnSettingsChanged;
            if (Application.isPlaying)
            {
                _tickService.Add(this);
                OnInitialize.Invoke(this);
            }
            //if (_deformersQueueTask != null)
            //{
            //    await _deformersQueueTask;
            //} 
        }

        public void Tick()
        {
            LoadPropsForCurrentPosition();
        }

        private void OnSettingsChanged()
        {
            if (_terrainData == null)
            {
                return;
            }

            BindMaterialParams();
            _settingsBiome?.Refresh();
            _composer?.Invalidate();
            ScheduleBake();
        }

        /// <summary>Biomes of the scene, or the whole terrain as one biome made from the settings when there are none.</summary>
        private IReadOnlyList<IBiome> CurrentBiomes =>
            _biomes != null && _biomes.Biomes.Count > 0 ? _biomes.Biomes : _fallbackBiomes;

        private void OnBiomesChanged()
        {
            _composer?.Invalidate();
            RefreshSurfaces();
            CollectBiomeFilters();
            ScheduleBiomesBake();
        }

        // Biome filter edits only need filtering and blending again; added or removed filters change the biomes
        private void CollectBiomeFilters()
        {
            UnsubscribeBiomeFilters();
            foreach (IBiome biome in CurrentBiomes)
            {
                if (biome.Filters == null)
                {
                    continue;
                }

                foreach (IHeightmapFilterSource source in biome.Filters)
                {
                    if (source != null)
                    {
                        _biomeFilterSources.Add(source);
                        source.Changed += OnBiomeFilterChanged;
                    }
                }
            }
        }

        private void UnsubscribeBiomeFilters()
        {
            foreach (IHeightmapFilterSource source in _biomeFilterSources)
            {
                source.Changed -= OnBiomeFilterChanged;
            }
            _biomeFilterSources.Clear();
        }

        private void CollectFilters()
        {
            UnsubscribeFilters();
            _filterSources.AddRange(Settings.HeightmapFilters);
            foreach (IHeightmapFilterSource source in _filterSources)
            {
                source.Changed += ScheduleBake;
            }
        }

        private void UnsubscribeFilters()
        {
            foreach (IHeightmapFilterSource source in _filterSources)
            {
                if (source != null)
                {
                    source.Changed -= ScheduleBake;
                }
            }
            _filterSources.Clear();
        }

        /// <summary>
        /// Coalesces bake requests (chunk uploads, filter changes) into a single bake per frame,
        /// done before rendering. New chunks need both the heightmap and the biomes.
        /// </summary>
        private void ScheduleBake()
        {
            _heightmapDirty = true;
            _biomesDirty = true;
            ScheduleBakeBeforeRendering();
        }

        private void OnBiomeFilterChanged()
        {
            _biomeFiltersChanged = true;
            ScheduleBakeBeforeRendering();
        }

        private void ScheduleBiomesBake()
        {
            _biomesDirty = true;
            ScheduleBakeBeforeRendering();
        }

        private void ScheduleBakeBeforeRendering()
        {
            if (_bakeScheduled)
            {
                return;
            }

            _bakeScheduled = true;
            BakeBeforeRendering().Forget();
        }

        private async UniTaskVoid BakeBeforeRendering()
        {
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            _bakeScheduled = false;
            if (this == null || _terrainData == null)
            {
                return;
            }

            // Newly composed chunks enter the map here, so they never show unbaked heights.
            // Biome filters run inside composing, before the biomes are blended
            _terrainData.FlushMap();
            bool biomeFiltersChanged = _biomeFiltersChanged;
            _biomeFiltersChanged = false;
            if (_composer.Compose(_terrainData, CurrentBiomes, transform.worldToLocalMatrix,
                    _settings.heightmapSettings.chunkSizeMeters, _settings.heightmapSettings.sharedEdgePixels, _settings.Height,
                    _settings.alphamapSettings, biomeFiltersChanged, _surfaces, _settings.biomeEdgeNoise))
            {
                _heightmapDirty = true;
                _biomesDirty = true;
                _terrainData.FlushMap();
            }

            if (_heightmapDirty)
            {
                _heightmapDirty = false;
                // Filters are read at bake time: a source may get a different filter assigned
                _filters.Clear();
                foreach (IHeightmapFilterSource source in _filterSources)
                {
                    if (source?.Filter != null)
                    {
                        _filters.Add(source.Filter);
                    }
                }
                _heightmapBaker.Bake(_terrainData, _filters, _settings.heightmapSettings.chunkSizeMeters, _settings.Height,
                    _settings.heightmapSettings.sharedEdgePixels);
            }

            // Biome colors are only shown in their view mode; they stay dirty until it's switched on
            if (_biomesDirty && viewMode == TerrainViewMode.Biomes)
            {
                _biomesDirty = false;
                BakeBiomes();
            }
        }

        private void BakeBiomes()
        {
            _biomeDebugBaker ??= new BiomeDebugBaker(_settings.BiomeDebugShader);
            // Biome positions are converted to terrain space, the space of chunk coordinates,
            // so they stay valid when the world offset moves the terrain
            bool hasBiomes = _biomeDebugBaker.Bake(_terrainData, CurrentBiomes, transform.worldToLocalMatrix,
                _settings.heightmapSettings.chunkSizeMeters, _settings.heightmapSettings.sharedEdgePixels, _settings.biomeEdgeNoise);
            if (hasBiomes)
            {
                Settings.Material.SetTexture(BiomeDebugMap, _biomeDebugBaker.DebugTex);
            }
            Settings.Material.SetFloat(BiomeDebugEnabled, hasBiomes ? 1f : 0f);
        }
        
        public void LoadPropsForCurrentPosition()
        {
            _quadTree ??= new TerrainQuadTree(Settings.lodConfig);
            CollectCurrentLeaves();
            // Geomorphing follows the view continuously, the leaves only change at the merge distances
            Settings.Material.SetVector(TerrainLodViewPosition, _lodViewPosition);
            if (!HaveLeavesChanged())
            {
                return;
            }

            Load();
            // Chunks released by Load must leave the map before their slots are reused
            _terrainData.FlushMap();
        }

        private bool HaveLeavesChanged()
        {
            if (_leavesSet.Count != _activePatches.Count)
            {
                return true;
            }

            foreach (RectInt leaf in _leavesSet)
            {
                if (!_activePatches.TryGetValue(leaf, out IPatch patch) || patch == null || !patch.Exist)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Map buffer content and the matrix that addresses it must always change together,
        /// so this is called whenever TerrainData rebuilds the map.
        /// </summary>
        private void BindMapToMaterial()
        {
            // Kept for ITerrainGpuData: compute users get the map the material has, without flushing it themselves
            _mapBuffer = _terrainData.GetMapBuffer(out Vector2Int mapMin, out _mapSize);
            Settings.Material.SetBuffer(Map, _mapBuffer);
            float hmPixSize = 1f / _settings.heightmapSettings.chunkSizeMeters;//((_settings.HeightmapResolution + 2) * _settings.MaxLoadedChunksByOneSide);
            // Rendered positions are in scene space; terrain space (patches, chunks, biomes) is the local space of this
            // transform, which the world offset moves along with everything else: terrain = scene - transform.position
            _mapCorner = (Vector2)mapMin * _settings.heightmapSettings.chunkSizeMeters;
            Vector3 origin = transform.position;
            _positionToChunk = new Vector4(hmPixSize, hmPixSize, _mapCorner.x + origin.x, _mapCorner.y + origin.z);
            Settings.Material.SetVector(PositionToChunkMatrix, _positionToChunk);
            Settings.Material.SetFloat(MapSize, _mapSize);
            Settings.Material.SetVector(TerrainMapOrigin, _mapCorner);
        }

        public bool IsReady => _terrainData != null && _terrainData.HeightmapTex && _terrainData.AlphamapTex &&
                               _mapBuffer != null && _lodMap.Texture;

        public IReadOnlyList<SurfaceType> Surfaces => _surfaces != null ? _surfaces.Surfaces : Array.Empty<SurfaceType>();

        public event Action SurfacesChanged;

        public void Bind(ComputeShader shader, int kernel)
        {
            if (!IsReady)
            {
                return;
            }

            shader.SetTexture(kernel, SourceHeightmap, _terrainData.HeightmapTex);
            shader.SetTexture(kernel, SourceAlphamap, _terrainData.AlphamapTex);
            shader.SetBuffer(kernel, Map, _mapBuffer);
            shader.SetInt(MapSize, _mapSize);
            shader.SetVector(PositionToChunkMatrix, _positionToChunk);
            shader.SetVector(TerrainMapOrigin, _mapCorner);
            shader.SetFloat(HeightScale, _settings.Height);
            shader.SetFloat(WidthScale, _settings.ChunkMeshSize);
            shader.SetFloat(SlotsCountInv, 1f / _settings.MaxLoadedChunksByOneSide);
            shader.SetFloat(HeightmapChunkResolution, _settings.HeightmapResolution + 2);
            shader.SetFloat(PixelSizeUVSpace, 1f / (_settings.HeightmapResolution + 2));
            shader.SetFloat(HeightmapSharedEdges, _settings.heightmapSettings.sharedEdgePixels ? 1f : 0f);
            shader.SetVector(TerrainOrigin, transform.position);

            _lodMap.Bind(shader, kernel);
            shader.SetFloat(TerrainPatchResolution, _settings.ChunkMeshResolution);
            QuadTreeLodConfig config = _settings.lodConfig;
            shader.SetKeyword(new UnityEngine.Rendering.LocalKeyword(shader, GeomorphKeyword), config.Geomorphing);
            for (int i = 0; i < _morphEnds.Length; i++)
            {
                _morphEndsPadded[i * 4] = Mathf.Min(config.MergeDistance(1 << i), 1e30f);
            }
            shader.SetFloats(TerrainMorphEnd, _morphEndsPadded);
            shader.SetFloat(TerrainMorphRange, config.MorphRange);
            shader.SetVector(TerrainLodViewPosition, _lodViewPosition);
        }

        public void SetFarTint(GraphicsBuffer perSurface, Vector3 center, Vector2 range)
        {
            _farTint = perSurface;
            _farTintCenter = center;
            _farTintRange = range;
            ApplyFarTint();
        }

        public void ClearFarTint()
        {
            _farTint = null;
            ApplyFarTint();
        }

        private void ApplyFarTint()
        {
            if (!_settings || !Settings.Material)
            {
                return;
            }

            // Every buffer the shader reads must be bound, so no tint has a buffer too
            bool hasTint = _farTint != null && _farTint.IsValid();
            if (!hasTint && _noFarTint == null)
            {
                _noFarTint = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, sizeof(float) * 4);
                _noFarTint.SetData(new[] { Vector4.zero });
            }

            Material material = Settings.Material;
            material.SetBuffer(TerrainFarTint, hasTint ? _farTint : _noFarTint);
            material.SetFloat(TerrainFarTintCount, hasTint ? _farTint.count : 0);
            material.SetVector(TerrainFarTintCenter, _farTintCenter);
            material.SetVector(TerrainFarTintRange, _farTintRange);
        }

        public bool Enabled => gameObject.activeInHierarchy && enabled;

        void ITerrainProviderHandler.Unload()
        {
            foreach (var patch in _activePatches.Values)
            {
                ReleasePatch(patch);
            }

            _activePatches.Clear();
        }

        private void OnWorldOffsetChange(Vector3 offset)
        {
            transform.position += offset;
            foreach (var patch in _activePatches.Values)
            {
                patch?.RefreshPosition();
            }

            if (_terrainData != null)
            {
                BindMapToMaterial();
            }
        }
        //HashSet<Vector2Int> _toUpdateCache = new HashSet<Vector2Int>();
        private List<QuadTreeNode> _tree;

        private void ReleasePatch(IPatch patch)
        {
            if (patch == null || !patch.Exist)
            {
                return;
            }

            patch.Disable();
            // Disable destroys the patch in edit mode
            if (patch.Exist)
            {
                _inactivePatches.Add(patch);
            }
        }

        private IPatch AcquirePatch(RectInt rect)
        {
            while (_inactivePatches.Count > 0)
            {
                IPatch patch = _inactivePatches[^1];
                _inactivePatches.RemoveAt(_inactivePatches.Count - 1);
                if (patch.Exist)
                {
                    patch.SetRect(rect);
                    patch.Enable();
                    return patch;
                }
            }

            return CreateTerrain(rect);
        }

        private void Load()
        {
            // Patches whose leaf is unchanged stay as they are
            foreach (var pair in _activePatches)
            {
                if (!_leavesSet.Contains(pair.Key) || pair.Value == null || !pair.Value.Exist)
                {
                    _toRemovePatchesCache.Add(pair.Key);
                }
            }
            foreach (RectInt rect in _toRemovePatchesCache)
            {
                ReleasePatch(_activePatches[rect]);
                _activePatches.Remove(rect);
            }
            _toRemovePatchesCache.Clear();

            Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (RectInt prop in _leaves)
            {
                max = Vector2Int.Max(max, prop.max);
                min = Vector2Int.Min(min, prop.min);

                if (_activePatches.ContainsKey(prop))
                {
                    continue;
                }

                IPatch patch = AcquirePatch(prop);
                if (patch != null)
                {
                    _activePatches.Add(prop, patch);
                }
            }
            // Patch cells -> heightmap chunks. Both ends are rounded outwards: rounding the size instead of the far end
            // drops the last chunk row/column whenever the near end is rounded down
            float cellsToChunks = Settings.ChunkMeshSize / _settings.heightmapSettings.chunkSizeMeters;
            Vector2Int chunksMin = Vector2Int.FloorToInt((Vector2)min * cellsToChunks);
            Vector2Int chunksMax = Vector2Int.CeilToInt((Vector2)max * cellsToChunks);
            RectInt chunksRange = new RectInt(chunksMin, chunksMax - chunksMin);

            // Patches find the LOD of their neighbours in it, to stitch their edges
            _lodMap.Bake(_leaves);
            _lodMap.Bind(Settings.Material);

            _visibleChunks.Clear();
            foreach (var cell in chunksRange.EnumerateYFirst())
            {
                _visibleChunks.Add(cell);
            }

            // Chunks get their data from the biomes; new ones are composed before the next rendering
            _composer.SetVisibleChunks(_visibleChunks, _terrainData);
            ScheduleBakeBeforeRendering();
        }
        
        private static readonly int PixelSizeUVSpace = Shader.PropertyToID("pixel_size_uv_space");
        private static readonly int HeightmapChunkResolution = Shader.PropertyToID("heightmap_chunk_resolution");
        private static readonly int WidthScale = Shader.PropertyToID("width_scale");
        private static readonly int HeightmapOffset = Shader.PropertyToID("heightmap_offset");
        private static readonly int PositionToChunkMatrix = Shader.PropertyToID("position_to_chunk_matrix");
        private static readonly int MapSpaceToChunkSpaceUV = Shader.PropertyToID("map_space_to_chunk_space_uv");
        private static readonly int SourceHeightmap = Shader.PropertyToID("source_heightmap");
        private static readonly int Map = Shader.PropertyToID("map");
        private static readonly int SlotsCountInv = Shader.PropertyToID("slots_count_inv");
        private static readonly int MapSize = Shader.PropertyToID("map_size");
        private static readonly int HeightScale = Shader.PropertyToID("height_scale");
        private static readonly int HeightmapSharedEdges = Shader.PropertyToID("heightmap_shared_edges");
        private static readonly int BiomeDebugMap = Shader.PropertyToID("biome_debug_map");
        private static readonly int BiomeDebugEnabled = Shader.PropertyToID("biome_debug_enabled");
        private static readonly int TerrainViewModeProperty = Shader.PropertyToID("terrain_view_mode");
        private static readonly int TerrainHeightGradient = Shader.PropertyToID("terrain_height_gradient");
        private static readonly int TerrainHeightViewRange = Shader.PropertyToID("terrain_height_view_range");
        private static readonly int TerrainSlotTexture = Shader.PropertyToID("terrain_slot_texture");
        private static readonly int TerrainMapOrigin = Shader.PropertyToID("terrain_map_origin");
        private static readonly int TerrainPatchResolution = Shader.PropertyToID("terrain_patch_resolution");
        private static readonly int TerrainMorphEnd = Shader.PropertyToID("terrain_morph_end");
        private static readonly int TerrainMorphRange = Shader.PropertyToID("terrain_morph_range");
        private static readonly int TerrainLodViewPosition = Shader.PropertyToID("terrain_lod_view_position");
        private static readonly int TerrainOrigin = Shader.PropertyToID("terrain_origin");
        private static readonly int TerrainFarTint = Shader.PropertyToID("terrain_far_tint");
        private static readonly int TerrainFarTintCount = Shader.PropertyToID("terrain_far_tint_count");
        private static readonly int TerrainFarTintCenter = Shader.PropertyToID("terrain_far_tint_center");
        private static readonly int TerrainFarTintRange = Shader.PropertyToID("terrain_far_tint_range");
        private const string GeomorphKeyword = "TERRAIN_GEOMORPH";
        
        private void BindMaterialParams()
        {
            Settings.Material.SetTexture(SourceHeightmap, _terrainData.HeightmapTex);
            Settings.Material.SetFloat(HeightScale, _settings.Height);
            Settings.Material.SetFloat(WidthScale, _settings.ChunkMeshSize);
            Settings.Material.SetFloat(SlotsCountInv, 1f / _settings.MaxLoadedChunksByOneSide);
            Settings.Material.SetFloat(HeightmapChunkResolution, _settings.HeightmapResolution + 2); // 2 pix for border
            Settings.Material.SetFloat(PixelSizeUVSpace, 1f / (_settings.HeightmapResolution + 2));
            Settings.Material.SetFloat(HeightmapSharedEdges, _settings.heightmapSettings.sharedEdgePixels ? 1f : 0f);
            Settings.Material.SetFloat(TerrainPatchResolution, _settings.ChunkMeshResolution);
            _lodMap.Bind(Settings.Material);
            BindGeomorphing();
            ApplyFarTint();
            RefreshSurfaces();
            ApplyViewMode();
        }

        private void BindGeomorphing()
        {
            Material material = Settings.Material;
            QuadTreeLodConfig config = _settings.lodConfig;
            material.SetKeyword(new UnityEngine.Rendering.LocalKeyword(material.shader, GeomorphKeyword), config.Geomorphing);
            // By log2 of the patch size. Finite, so the shader never gets infinity - infinity
            for (int i = 0; i < _morphEnds.Length; i++)
            {
                _morphEnds[i] = Mathf.Min(config.MergeDistance(1 << i), 1e30f);
            }
            material.SetFloatArray(TerrainMorphEnd, _morphEnds);
            material.SetFloat(TerrainMorphRange, config.MorphRange);
        }

        // Surface types of the settings and the biomes; alphamaps written with outdated ids are composed again
        private void RefreshSurfaces()
        {
            if (_surfaces == null)
            {
                return;
            }

            if (_surfaces.Collect(_settings.surfaceSettings, CurrentBiomes))
            {
                SurfacesChanged?.Invoke();
                _composer?.Invalidate();
                ScheduleBake();
            }
            _surfaces.Bind(Settings.Material, _settings.surfaceSettings);
        }

        // Edited textures or parameters: the alphamap stays valid
        private void OnSurfaceTypeChanged()
        {
            _surfaces.Bind(Settings.Material, _settings.surfaceSettings);
        }

        private void OnValidate()
        {
            if (_initialized && _terrainData != null && _settings)
            {
                ApplyViewMode();
            }
        }

        private void ApplyViewMode()
        {
            if (_terrainData == null)
            {
                return;
            }

            Material material = Settings.Material;
            material.SetFloat(TerrainViewModeProperty, (float)viewMode);
            switch (viewMode)
            {
                case TerrainViewMode.Heightmap:
                    material.SetTexture(TerrainHeightGradient, BakeHeightGradient());
                    material.SetVector(TerrainHeightViewRange, heightViewRange);
                    break;
                case TerrainViewMode.Uv:
                    material.SetTexture(TerrainSlotTexture, GetSlotTexture());
                    break;
            }

            if (_appliedViewMode != viewMode && viewMode == TerrainViewMode.Biomes)
            {
                ScheduleBiomesBake();
            }
            _appliedViewMode = viewMode;
        }

        private Texture2D BakeHeightGradient()
        {
            const int width = 256;
            if (!_heightGradientTex)
            {
                _heightGradientTex = new Texture2D(width, 1, TextureFormat.RGBA32, false, false)
                {
                    name = "Terrain height gradient",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave,
                };
            }

            for (int i = 0; i < width; i++)
            {
                _heightGradientTex.SetPixel(i, 0, heightGradient.Evaluate(i / (width - 1f)));
            }
            _heightGradientTex.Apply(false);
            return _heightGradientTex;
        }

        private Texture GetSlotTexture()
        {
            if (slotTexture)
            {
                return slotTexture;
            }

            int side = _settings.MaxLoadedChunksByOneSide;
            if (!_generatedSlotTex || _generatedSlotTex.width != side * 128)
            {
                DestroyTexture(_generatedSlotTex);
                _generatedSlotTex = SlotDebugTexture.Create(side);
            }
            return _generatedSlotTex;
        }

        private static void DestroyTexture(Texture texture)
        {
            if (!texture)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }
        
        //public async void RefreshProps()
        //{
        //    UnityEngine.Profiling.Profiler.BeginSample("TERRAIN");
        //    await LoadPropsForCurrentPosition();
        //    UnityEngine.Profiling.Profiler.EndSample();
        //}
        
        //private async Task AwaitForReadyAndApply()
        //{
        //    UnityEngine.Profiling.Profiler.BeginSample("Apply changes");
        //    await Task.WhenAll(_activeChunkChannels.SelectMany(x => x.Value.Select(WaitForChannelLoadingAndApply)));
        //    await Task.WhenAll(_activeChunkChannels.SelectMany(x => x.Value.Select(v => v.PostApply())));
        //    UnityEngine.Profiling.Profiler.EndSample();
        //}
        //
        //private async Task WaitForChannelLoadingAndApply(ChunkData channel)
        //{
        //    if (channel.IsDirty)
        //    {
        //        await channel.LoadingTask;
        //        if (_deformersByChunk.TryGetValue(channel.Coordinates, out HashSet<IDeformer> deformers))
        //        {
        //            foreach (IDeformer deformer in deformers)
        //            {
        //                channel.RegisterDeformer(deformer);
        //            }
        //            channel.ApplyDirtyToCache();
        //        }
        //        await channel.Apply();
        //    }
        //}

        private void CollectCurrentLeaves()
        {
            _leaves.Clear();
            _leavesSet.Clear();
            Vector3 pos = GetViewPosition();
            var flatPos = new Vector2(pos.x, pos.z);
            _lodViewPosition = flatPos;
            _tree ??= new List<QuadTreeNode>();
            foreach (var root in _quadTree.GetRelevantRoots(flatPos, Settings.ChunkMeshSize, QuadTreeLodConfig.RootSizeInCells))
            {
                _quadTree.Build(root, QuadTreeLodConfig.RootSizeInCells, flatPos, Settings.ChunkMeshSize, _tree);
                foreach (QuadTreeNode node in _tree)
                {
                    var rect = new RectInt(node.CellMin, node.CellMax - node.CellMin);
                    _leaves.Add(rect);
                    _leavesSet.Add(rect);
                }
            }

            /*Vector3 viewPosition = GetViewPosition();

            float sI = 1f / Settings.ChunkMeshSize;
            Vector2 viewCell = new Vector2(viewPosition.x * sI, viewPosition.z * sI);

            var lastViewCoord = new Vector2Int(Mathf.FloorToInt(viewCell.x), Mathf.FloorToInt(viewCell.y));

            for (int x = lastViewCoord.x - 8; x <= lastViewCoord.x + 8; x++)
            {
                for (int y = lastViewCoord.y - 8; y <= lastViewCoord.y + 8; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (IsPropInView(position, viewPosition)) yield return position;
                }
            }*/
        }

        private bool IsPropInView(Vector2Int position, Vector3 viewPosition)
        {
            viewPosition.y = 0;
            Vector3 center = GetPropCenter(position);
            Vector3 closestPointToProp = viewPosition + (center - viewPosition).normalized * Mathf.Min(Settings.VisibleDistance, Vector3.Distance(center, viewPosition));
            Vector3 difference = closestPointToProp - center;
            difference.x = Mathf.Abs(difference.x);
            difference.z = Mathf.Abs(difference.z);
            return difference.x < Settings.ChunkMeshSize * 0.5f && difference.z < Settings.ChunkMeshSize * 0.5f;
        }

        private Vector3 GetViewPosition()
        {
            if (_playerTracker == null)
            {
                return FindAnyObjectByType<SpawnPerson>().transform.position;
            }
            var viewPosition = _playerTracker.GetPredictedWorldPosition(2, 100);
            viewPosition.y = 0;
            return viewPosition;
        }
        

        private IPatch CreateTerrain(RectInt prop)
        {
            try
            {
                var instance = new GameObject();
                instance.transform.SetParent(transform);
                Patch patch = instance.AddComponent<Patch>().Init($"Terrain ({prop.xMin}, {prop.yMin})", transform, Settings, GetHeightmapData());
                patch.SetRect(prop);
                patch.gameObject.hideFlags = HideFlags.DontSave;
                return patch;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

       /* private Task _deformersQueueTimer;
        private Task _deformersQueueTask;
        private Vector3 _lastViewPosition;

        public void RegisterDeformer(IDeformer deformer)
        {
            _deformersQueue.Add(deformer);
            IEnumerable<Vector2Int> affected = deformer.GetAffectChunks(Settings.ChunkMeshSize);
            foreach (Vector2Int coord in affected)
            {
                if (!_deformersByChunk.ContainsKey(coord))
                {
                    _deformersByChunk.Add(coord, new HashSet<IDeformer>());
                }
                
                _deformersByChunk[coord].Add(deformer);
                if (_activeChunkChannels.TryGetValue(coord, out List<ChunkData> channelsList))
                {
                    foreach (ChunkData channel in channelsList)
                    {
                        channel.RegisterDeformer(deformer);
                    }
                }
            }

            if (_deformersQueueTimer == null)
            {
                TaskCompletionSource<bool> queueCompletionSource = new TaskCompletionSource<bool>();
                _deformersQueueTimer = queueCompletionSource.Task;
                _deformersQueueTask = LaunchDeformersQueue(queueCompletionSource);
                WaitForDeformersQueueAndSetTaskNull();
            }
        }

        private async void WaitForDeformersQueueAndSetTaskNull()
        {
            await _deformersQueueTask;
            _deformersQueueTask = null;
        }
        

        private async Task LaunchDeformersQueue(TaskCompletionSource<bool> queueCompletionSource)
        {
            await Task.Delay(2000);
            queueCompletionSource.SetResult(true);
            _deformersQueueTimer = null;

            foreach (List<ChunkData> deformationChannels in _activeChunkChannels.Values)
            {
                foreach (ChunkData deformationChannel in deformationChannels)
                {
                    if (deformationChannel.IsDirty) deformationChannel.ApplyDirtyToCache();
                }
            }

            await Task.WhenAll(_activeChunkChannels.SelectMany(x => x.Value.Select(v => v.IsDirty ? v.Apply() : Task.CompletedTask)));
            await Task.WhenAll(_activeChunkChannels.SelectMany(x => x.Value.Select(v => v.PostApply())));

            _deformersQueue.Clear();
        }*/

       private void OnDrawGizmos()
       {
           if (!Settings) return;
           DrawQuadTree();
       }

       private void OnDrawGizmosSelected()
        {
            if (!Settings) return;
            //DrawBoundsForProps(GetCurrentProps());
            
            Gizmos.color = Color.white * 0.5f;
            Matrix4x4 defaultMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 0, 1));
            Gizmos.DrawWireSphere(GetViewPosition(), Settings.VisibleDistance);
            Gizmos.matrix = defaultMatrix;
        }

        private void DrawQuadTree()
        {
            if (_quadTree == null)
            {
                return;
            }

            // The tree as it was last built, drawn in terrain space
            Vector2 flatPos = _lodViewPosition;
            Matrix4x4 defaultMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.Translate(transform.position);
            var roots = _quadTree.GetRelevantRoots(flatPos, Settings.ChunkMeshSize, QuadTreeLodConfig.RootSizeInCells);
            _tree ??= new List<QuadTreeNode>();
            foreach (Vector2Int root in roots)
            {
                Vector3 p = new Vector3(root.x * Settings.ChunkMeshSize, 0, root.y * Settings.ChunkMeshSize);
                Gizmos.color = new Color(.7f, .5f, .9f, 1); 
                Gizmos.DrawSphere(p, Mathf.Min(UnityEditor.HandleUtility.GetHandleSize(p) * 0.5f, 200));
                _quadTree.Build(root, QuadTreeLodConfig.RootSizeInCells, flatPos, Settings.ChunkMeshSize, _tree);
                foreach (QuadTreeNode node in _tree)
                {
                    Vector3 min = new Vector3(node.CellMin.x * Settings.ChunkMeshSize, 0, node.CellMin.y * Settings.ChunkMeshSize);
                    Vector3 max = new Vector3(node.CellMax.x * Settings.ChunkMeshSize, 0, node.CellMax.y * Settings.ChunkMeshSize);
                    Vector3 center = (min + max) * 0.5f;
                    Vector3 size = max - min;
                    size *= 0.9f;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(center, size);
                }
            }
            Gizmos.matrix = defaultMatrix;
        }

        private void DrawBoundsForProps(IEnumerable<Vector2Int> props)
        {
            Gizmos.color = Color.white * 0.2f;
            foreach (Vector2Int position in props)
            {
                Vector3 center = GetPropCenter(position) + Vector3.up * Settings.Height * 0.5f;
                Vector3 size = new Vector3(Settings.ChunkMeshSize, Settings.Height, Settings.ChunkMeshSize);
                Gizmos.DrawWireCube(center, size);
            }
            Gizmos.color = Color.white;
        }

        private Vector3 GetPropCenter(Vector2Int position)
        {
            return new Vector3(position.x + 0.5f, 0, position.y + 0.5f) * Settings.ChunkMeshSize;
        }

        private void OnDestroy()
        {
            if (_terrainData != null)
            {
                _terrainData.MapChanged -= BindMapToMaterial;
                _terrainData.RawDataChanged -= ScheduleBake;
                _terrainData.Dispose();
            }
            _biomeDebugBaker?.Dispose();
            _noFarTint?.Dispose();
            _noFarTint = null;
            _lodMap.Dispose();
            if (_surfaces != null)
            {
                _surfaces.Changed -= OnSurfaceTypeChanged;
                _surfaces.Dispose();
            }
            DestroyTexture(_heightGradientTex);
            DestroyTexture(_generatedSlotTex);
            if (_composer != null)
            {
                _composer.TilesLoaded -= ScheduleBakeBeforeRendering;
                _composer.Dispose();
            }
            if (_biomes != null)
            {
                _biomes.Changed -= OnBiomesChanged;
            }
            _visibleChunks.Clear();
            _activePatches.Clear(); 
            _deformersByChunk.Clear();
            UnsubscribeFilters();
            UnsubscribeBiomeFilters();
            if (_settings)
            {
                _settings.Changed -= OnSettingsChanged;
            }
            //_deformersQueue.Clear();
            _tickService.Remove(this);
            OnInitialize.Reset();
        }

        public void InstallBindings(DiContainer container)
        {
            //container.BindInstance(this);
            container.Bind<ITerrainProviderHandler>().FromInstance(this);
            container.Bind<ITerrainGpuData>().FromInstance(this);
            container.Bind<ITerrainFarTint>().FromInstance(this);
        }

        public bool IsDeformersClear()
        {
            return true;//_deformersQueue.Count == 0;
        }

        //public Task ProcessDeformersTask()
        //{
        //    return _deformersQueueTask;
        //}
    }
}