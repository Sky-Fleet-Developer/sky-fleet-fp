using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.TerrainGenerator.Settings;
using Core.TerrainGenerator.Utility;
using Core.Utilities;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// Saves info about deformation channels and chunk values
    /// </summary>
    [System.Serializable, CreateAssetMenu]
    public class TerrainGenerationSettings : CompoundScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private string targetDirectory;
        [Space, SerializeField] private Material material;
        [Space, SerializeField] private int chunkSize = 1000;
        [SerializeField] private int height = 600;
        [Space, SerializeField] private int heightmapResolution = 257;
        [SerializeField] private int maxLoadedChunksByOneSide = 7;
        [SerializeField] private int alphamapResolution = 257;
        [Space(20), SerializeField] private float visibleDistance = 1000;
        [SerializeField] private float chunksRefreshDistance = 300;
        [SerializeField] private Vector2Int chunksCenter;
        [SerializeField] private CollisionGenerationSettings collisionSettings;
        private List<ChannelSettings> _settings;


        public DirectoryInfo directory;
        public IReadOnlyList<ChannelSettings> Settings => _settings;
        public float ChunkSize => chunkSize;
        public float VisibleDistance => visibleDistance;
        public float ChunksRefreshDistance => chunksRefreshDistance;
        public int MaxLoadedChunksByOneSide => maxLoadedChunksByOneSide;
        public int HeightmapResolution => heightmapResolution;
        public int AlphamapResolution => alphamapResolution;
        public Vector2Int ChunksCenter => chunksCenter;
        public int Height => height;
        public Material Material => material;
        public CollisionGenerationSettings CollisionSettings => collisionSettings;
        
        private void OnValidate()
        {
            Setup();
        }

        private void Setup()
        {
            directory = DirectoryUtilities.GetDirectory(targetDirectory);
            if (directory == null) Debug.LogWarning("Wrong directory!");
            _settings ??= new List<ChannelSettings>();
            _settings.Clear();
            _settings.AddRange(children.OfType<ChannelSettings>());
        }

#if UNITY_EDITOR
        [Button]
        private void MakeHeightmapLayer()
        {
            MakeNewLayer<MeshHeightmapChannelSettings>("Heightmap");
        }

        [Button]
        private void MakeColorLayer()
        {
            MakeNewLayer<ColorChannelSettings>("Color map");
        }

        [Button]
        private void MakeTreesLayer()
        {
            MakeNewLayer<TreesChannelSettings>("Trees map");
        }

        private void MakeNewLayer<T>(string n) where T : ChannelSettings
        {
            if (Settings.FirstOrDefault(x => x.GetType() == typeof(T))) return;

            T newSettings = CreateInstance<T>();
            newSettings.name = n;
            newSettings.Initialize(this);
            children.Add(newSettings);

            AssetDatabase.AddObjectToAsset(newSettings, this);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(newSettings);
        }
#endif


        [Button]
        private void CorrectDirectory()
        {
            string path = PathStorage.GetPathToLandscapesDirectory();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            Setup();
        }
    }
}