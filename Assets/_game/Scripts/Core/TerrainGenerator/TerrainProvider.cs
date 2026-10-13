using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Core.Utilities;
using System.Threading.Tasks;
using Core.Explorer;
using Core.Misc;
using Core.TerrainGenerator.Filters;
using Core.TerrainGenerator.LOD;
using Core.TerrainGenerator.Settings;
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
    public class TerrainProvider : MonoBehaviour, ILoadAtStart, IMyInstaller, TerrainProvider.ITerrainProviderHandler, ITickable
    {
        public interface ITerrainProviderHandler
        {
            public TerrainGenerationSettings Settings { get; }
            void Initialize(bool force = false);
            void LoadPropsForCurrentPosition();
            void Unload();
            bool Enabled { get; }
            public TerrainData GetHeightmapData();
        }
        
        public static readonly LateEvent<TerrainProvider> OnInitialize = new ();
        private static readonly int SourceAlphamap = Shader.PropertyToID("source_alphamap");
        [SerializeField] private TerrainGenerationSettings _settings;
        [Inject] private TickService _tickService;
        [Inject(Id = "Player")] private IDynamicPositionProvider _playerTracker;

        [ShowInInspector]
        private Dictionary<Vector2Int, ChunkData> _activeChunks = new ();
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
        private readonly List<IHeightmapFilter> _filters = new ();
        private readonly HeightmapBaker _heightmapBaker = new ();
        private bool _bakeScheduled;
        private TerrainQuadTree _quadTree;
        
        public TerrainData GetHeightmapData()
        {
            return _terrainData;
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
            if (Settings.Directory == null) throw new System.Exception("Wrong directory!");
            CollectFilters();
            
            if (_terrainData == null)
            {
                _terrainData = new TerrainData(Settings.MaxLoadedChunksByOneSide, Settings.HeightmapResolution);
                _terrainData.MapChanged += BindMapToMaterial;
                _terrainData.RawDataChanged += ScheduleBake;
                _settings.Material.SetTexture(SourceAlphamap, AlphamapTexture);
            }
            BindMaterialParams();
            BindMapToMaterial();
            ScheduleBake();

            WorldOffset.OnWorldOffsetChange -= OnWorldOffsetChange;
            WorldOffset.OnWorldOffsetChange += OnWorldOffsetChange;
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
            ScheduleBake();
        }

        private void CollectFilters()
        {
            UnsubscribeFilters();
            _filters.AddRange(Settings.HeightmapFilters);
            foreach (IHeightmapFilter filter in _filters)
            {
                filter.Changed += ScheduleBake;
            }
        }

        private void UnsubscribeFilters()
        {
            foreach (IHeightmapFilter filter in _filters)
            {
                if (filter != null)
                {
                    filter.Changed -= ScheduleBake;
                }
            }
            _filters.Clear();
        }

        /// <summary>
        /// Coalesces bake requests (chunk uploads, filter changes) into a single bake per frame,
        /// done before rendering.
        /// </summary>
        private void ScheduleBake()
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

            // Newly uploaded chunks enter the map here, so they never show unbaked heights
            _terrainData.FlushMap();
            _heightmapBaker.Bake(_terrainData, _filters, _settings.heightmapSettings.chunkSizeMeters, _settings.Height,
                _settings.heightmapSettings.sharedEdgePixels);
        }
        
        public void LoadPropsForCurrentPosition()
        {
            _quadTree ??= new TerrainQuadTree(Settings.lodConfig);
            CollectCurrentLeaves();
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
            Settings.Material.SetBuffer(Map, _terrainData.GetMapBuffer(out Vector2Int mapMin, out int mapSize));
            float hmPixSize = 1f / _settings.heightmapSettings.chunkSizeMeters;//((_settings.HeightmapResolution + 2) * _settings.MaxLoadedChunksByOneSide);
            Settings.Material.SetVector(PositionToChunkMatrix, new Vector4(hmPixSize, hmPixSize, mapMin.x * _settings.heightmapSettings.chunkSizeMeters - WorldOffset.Offset.x, mapMin.y * _settings.heightmapSettings.chunkSizeMeters - WorldOffset.Offset.z));
            Settings.Material.SetFloat(MapSize, mapSize);
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
        HashSet<Vector2Int> _toRemoveCache = new HashSet<Vector2Int>();
        HashSet<Vector2Int> _toCreateCache = new HashSet<Vector2Int>();
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
            foreach (var chunk in _activeChunks)
            {
                if (chunk.Value != null)
                {
                    chunk.Value.IsChunkVisible = false;
                }
            }

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
            Vector2Int size = max - min;
            Rect minMaxChunk = new Rect((Vector2)min * Settings.ChunkMeshSize / _settings.heightmapSettings.chunkSizeMeters, (Vector2)size * Settings.ChunkMeshSize / _settings.heightmapSettings.chunkSizeMeters);
            RectInt chunksRange = new RectInt(new Vector2Int(Mathf.FloorToInt(minMaxChunk.xMin), Mathf.FloorToInt(minMaxChunk.yMin)), new Vector2Int(Mathf.CeilToInt(minMaxChunk.size.x), Mathf.CeilToInt(minMaxChunk.size.y)));

            foreach (var cell in chunksRange.EnumerateYFirst())
            {
                if (!_activeChunks.TryGetValue(cell, out var chunk) || chunk == null)
                {
                    _toCreateCache.Add(cell);
                }
                else
                {
                    chunk.IsChunkVisible = true;
                }
            }


            foreach (var chunk in _activeChunks)
            {
                if (chunk.Value is { IsChunkVisible: false })
                {
                    _toRemoveCache.Add(chunk.Key);
                }
            }
            foreach (Vector2Int coord in _toRemoveCache)
            {
                _activeChunks[coord].Dispose();
                _activeChunks.Remove(coord);
            }

            foreach (Vector2Int coord in _toCreateCache)
            {
                //Debug.Log($"Create chunk {coord}");
                string heightmapPath = Settings.heightmapSettings.GetPath(coord + Settings.ChunksCenter, Settings.Directory.FullName);
                string alphamapPath = Settings.alphamapSettings.GetPath(coord + Settings.ChunksCenter, Settings.Directory.FullName);
                ChunkData chunkData = new ChunkData(heightmapPath, alphamapPath, this, coord);
                _activeChunks[coord] = chunkData;
            }
            /*foreach (var coord in _toUpdateCache)
            {
                _activePatches[coord].OnChunksRefreshed();
            }*/
            _toRemoveCache.Clear();
            _toCreateCache.Clear();
            //_toUpdateCache.Clear();
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
        
        private void BindMaterialParams()
        {
            Settings.Material.SetTexture(SourceHeightmap, _terrainData.HeightmapTex);
            Settings.Material.SetFloat(HeightScale, _settings.Height);
            Settings.Material.SetFloat(WidthScale, _settings.ChunkMeshSize);
            Settings.Material.SetFloat(SlotsCountInv, 1f / _settings.MaxLoadedChunksByOneSide);
            Settings.Material.SetFloat(HeightmapChunkResolution, _settings.HeightmapResolution + 2); // 2 pix for border
            Settings.Material.SetFloat(PixelSizeUVSpace, 1f / (_settings.HeightmapResolution + 2));
            Settings.Material.SetFloat(HeightmapSharedEdges, _settings.heightmapSettings.sharedEdgePixels ? 1f : 0f);
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
            _tree ??= new List<QuadTreeNode>();
            foreach (var root in _quadTree.GetRelevantRoots(flatPos, Settings.ChunkMeshSize, 8))
            {
                _quadTree.Build(root, 8, flatPos, Settings.ChunkMeshSize, _tree);
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

            Vector3 pos = _playerTracker.SpacePosition;
            var flatPos = new Vector2(pos.x, pos.z);
            var roots = _quadTree.GetRelevantRoots(flatPos, Settings.ChunkMeshSize, 8);
            _tree ??= new List<QuadTreeNode>();
            foreach (Vector2Int root in roots)
            {
                Vector3 p = new Vector3(root.x * Settings.ChunkMeshSize, 0, root.y * Settings.ChunkMeshSize);
                Gizmos.color = new Color(.7f, .5f, .9f, 1); 
                Gizmos.DrawSphere(p, Mathf.Min(UnityEditor.HandleUtility.GetHandleSize(p) * 0.5f, 200));
                _quadTree.Build(root, 8, flatPos, Settings.ChunkMeshSize, _tree);
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
            _activeChunks.Clear();
            _activePatches.Clear(); 
            _deformersByChunk.Clear();
            UnsubscribeFilters();
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