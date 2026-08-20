using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Core.Utilities;
using System.Threading.Tasks;
using Core.Explorer;
using Core.Misc;
using Core.TerrainGenerator.Settings;
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
            Task Initialize();
            Task LoadPropsForCurrentPosition();
            void Unload();
            bool Enabled { get; }
            public HeightmapData GetHeightmapData();
        }
        
        public static readonly LateEvent<TerrainProvider> OnInitialize = new ();
        public static float MaxWorldHeight { get; private set; }
        [SerializeField] private TerrainGenerationSettings _settings;
        [Inject] private TickService _tickService;
        [Inject(Id = "Player")] private IDynamicPositionProvider _playerTracker;

        [ShowInInspector]
        private Dictionary<Vector2Int, List<DeformationChannel>> _activeChunkChannels = new ();
        private Dictionary<Vector2Int, List<DeformationChannel>> _inactiveChunkChannels = new ();
        private Dictionary<Vector2Int, IChunk> _inactiveChunks = new ();
        private Dictionary<Vector2Int, IChunk> _activeChunks = new ();
        private Dictionary<Vector2Int, HashSet<IDeformer>> _deformersByChunk = new ();
        private List<IDeformer> _deformersQueue = new ();
        [ShowInInspector] private HeightmapData _heightmapData;
        [ShowInInspector] RenderTexture HeightmapTexture => _heightmapData?.HeightmapTex;
        [ShowInInspector] Dictionary<Vector2Int, int2> ActiveChunks => _heightmapData?._activeChunks;
        public TerrainGenerationSettings Settings => _settings;
        public int TickRate => 60;
        
        public HeightmapData GetHeightmapData()
        {
            if (_heightmapData == null)
            {
                _heightmapData = new HeightmapData(Settings.MaxLoadedChunksByOneSide, Settings.HeightmapResolution);
            }
            return _heightmapData;
        }

        public IEnumerable<(Vector2Int, HeightChannel)> EnumerateActiveSurfaceChannels()
        {
            foreach (var activeChunkChannel in _activeChunkChannels)
            {
                yield return (activeChunkChannel.Key, activeChunkChannel.Value[_heightChannelIndex] as HeightChannel);
            }
        }

        public IChunk GetChunk(Vector2Int position)
        {
            return _activeChunks[position];
        }

        public IEnumerable<IChunk> GetActiveChunks()
        {
            return _activeChunks.Values;
        }

        bool ILoadAtStart.enabled
        {
            get => enabled && gameObject.activeInHierarchy;
        }

        Task ILoadAtStart.Load()
        {
            return Initialize();
        }

        Task ITerrainProviderHandler.Initialize()
        {
            return Initialize();
        }
        
        private async Task Initialize()
        {
            if (Settings.directory == null) throw new System.Exception("Wrong directory!");
            
            WorldOffset.OnWorldOffsetChange += OnWorldOffsetChange;
            MaxWorldHeight = Mathf.Max(MaxWorldHeight, Settings.Height);
            if (Application.isPlaying)
            {
                _tickService.Add(this);
            }
            for (var i = 0; i < Settings.Settings.Count; i++)
            {
                if (Settings.Settings[i] is MeshHeightmapChannelSettings)
                {
                    _heightChannelIndex = i;
                    break;
                }
            }

            await LoadPropsForCurrentPosition();
            OnInitialize.Invoke(this);
            if (_deformersQueueTask != null)
            {
                await _deformersQueueTask;
            } 
        }

        Task ITerrainProviderHandler.LoadPropsForCurrentPosition()
        {
            return LoadPropsForCurrentPosition();
        }
        
        public void Tick()
        {
            LoadPropsForCurrentPosition().AsUniTask().Forget();
        }
        
        private async Task LoadPropsForCurrentPosition()
        {
            var props = GetCurrentProps();
            await Load(props);
        }

        public bool Enabled => gameObject.activeInHierarchy && enabled;

        void ITerrainProviderHandler.Unload()
        {
            foreach (KeyValuePair<Vector2Int, IChunk> chunk in _activeChunks)
            {
                chunk.Value?.Disable();
            }
            _activeChunks.Clear();
        }

        private void OnWorldOffsetChange(Vector3 offset)
        {
            transform.position += offset;
            foreach (KeyValuePair<Vector2Int, IChunk> chunk in _activeChunks)
            {
                chunk.Value.RefreshPosition();
            }
        }
        HashSet<Vector2Int> _toRemoveCache = new HashSet<Vector2Int>();
        HashSet<Vector2Int> _toCreateCache = new HashSet<Vector2Int>();
        HashSet<Vector2Int> _toUpdateCache = new HashSet<Vector2Int>();
        private async Task Load(IEnumerable<Vector2Int> props)
        {
            foreach (KeyValuePair<Vector2Int, IChunk> chunk in _activeChunks)
            {
                chunk.Value.IsChunkVisible = false;
            }
            
            foreach (Vector2Int prop in props)
            {
                if (!_activeChunks.ContainsKey(prop))
                {
                    _activeChunks.Add(prop, null);
                }
                else
                {
                    _activeChunks[prop].IsChunkVisible = true;
                }
            }


            foreach (KeyValuePair<Vector2Int, IChunk> chunk in _activeChunks)
            {
                if (chunk.Value == null)
                {
                    _toCreateCache.Add(chunk.Key);
                }
                else
                {
                    if (chunk.Value.IsChunkVisible == false)
                    {
                        _toRemoveCache.Add(chunk.Key);
                    }
                    else
                    {
                        _toUpdateCache.Add(chunk.Key);
                    }
                }
            }
            foreach (Vector2Int coord in _toRemoveCache)
            {
                _activeChunks.Remove(coord, out var chunk);
                _inactiveChunks.Add(coord, chunk);
                chunk.Disable();
                var channels = _activeChunkChannels[coord];
                foreach (var deformationChannel in channels)
                {
                    deformationChannel.OnChunkUnload();
                }
                _inactiveChunkChannels[coord] = channels;
                _activeChunkChannels.Remove(coord);
            }

            foreach (Vector2Int coord in _toCreateCache)
            {
                if (!_inactiveChunks.Remove(coord, out var chunk))
                {
                    chunk = CreateTerrain(coord);
                    if (chunk == null)
                    {
                        continue;
                    }
                }
                else
                {
                    chunk.Enable();
                }
                
                _activeChunks[coord] = chunk;
                if (_inactiveChunkChannels.Remove(coord, out List<DeformationChannel> channels))
                {
                    //Debug.Log($"Reuse chunk {coord}");
                    foreach (var deformationChannel in channels)
                    {
                        deformationChannel.OnChunkLoad();
                    }
                    _activeChunkChannels.Add(coord, channels);
                    foreach (var deformationChannel in channels)
                    {
                        deformationChannel.SetChunk(_activeChunks[coord]);
                    }
                }
                else
                {
                    //Debug.Log($"Create chunk {coord}");
                    _activeChunkChannels.Add(coord, new List<DeformationChannel>());
                    foreach (ChannelSettings layerSettings in Settings.Settings)
                    {
                        DeformationChannel channel =
                            layerSettings.MakeDeformationChannel(this, coord, Settings.directory.FullName);

                        if (channel != null) _activeChunkChannels[coord].Add(channel);
                    }
                }
            }
            foreach (var coord in _toUpdateCache)
            {
                _activeChunks[coord].OnChunksRefreshed();
            }
            await AwaitForReadyAndApply();
            _toRemoveCache.Clear();
            _toCreateCache.Clear();
            _toUpdateCache.Clear();
        }

        public async void RefreshProps()
        {
            UnityEngine.Profiling.Profiler.BeginSample("TERRAIN");
            await LoadPropsForCurrentPosition();
            UnityEngine.Profiling.Profiler.EndSample();
        }
        
        private async Task AwaitForReadyAndApply()
        {
            UnityEngine.Profiling.Profiler.BeginSample("Apply changes");
            await Task.WhenAll(_activeChunkChannels.SelectMany(x => x.Value.Select(WaitForChannelLoadingAndApply)));
            await Task.WhenAll(_activeChunkChannels.SelectMany(x => x.Value.Select(v => v.PostApply())));
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private async Task WaitForChannelLoadingAndApply(DeformationChannel channel)
        {
            if (channel.IsDirty)
            {
                await channel.LoadingTask;
                if (_deformersByChunk.TryGetValue(channel.Coordinates, out HashSet<IDeformer> deformers))
                {
                    foreach (IDeformer deformer in deformers)
                    {
                        channel.RegisterDeformer(deformer);
                    }
                    channel.ApplyDirtyToCache();
                }
                await channel.Apply();
            }
        }

        private IEnumerable<Vector2Int> GetCurrentProps()
        {
            Vector3 viewPosition = GetViewPosition(); 

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
            }
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
            _lastViewPosition = _playerTracker.GetPredictedWorldPosition(2, 100);
            _lastViewPosition.y = 0;
            return _lastViewPosition;
        }
        

        private IChunk CreateTerrain(Vector2Int prop)
        {
            try
            {
                var instance = new GameObject();
                instance.transform.SetParent(transform);
                IChunk chunk = instance.AddComponent<ChunkByTesselation>().Init($"Terrain ({prop.x}, {prop.y})", prop, transform, Settings, GetHeightmapData());
                return chunk;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private Task _deformersQueueTimer;
        private Task _deformersQueueTask;
        private Vector3 _lastViewPosition;
        private int _heightChannelIndex;

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
                if (_activeChunkChannels.TryGetValue(coord, out List<DeformationChannel> channelsList))
                {
                    foreach (DeformationChannel channel in channelsList)
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

            foreach (List<DeformationChannel> deformationChannels in _activeChunkChannels.Values)
            {
                foreach (DeformationChannel deformationChannel in deformationChannels)
                {
                    if (deformationChannel.IsDirty) deformationChannel.ApplyDirtyToCache();
                }
            }

            await Task.WhenAll(_activeChunkChannels.SelectMany(x => x.Value.Select(v => v.IsDirty ? v.Apply() : Task.CompletedTask)));
            await Task.WhenAll(_activeChunkChannels.SelectMany(x => x.Value.Select(v => v.PostApply())));

            _deformersQueue.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (!Settings) return;
            DrawBoundsForProps(GetCurrentProps());
            
            Gizmos.color = Color.white * 0.5f;
            Matrix4x4 defaultMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 0, 1));
            Gizmos.DrawWireSphere(GetViewPosition(), Settings.VisibleDistance);
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
            _heightmapData.Dispose();
            _activeChunkChannels.Clear();
            _inactiveChunkChannels.Clear();
            _activeChunks.Clear(); 
            _deformersByChunk.Clear();
            _deformersQueue.Clear();
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
            return _deformersQueue.Count == 0;
        }

        public Task ProcessDeformersTask()
        {
            return _deformersQueueTask;
        }
    }
}