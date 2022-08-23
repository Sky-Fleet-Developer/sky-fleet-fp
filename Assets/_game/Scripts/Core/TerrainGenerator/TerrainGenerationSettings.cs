using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.TerrainGenerator.Settings;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// Saves info about deformation channels and chunk values
    /// </summary>
    [System.Serializable, CreateAssetMenu]
    public class TerrainGenerationSettings : ScriptableObject
    {
        [SerializeField] private string targetDirectory;
        [Space, SerializeField] private Material material;
        [Space, SerializeField] private int chunkSize = 1000;
        [SerializeField] private int height = 600;
        [Space, SerializeField] private int heightmapResolution = 257;
        [SerializeField] private int alphamapResolution = 257;
        [Space(20), SerializeField] private float visibleDistance = 1000;
        [SerializeField] private float chunksRefreshDistance = 300;
        [SerializeField] private List<ChannelSettings> settings;


        public DirectoryInfo directory;
        public List<ChannelSettings> Settings => settings;
        public int ChunkSize => chunkSize;
        public float VisibleDistance => visibleDistance;
        public float ChunksRefreshDistance => chunksRefreshDistance;
        public int HeightmapResolution => heightmapResolution;
        public int AlphamapResolution => alphamapResolution;
        public int Height => height;
        public Material Material => material;

        private void OnEnable()
        {
            directory = GetDirectory(targetDirectory);
            if (directory == null) Debug.LogWarning("Wrong directory!");
        }

        private void OnValidate()
        {
            directory = GetDirectory(targetDirectory);
        }

#if UNITY_EDITOR
        [Button]
        private void MakeHeightmapLayer()
        {
            MakeNewLayer<HeightmapChannelSettings>("Heightmap");
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
            Settings.Add(newSettings);

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

        private DirectoryInfo GetDirectory(string directoryName)
        {
            string[] directories = Directory.GetDirectories(PathStorage.GetPathToLandscapesDirectory());
            foreach (string t in directories)
            {
                DirectoryInfo info = new DirectoryInfo(t);
                if (info.Name == directoryName)
                {
                    return info;
                }
            }

            return null;
        }
    }
}