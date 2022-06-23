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
        public string targetDirectory;
        [Space] public Material material;
        [Space] public int chunkSize = 1000;
        public int height = 600;
        [Space] public int heightmapResolution = 257;
        [Space(20)] public float visibleDistance = 1000;

        [SerializeField] public List<ChannelSettings> settings;

        public DirectoryInfo directory;

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
            if (settings.FirstOrDefault(x => x.GetType() == typeof(T))) return;

            T newSettings = CreateInstance<T>();
            newSettings.name = n;
            newSettings.Initialize(this);
            settings.Add(newSettings);

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