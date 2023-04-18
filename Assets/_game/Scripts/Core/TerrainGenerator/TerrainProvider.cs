using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Paterns;
using Core.Utilities;
using Core.Boot_strapper;
using System.Threading.Tasks;
using Cinemachine;
using Core.Game;
using Core.SessionManager;
using Core.TerrainGenerator.Settings;
using Sirenix.OdinInspector;
using Core.TerrainGenerator.Utility;
using Sirenix.Serialization;
using UnityEditor;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// runtime generating terrain chunks by TerrainGenerationSettings
    /// </summary>
    public class TerrainProvider : MonoBehaviour, ILoadAtStart
    {
        public static TerrainProvider Instance;
        public static readonly LateEvent OnInitialize = new LateEvent();
       
        public TerrainGenerationSettings settings;

        [ShowInInspector]
        private Dictionary<Vector2Int, List<DeformationChannel>> channels =
            new Dictionary<Vector2Int, List<DeformationChannel>>();

        private Dictionary<Vector2Int, Terrain> chunks = new Dictionary<Vector2Int, Terrain>();
        private List<TerrainData> terrainsData = new List<TerrainData>();
        private List<IDeformer> deformers = new List<IDeformer>();
        private List<IDeformer> deformersQueue = new List<IDeformer>();

        private PropsRefreshTicker refreshTicker;

        public static Terrain GetTerrain(Vector2Int position)
        {
            return Instance.chunks[position];
        }

        [Button]
        private void TestLoad()
        {
            Instance = this;
            Load(GetCurrentProps());
            foreach (Deformer deformer in FindObjectsOfType<Deformer>())
            {
                deformer.Start();
            }
        }
        [Button]
        private void RemoveTest()
        {
            foreach (Terrain chunksValue in chunks.Values)
            {
                if(chunksValue != null) DestroyImmediate(chunksValue.gameObject);
            }

            foreach (TerrainData data in terrainsData)
            {
                if(terrainsData != null) DestroyImmediate(data);
            }

            channels = new Dictionary<Vector2Int, List<DeformationChannel>>();
            deformers = new List<IDeformer>();
            deformersQueue = new List<IDeformer>();
        }

        private TaskCompletionSource<bool> deformersInitialization;
        async Task ILoadAtStart.Load()
        {
            Instance = this;
            WorldOffset.OnWorldOffsetChange += OnWorldOffsetChange;
            deformersInitialization = new TaskCompletionSource<bool>();
            
            if (settings.directory == null) throw new System.Exception("Wrong directory!");

            await LoadPropsForCurrentPosition();
            refreshTicker?.TryRun();
            await deformersInitialization.Task;
        }

        private async Task LoadPropsForCurrentPosition()
        {
            await Load(GetCurrentProps());
        }

        private void OnWorldOffsetChange(Vector3 offset)
        {
            transform.position += offset;
            foreach (KeyValuePair<Vector2Int, Terrain> chunk in chunks)
            {
                chunk.Value.transform.position += offset;
            }
        }

        private Task Load(IEnumerable<Vector2Int> props)
        {
            foreach (Vector2Int prop in props)
            {
                if (!chunks.ContainsKey(prop))
                {
                    var terrain = CreateTerrain(prop);

                    terrainsData.Add(terrain.terrainData);
                    chunks.Add(prop, terrain);
                }

                if (!channels.ContainsKey(prop))
                {
                    channels.Add(prop, new List<DeformationChannel>());
                    
                    foreach (ChannelSettings layerSettings in settings.Settings)
                    {
                        DeformationChannel channel = layerSettings.MakeDeformationChannel(prop, settings.directory.FullName);
                        if (channel != null) channels[prop].Add(channel);
                    }
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
            foreach (KeyValuePair<Vector2Int, List<DeformationChannel>> channelKV in channels)
            {
                foreach (DeformationChannel channel in channelKV.Value)
                {
                    while (!channel.IsReady)
                    {
                        await Task.Delay(100);
                    }
                }
            }

            UnityEngine.Profiling.Profiler.BeginSample("Apply changes");

            foreach (KeyValuePair<Vector2Int, List<DeformationChannel>> layer in channels)
            {
                foreach (DeformationChannel terrainLayer in layer.Value)
                {
                    terrainLayer.Apply();
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();

            await Task.Delay(1000);

            OnInitialize.Invoke();
            
            if(deformersQueueTimer == null ) deformersInitialization.TrySetResult(true);
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

        private bool isCameraInitialized;
        private CinemachineBrain mainCamera;

        private Vector3 GetViewPosition()
        {
            if (!isCameraInitialized)
            {
                Camera cam = Camera.main;
                if (cam)
                {
                    mainCamera = cam.GetComponent<CinemachineBrain>();
                    refreshTicker = new PropsRefreshTicker(mainCamera, settings.ChunksRefreshDistance, this);
                    refreshTicker.TryRun();
                    isCameraInitialized = true;
                }
                else return Vector3.zero;
            }

            Vector3 pos;
            ICinemachineCamera activeVirtualCamera = mainCamera.ActiveVirtualCamera;
            if (activeVirtualCamera == null)
            {
                pos = mainCamera.transform.position;
            }
            else
            {
                pos  = activeVirtualCamera.VirtualCameraGameObject.transform.position - WorldOffset.Offset;
            }
            pos.y = 0;
            return pos;
        }
        

        private Terrain CreateTerrain(Vector2Int prop)
        {
            GameObject obj = new GameObject($"Terrain ({prop.x}, {prop.y})");

            Vector3 selfPos = transform.position;
            obj.transform.position = new Vector3(selfPos.x + prop.x * settings.ChunkSize, selfPos.y,
                selfPos.z + prop.y * settings.ChunkSize);

            Terrain ter = obj.AddComponent<Terrain>();
            TerrainData data = new TerrainData();
            data.name = obj.name;
            ter.terrainData = data;

            ter.drawInstanced = true;
            data.heightmapResolution = settings.HeightmapResolution;
            data.alphamapResolution = settings.AlphamapResolution;
            data.size = new Vector3(settings.ChunkSize, settings.Height, settings.ChunkSize);
            ter.materialTemplate = settings.Material;

            TerrainCollider collider = obj.AddComponent<TerrainCollider>();
            collider.terrainData = ter.terrainData;

            ter.allowAutoConnect = true;

            return ter;
        }

        private Task deformersQueueTimer;

        public async void RegisterDeformer(IDeformer deformer)
        {
            deformersQueue.Add(deformer);
            deformers.Add(deformer);

            if (deformersQueueTimer == null)
            {
                deformersQueueTimer = LaunchDeformersQueue();
                await deformersQueueTimer;
            }
        }

        private void ApplyToChannels(IDeformer deformer)
        {
            IEnumerable<Vector2Int> affectChunks = deformer.GetAffectChunks(settings.ChunkSize);

            Vector2Int[] chunksArr = affectChunks as Vector2Int[] ?? affectChunks.ToArray();

            foreach (Vector2Int chunk in chunksArr)
            {
                if (channels.TryGetValue(chunk, out List<DeformationChannel> channelsList))
                {
                    foreach (DeformationChannel channel in channelsList)
                    {
                        channel.RegisterDeformer(deformer);
                    }
                }
            }
        }

        private async Task LaunchDeformersQueue()
        {
            await Task.Delay(2000);
            deformersQueueTimer = null;

            foreach (IDeformer deformer in deformersQueue)
            {
                ApplyToChannels(deformer);
            }

            foreach (List<DeformationChannel> deformationChannels in channels.Values)
            {
                foreach (DeformationChannel deformationChannel in deformationChannels)
                {
                    if (deformationChannel.IsDirty) deformationChannel.ApplyDirtyToCache();
                }
            }

            foreach (List<DeformationChannel> deformationChannels in channels.Values)
            {
                foreach (DeformationChannel deformationChannel in deformationChannels)
                {
                    if (deformationChannel.IsDirty) deformationChannel.Apply();
                }
            }

            deformersQueue.Clear();
            if (deformersInitialization != null)
            {
                deformersInitialization.SetResult(true);
                deformersInitialization = null;
            }
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
    }
}