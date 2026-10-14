using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.TerrainGenerator.Filters;
using Core.TerrainGenerator.LOD;
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
        [Space, SerializeField] private int chunkMeshSize = 3600;
        [SerializeField] private int chunkMeshResolution = 257;
        [SerializeField] private int height = 600;
        [Space, SerializeField] private int heightmapResolution = 257;
        [SerializeField] private int maxLoadedChunksByOneSide = 7;
        [SerializeField] private int alphamapResolution = 257;
        [Space(20), SerializeField] private float visibleDistance = 1000;
        [SerializeField] private float chunksRefreshDistance = 300;
        [SerializeField] private Vector2Int chunksCenter;
        [SerializeField] private ComputeShader gpuWorksShader;
        [SerializeField] private ComputeShader biomeDebugShader;
        [SerializeField] private ComputeShader biomeComposeShader;
        [Tooltip("Source tiles of all biomes kept in GPU memory at once")]
        [SerializeField, Min(1)] private int biomeTilePoolSize = 64;
        
        public HeightmapSettings heightmapSettings;
        public AlphamapSettings alphamapSettings;
        public SurfaceSettings surfaceSettings = new ();
        public BiomeEdgeNoise biomeEdgeNoise = new ();
        public QuadTreeLodConfig lodConfig;
        
        private MapGpuWorker _gpuWorker;


        public DirectoryInfo Directory;
        [FormerlySerializedAs("HeightmapOffset")] public Vector2 heightmapOffset;
        public float ChunkMeshSize => chunkMeshSize;
        public float VisibleDistance => visibleDistance;
        public float ChunksRefreshDistance => chunksRefreshDistance;
        public int MaxLoadedChunksByOneSide => maxLoadedChunksByOneSide;
        public int HeightmapResolution => heightmapResolution;
        public int AlphamapResolution => alphamapResolution;
        public Vector2Int ChunksCenter => chunksCenter;
        /// <summary>Meters represented by normalized heightmap value 1.0.</summary>
        public int Height => height;
        /// <summary>Global heightmap filters among the child assets, in list order.</summary>
        public IEnumerable<IHeightmapFilterSource> HeightmapFilters => children.OfType<IHeightmapFilterSource>();
        public int ChunkMeshResolution => chunkMeshResolution;
        public Material Material => material;
        public ComputeShader BiomeDebugShader => biomeDebugShader;
        public ComputeShader BiomeComposeShader => biomeComposeShader;
        public int BiomeTilePoolSize => biomeTilePoolSize;
        /// <summary>Folder name of the terrain files inside the landscapes directory.</summary>
        public string TargetDirectory => targetDirectory;
        public MapGpuWorker GpuWorker
        {
            get
            {
                _gpuWorker ??= new MapGpuWorker(gpuWorksShader);
                return _gpuWorker;
            }
        }
        
        /// <summary>Raised when the settings are edited in the inspector.</summary>
        public event Action Changed;

        private void OnValidate()
        {
            Setup();
            Changed?.Invoke();
        }

        private void Setup()
        {
            Directory = DirectoryUtilities.GetDirectory(targetDirectory);
            if (Directory == null) Debug.LogWarning("Wrong directory!");
        }

#if UNITY_EDITOR
        /*[Button]
        private void MakeHeightmapLayer()
        {
            MakeNewLayer<HeightmapSettings>("Heightmap")?.Initialize(this);
        }

        [Button]
        private void MakeColorLayer()
        {
            MakeNewLayer<AlphamapSettings>("Color map")?.Initialize(this);
        }

        [Button]
        private void MakeTreesLayer()
        {
            MakeNewLayer<TreesChannelSettings>("Trees map")?.Initialize(this);
        }*/
        
        [Button]
        private void AddHeightmapFilter([ValueDropdown(nameof(GetHeightmapFilterTypes))] Type filterType)
        {
            if (filterType == null) return;
            var asset = MakeNewLayer<HeightmapFilterAsset>(filterType.Name);
            asset.Filter = (IHeightmapFilter)Activator.CreateInstance(filterType);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static IEnumerable<ValueDropdownItem<Type>> GetHeightmapFilterTypes()
        {
            return TypeCache.GetTypesDerivedFrom<IHeightmapFilter>()
                .Where(t => !t.IsAbstract && !typeof(UnityEngine.Object).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => new ValueDropdownItem<Type>(t.Name, t));
        }

        private T MakeNewLayer<T>(string n) where T : ScriptableObject
        {
            return (T)MakeNewLayer(typeof(T), n);
        }

        private ScriptableObject MakeNewLayer(Type type, string n)
        {
            ScriptableObject instance = CreateInstance(type);
            instance.name = n;
            children.Add(instance);

            AssetDatabase.AddObjectToAsset(instance, this);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(instance);
            return instance;
        }
#endif


        [Button]
        private void CorrectDirectory()
        {
            string path = PathStorage.GetPathToLandscapesDirectory();
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
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