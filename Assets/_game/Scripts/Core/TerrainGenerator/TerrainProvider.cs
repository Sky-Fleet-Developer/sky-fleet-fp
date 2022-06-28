using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Paterns;
using Core.Utilities;
using Core.Boot_strapper;
using System.Threading.Tasks;
using Core.SessionManager;
using Core.TerrainGenerator.Settings;
using Sirenix.OdinInspector;
using Core.TerrainGenerator.Utility;
using Runtime.Character.Control;
using UnityEditor;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// runtime generating terrain chunks by TerrainGenerationSettings
    /// </summary>
    public class TerrainProvider : MonoBehaviour, ILoadAtStart
    {
        public static TerrainProvider Instance;

        public TerrainGenerationSettings settings;

        [ShowInInspector]
        private Dictionary<Vector2Int, List<DeformationChannel>> channels = new Dictionary<Vector2Int, List<DeformationChannel>>();

        private Dictionary<Vector2Int, Terrain> chunks = new Dictionary<Vector2Int, Terrain>();
        private List<TerrainData> terrainsDates = new List<TerrainData>();
        private List<IDeformer> deformers = new List<IDeformer>();
        private List<IDeformer>  deformersQueue = new List<IDeformer>();

        public static LateEvent onInitialize = new LateEvent();
        
        public static Terrain GetTerrain(Vector2Int position)
        {
            return Instance.chunks[position];
        }

        [Button]
        public void TestLoad()
        {
            Load(new List<Vector2Int>()
            {
                Vector2Int.zero,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.one
            });
        }

        async Task ILoadAtStart.Load()
        {
            Instance = this;
            WorldOffset.OnWorldOffsetChange += OnWorldOffsetChange;
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

        public Task Load(IEnumerable<Vector2Int> props)
        {
            if (settings.directory == null) throw new System.Exception("Wrong directory!");

            foreach (Vector2Int prop in props)
            {
                if (!chunks.TryGetValue(prop, out Terrain terrain) || terrain == null)
                    terrain = CreateTerrain(prop);

                if (!channels.ContainsKey(prop))
                {
                    channels.Add(prop, new List<DeformationChannel>());
                }

                foreach (ChannelSettings layerSettings in settings.settings)
                {
                    DeformationChannel channel = layerSettings.MakeDeformationChannel(prop, settings.directory.FullName);
                    if (channel != null) channels[prop].Add(channel);
                }
            }

            return AwaitForReadyAndApply();
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


            foreach (KeyValuePair<Vector2Int, List<DeformationChannel>> layer in channels)
            {
                foreach (DeformationChannel terrainLayer in layer.Value)
                {
                    terrainLayer.Apply();
                }
            }

            await Task.Delay(1000);
            
            onInitialize.Invoke();
        }

        private IEnumerable<Vector2Int> GetCurrentProps()
        {
            Vector3 playerPosition = Vector3.zero;

            if (Session.hasInstance)
            {
                FirstPersonController player = Session.Instance.Player;
                if (player != null) playerPosition = player.transform.position;
            }

            float sI = 1f / settings.chunkSize;
            Vector2 playerCell =
                new Vector2(playerPosition.x * sI, -playerPosition.z * sI);
            float visibleDistance = settings.visibleDistance * sI;

            Vector2Int playerCellInt = new Vector2Int(Mathf.FloorToInt(playerCell.x), Mathf.FloorToInt(playerCell.y));

            for (int x = playerCellInt.x - 3; x <= playerCellInt.x + 3; x++)
            {
                for (int y = playerCellInt.y - 3; y <= playerCellInt.y + 3; y++)
                {
                    if (Mathf.Abs(playerCell.x - x) < visibleDistance && Mathf.Abs(playerCell.y - y) < visibleDistance)
                        yield return new Vector2Int(x, y);
                }
            }
        }


        private Terrain CreateTerrain(Vector2Int prop)
        {
            GameObject obj = new GameObject($"Terrain ({prop.x}, {prop.y})");

            Vector3 selfPos = transform.position;
            obj.transform.position = new Vector3(selfPos.x + prop.x * settings.chunkSize, selfPos.y, selfPos.z + prop.y * settings.chunkSize);

            Terrain ter = obj.AddComponent<Terrain>();
            TerrainData data = new TerrainData();
            data.name = obj.name;
            ter.terrainData = data;

            ter.drawInstanced = true;
            data.heightmapResolution = settings.heightmapResolution;
            data.size = new Vector3(settings.chunkSize, settings.height, settings.chunkSize);
            ter.materialTemplate = settings.material;

            TerrainCollider collider = obj.AddComponent<TerrainCollider>();
            collider.terrainData = ter.terrainData;

            ter.allowAutoConnect = true;

            if (chunks.ContainsKey(prop)) chunks[prop] = ter;
            else chunks.Add(prop, ter);

            terrainsDates.Add(ter.terrainData);

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
            IEnumerable<Vector2Int> affectChunks = deformer.GetAffectChunks(settings.chunkSize);

            Vector2Int[] chunksArr = affectChunks as Vector2Int[] ?? affectChunks.ToArray();
            
            foreach (Vector2Int chunk in chunksArr)
            {
                foreach (DeformationChannel channel in channels[chunk])
                {
                    channel.RegisterDeformer(deformer);
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
                    if(deformationChannel.IsDirty) deformationChannel.ApplyDirtyToCache();
                }
            }
            
            foreach (List<DeformationChannel> deformationChannels in channels.Values)
            {
                foreach (DeformationChannel deformationChannel in deformationChannels)
                {
                    if(deformationChannel.IsDirty) deformationChannel.Apply();
                }
            }

            deformersQueue.Clear();
        }
    }
}