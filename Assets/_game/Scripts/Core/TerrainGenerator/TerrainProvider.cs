using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Core.Utilities;
using Core.Boot_strapper;
using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
using Sirenix.OdinInspector;
using Core.World;
using Runtime.Character;
using Zenject;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// runtime generating terrain chunks by TerrainGenerationSettings
    /// </summary>
    public class TerrainProvider : MonoBehaviour, ILoadAtStart
    {
        public static readonly LateEvent<TerrainProvider> OnInitialize = new LateEvent<TerrainProvider>();
        public static float MaxWorldHeight { get; private set; }
        public TerrainGenerationSettings settings;

        [ShowInInspector]
        private Dictionary<Vector2Int, List<DeformationChannel>> channels =
            new Dictionary<Vector2Int, List<DeformationChannel>>();

        private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

        private Dictionary<Vector2Int, HashSet<IDeformer>> deformersByChunk = new Dictionary<Vector2Int, HashSet<IDeformer>>();
        private List<IDeformer> deformersQueue = new List<IDeformer>();
        [Inject(Id = "Player")] private TransformTracker _playerTracker;

        public Chunk GetChunk(Vector2Int position)
        {
            return chunks[position];
        }

        bool ILoadAtStart.enabled
        {
            get => enabled && gameObject.activeInHierarchy;
        }

        [Button]
        private void TestLoad()
        {
            Load(GetCurrentProps());
            foreach (Deformer deformer in FindObjectsOfType<Deformer>())
            {
                deformer.Start();
            }
        }
        [Button]
        private void RemoveTest()
        {
            foreach (KeyValuePair<Vector2Int, Chunk> chunk in chunks)
            {
                chunk.Value?.Destroy();
            }

            chunks = new Dictionary<Vector2Int, Chunk>();
            
            channels = new Dictionary<Vector2Int, List<DeformationChannel>>();
            deformersByChunk = new Dictionary<Vector2Int, HashSet<IDeformer>>();
            deformersQueue = new List<IDeformer>();
        }

        async Task ILoadAtStart.Load()
        {
            WorldOffset.OnWorldOffsetChange += OnWorldOffsetChange;
            MaxWorldHeight = Mathf.Max(MaxWorldHeight, settings.Height);
            
            if (settings.directory == null) throw new System.Exception("Wrong directory!");
            await LoadPropsForCurrentPosition();
            OnInitialize.Invoke(this);
            if (deformersQueueTask != null)
            {
                await deformersQueueTask;
            }
        }

        private async Task LoadPropsForCurrentPosition()
        {
            await Load(GetCurrentProps());
        }

        private void OnWorldOffsetChange(Vector3 offset)
        {
            transform.position += offset;
            foreach (KeyValuePair<Vector2Int, Chunk> chunk in chunks)
            {
                chunk.Value.Position += offset;
            }
        }

        private Task Load(IEnumerable<Vector2Int> props)
        {
            foreach (KeyValuePair<Vector2Int, Chunk> chunk in chunks)
            {
                chunk.Value.IsChunkVisible = false;
            }
            
            foreach (Vector2Int prop in props)
            {
                if (!chunks.ContainsKey(prop))
                {
                    chunks.Add(prop, null);
                }
                else
                {
                    chunks[prop].IsChunkVisible = true;
                }
            }

            HashSet<Vector2Int> toRemove = new HashSet<Vector2Int>();
            HashSet<Vector2Int> toCreate = new HashSet<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, Chunk> chunk in chunks)
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
                chunks.Remove(coord);
                channels.Remove(coord);
            }

            foreach (Vector2Int coord in toCreate)
            {
                chunks[coord] = CreateTerrain(coord);
                channels.Add(coord, new List<DeformationChannel>());
                foreach (ChannelSettings layerSettings in settings.Settings)
                {
                    DeformationChannel channel =
                        layerSettings.MakeDeformationChannel(this, coord, settings.directory.FullName);

                    if (channel != null) channels[coord].Add(channel);
                }
            }

            return AwaitForReadyAndApply();
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
            await Task.WhenAll(channels.SelectMany(x => x.Value.Select(WaitForChannelLoadingAndApply)));
            await Task.WhenAll(channels.SelectMany(x => x.Value.Select(v => v.PostApply())));
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private async Task WaitForChannelLoadingAndApply(DeformationChannel channel)
        {
            if (channel.IsDirty)
            {
                await channel.LoadingTask;
                if (deformersByChunk.TryGetValue(channel.Coordinates, out HashSet<IDeformer> deformers))
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

            Vector2Int viewPositionInt = new Vector2Int(Mathf.FloorToInt(viewCell.x), Mathf.FloorToInt(viewCell.y));

            for (int x = viewPositionInt.x - 8; x <= viewPositionInt.x + 8; x++)
            {
                for (int y = viewPositionInt.y - 8; y <= viewPositionInt.y + 8; y++)
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
            /*if (!_playerTracker)
            {
                return FindAnyObjectByType<SpawnPerson>().transform.position;
            }*/
            Vector3 pos = _playerTracker.GetPredictedPosition(20) + WorldOffset.Offset;
            pos.y = 0;
            return pos;
        }
        

        private Chunk CreateTerrain(Vector2Int prop)
        {
            Chunk chunk = new Chunk($"Terrain ({prop.x}, {prop.y})", transform, settings);

            Vector3 selfPos = transform.position;
            chunk.Position = new Vector3(selfPos.x + prop.x * settings.ChunkSize, selfPos.y,
                selfPos.z + prop.y * settings.ChunkSize);

            return chunk;
        }

        private Task deformersQueueTimer;
        private Task deformersQueueTask;
        public void RegisterDeformer(IDeformer deformer)
        {
            deformersQueue.Add(deformer);
            IEnumerable<Vector2Int> affected = deformer.GetAffectChunks(settings.ChunkSize);
            foreach (Vector2Int coord in affected)
            {
                if (!deformersByChunk.ContainsKey(coord))
                {
                    deformersByChunk.Add(coord, new HashSet<IDeformer>());
                }
                
                deformersByChunk[coord].Add(deformer);
                if (channels.TryGetValue(coord, out List<DeformationChannel> channelsList))
                {
                    foreach (DeformationChannel channel in channelsList)
                    {
                        channel.RegisterDeformer(deformer);
                    }
                }
            }

            if (deformersQueueTimer == null)
            {
                TaskCompletionSource<bool> queueCompletionSource = new TaskCompletionSource<bool>();
                deformersQueueTimer = queueCompletionSource.Task;
                deformersQueueTask = LaunchDeformersQueue(queueCompletionSource);
                WaitForDeformersQueueAndSetTaskNull();
            }
        }

        private async void WaitForDeformersQueueAndSetTaskNull()
        {
            await deformersQueueTask;
            deformersQueueTask = null;
        }
        

        private async Task LaunchDeformersQueue(TaskCompletionSource<bool> queueCompletionSource)
        {
            await Task.Delay(2000);
            queueCompletionSource.SetResult(true);
            deformersQueueTimer = null;

            foreach (List<DeformationChannel> deformationChannels in channels.Values)
            {
                foreach (DeformationChannel deformationChannel in deformationChannels)
                {
                    if (deformationChannel.IsDirty) deformationChannel.ApplyDirtyToCache();
                }
            }

            await Task.WhenAll(channels.SelectMany(x => x.Value.Select(v => v.IsDirty ? v.Apply() : Task.CompletedTask)));
            await Task.WhenAll(channels.SelectMany(x => x.Value.Select(v => v.PostApply())));

            deformersQueue.Clear();
        }

        private void OnDrawGizmos()
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
            channels.Clear();
            chunks.Clear(); 
            deformersByChunk.Clear();               
            deformersQueue.Clear();       
            OnInitialize.Reset();
        }
    }
}