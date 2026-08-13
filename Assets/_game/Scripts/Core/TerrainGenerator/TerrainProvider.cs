using System;
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
            Task Initialize();
            Task LoadPropsForCurrentPosition();
            void Unload();
            bool Enabled { get; }
        }
        
        public static readonly LateEvent<TerrainProvider> OnInitialize = new ();
        public static float MaxWorldHeight { get; private set; }
        public TerrainGenerationSettings settings;
        [Inject] private TickService _tickService;
        [Inject(Id = "Player")] private IDynamicPositionProvider _playerTracker;

        [ShowInInspector]
        private Dictionary<Vector2Int, List<DeformationChannel>> _activeChunkChannels = new ();
        private Dictionary<Vector2Int, List<DeformationChannel>> _inactiveChunkChannels = new ();
        private Dictionary<Vector2Int, Chunk> _chunks = new ();
        private Dictionary<Vector2Int, HashSet<IDeformer>> _deformersByChunk = new ();
        private List<IDeformer> _deformersQueue = new ();
        private Collision _collision;
        private HeightmapData _heightmapData;
        [ShowInInspector] RenderTexture HeightmapTexture => _heightmapData?.Texture;

        public int TickRate => 60;
        
        public HeightmapData GetHeightmapData()
        {
            return _heightmapData;
        }

        public IEnumerable<(Vector2Int, HeightChannel)> EnumerateActiveSurfaceChannels()
        {
            foreach (var activeChunkChannel in _activeChunkChannels)
            {
                yield return (activeChunkChannel.Key, activeChunkChannel.Value[_heightChannelIndex] as HeightChannel);
            }
        }

        public Chunk GetChunk(Vector2Int position)
        {
            return _chunks[position];
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
            if (settings.directory == null) throw new System.Exception("Wrong directory!");
            
            WorldOffset.OnWorldOffsetChange += OnWorldOffsetChange;
            MaxWorldHeight = Mathf.Max(MaxWorldHeight, settings.Height);
            if (Application.isPlaying)
            {
                _tickService.Add(this);
            }
            for (var i = 0; i < settings.Settings.Count; i++)
            {
                if (settings.Settings[i] is MeshHeightmapChannelSettings)
                {
                    _heightChannelIndex = i;
                    break;
                }
            }

            _heightmapData = new HeightmapData(settings.MaxLoadedChunksByOneSide, settings.HeightmapResolution);
            _collision = new Collision(this, settings.CollisionSettings);
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
            foreach (KeyValuePair<Vector2Int, Chunk> chunk in _chunks)
            {
                chunk.Value?.Destroy();
            }
            _chunks.Clear();
        }

        private void OnWorldOffsetChange(Vector3 offset)
        {
            transform.position += offset;
            foreach (KeyValuePair<Vector2Int, Chunk> chunk in _chunks)
            {
                chunk.Value.RefreshPosition();
            }
        }

        private async Task Load(IEnumerable<Vector2Int> props)
        {
            foreach (KeyValuePair<Vector2Int, Chunk> chunk in _chunks)
            {
                chunk.Value.IsChunkVisible = false;
            }
            
            foreach (Vector2Int prop in props)
            {
                if (!_chunks.ContainsKey(prop))
                {
                    _chunks.Add(prop, null);
                }
                else
                {
                    _chunks[prop].IsChunkVisible = true;
                }
            }

            HashSet<Vector2Int> toRemove = new HashSet<Vector2Int>();
            HashSet<Vector2Int> toCreate = new HashSet<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, Chunk> chunk in _chunks)
            {
                if (chunk.Value == null)
                {
                    toCreate.Add(chunk.Key);
                }
                else if(chunk.Value.IsChunkVisible == false)
                {
                    toRemove.Add(chunk.Key);
                    chunk.Value.Destroy();
                }
            }
            foreach (Vector2Int coord in toRemove)
            {
                _chunks.Remove(coord);
                var channels = _activeChunkChannels[coord];
                foreach (var deformationChannel in channels)
                {
                    deformationChannel.OnChunkUnload();
                }
                _inactiveChunkChannels[coord] = channels;
                _activeChunkChannels.Remove(coord);
            }

            foreach (Vector2Int coord in toCreate)
            {
                var t = CreateTerrain(coord);
                if (t == null)
                {
                    continue;
                }
                _chunks[coord] = t;
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
                        deformationChannel.SetChunk(_chunks[coord]);
                    }
                }
                else
                {
                    //Debug.Log($"Create chunk {coord}");
                    _activeChunkChannels.Add(coord, new List<DeformationChannel>());
                    foreach (ChannelSettings layerSettings in settings.Settings)
                    {
                        DeformationChannel channel =
                            layerSettings.MakeDeformationChannel(this, coord, settings.directory.FullName);

                        if (channel != null) _activeChunkChannels[coord].Add(channel);
                    }
                }
            }
            await AwaitForReadyAndApply();
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
            _collision?.UpdateTrackerPosition(_lastViewPosition, _lastViewCoord);
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

            float sI = 1f / settings.ChunkSize;
            Vector2 viewCell = new Vector2(viewPosition.x * sI, viewPosition.z * sI);

            _lastViewCoord = new Vector2Int(Mathf.FloorToInt(viewCell.x), Mathf.FloorToInt(viewCell.y));

            for (int x = _lastViewCoord.x - 8; x <= _lastViewCoord.x + 8; x++)
            {
                for (int y = _lastViewCoord.y - 8; y <= _lastViewCoord.y + 8; y++)
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
            Vector3 closestPointToProp = viewPosition + (center - viewPosition).normalized * Mathf.Min(settings.VisibleDistance, Vector3.Distance(center, viewPosition));
            Vector3 difference = closestPointToProp - center;
            difference.x = Mathf.Abs(difference.x);
            difference.z = Mathf.Abs(difference.z);
            return difference.x < settings.ChunkSize * 0.5f && difference.z < settings.ChunkSize * 0.5f;
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
        

        private Chunk CreateTerrain(Vector2Int prop)
        {
            try
            {
                Chunk chunk = new Chunk($"Terrain ({prop.x}, {prop.y})", prop, transform, settings);

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
        private Vector2Int _lastViewCoord;
        private int _heightChannelIndex;

        public void RegisterDeformer(IDeformer deformer)
        {
            _deformersQueue.Add(deformer);
            IEnumerable<Vector2Int> affected = deformer.GetAffectChunks(settings.ChunkSize);
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
            if (!settings) return;
            DrawBoundsForProps(GetCurrentProps());
            
            Gizmos.color = Color.white * 0.5f;
            Matrix4x4 defaultMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 0, 1));
            Gizmos.DrawWireSphere(GetViewPosition(), settings.VisibleDistance);
            Gizmos.matrix = defaultMatrix;
        }

        private void DrawBoundsForProps(IEnumerable<Vector2Int> props)
        {
            Gizmos.color = Color.white * 0.2f;
            foreach (Vector2Int position in props)
            {
                Vector3 center = GetPropCenter(position) + Vector3.up * settings.Height * 0.5f;
                Vector3 size = new Vector3(settings.ChunkSize, settings.Height, settings.ChunkSize);
                Gizmos.DrawWireCube(center, size);
            }
            Gizmos.color = Color.white;
        }

        private Vector3 GetPropCenter(Vector2Int position)
        {
            return new Vector3(position.x + 0.5f, 0, position.y + 0.5f) * settings.ChunkSize;
        }

        private void OnDestroy()
        {
            _heightmapData.Dispose();
            _activeChunkChannels.Clear();
            _inactiveChunkChannels.Clear();
            _chunks.Clear(); 
            _deformersByChunk.Clear();
            _deformersQueue.Clear();
            _tickService.Remove(this);
            OnInitialize.Reset();
        }

        public void InstallBindings(DiContainer container)
        {
            container.BindInstance(this);
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