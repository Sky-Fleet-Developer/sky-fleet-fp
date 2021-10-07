using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.ContentSerializer.HierarchySerializer;
using Core.Utilities;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;
using AssetBundle = Core.ContentSerializer.ResourceSerializer.AssetBundle;

namespace Core.ContentSerializer
{
    [System.Serializable]
    public class SerializationModule
    {
        [JsonIgnore]
        public GameObject[] PrefabsToSerialize;

        [JsonIgnore]
        public List<Object> AssetsToSerialize;

        public List<PrefabBundle> prefabsCache;
        public List<AssetBundle> assetsCache;

        [JsonIgnore]
        public List<Transform> prefabs;

        [JsonIgnore]
        [ShowInInspector] public Dictionary<int, Object> assets;
        
        public string ModFolderPath { get; set; }
        private bool isCurrentlyBuilded = false;
        
        [Button]
        public void SerializeAll()
        {
            AssetsToSerialize = new List<Object>();

            isCurrentlyBuilded = true;
            
            prefabsCache = SerializePrefabs();
            assetsCache = SerializeAssets();

            WriteClass();
        }

        public List<PrefabBundle> SerializePrefabs()
        {
            var serializer = PrefabProvider.GetSerializer();
            serializer.DetectedObjectReport = v =>
            {
                var id = v.GetInstanceID();
                if (AssetsToSerialize.FirstOrDefault(x => x.GetInstanceID() == id) != null) return;
                AssetsToSerialize.Add(v);
            };

            return serializer.GetBundlesFor(PrefabsToSerialize);
        }

        public List<AssetBundle> SerializeAssets()
        {
            var serializer = PrefabProvider.GetSerializer();
            var collector = AssetsToSerialize.Clone();
            var result = new List<AssetBundle>();

            serializer.DetectedObjectReport = v =>
            {
                var id = v.GetInstanceID();
                if (AssetsToSerialize.FirstOrDefault(x => x.GetInstanceID() == id) != null) return;
                AssetsToSerialize.Add(v);
                // ReSharper disable once AccessToModifiedClosure
                collector.Add(v);
            };

            while (collector.Count > 0)
            {
                var array = collector.ToArray();
                collector = new List<Object>();
                result.AddRange(serializer.GetBundlesFor(array));
            }

            return result;
        }

        [Button]
        public Task DeserializeAll()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return DeserializeAll(assemblies);
        }
        
        public async Task DeserializeAll(System.Reflection.Assembly[] availableAssemblies)
        {
            assets = await DeserializeAssets(availableAssemblies);
            prefabs = DeserializePrefabs(availableAssemblies);
            isCurrentlyBuilded = false;
        }

        public async Task<Dictionary<int, Object>> DeserializeAssets(System.Reflection.Assembly[] availableAssemblies)
        {
            var deserializer = PrefabProvider.GetDeserializer(ModFolderPath, availableAssemblies);
            deserializer.IsCurrentlyBuilded = isCurrentlyBuilded;
            Dictionary<int, Object> result = new Dictionary<int, Object>(assetsCache.Count);
            for (var i = 0; i < assetsCache.Count; i++)
            {
                var type = deserializer.GetTypeByName(assetsCache[i].type);
                if (CacheService.AssetCreators.TryGetValue(type, out var creator))
                {
                    var instance = await creator.CreateInstance(assetsCache[i].type, assetsCache[i].Cache, deserializer);
                    result.Add(assetsCache[i].id, instance);
                }
                else
                {
                    result.Add(assetsCache[i].id, (Object)System.Activator.CreateInstance(type));
                }
            }
            deserializer.GetObject = v => result[v];

            foreach (var cache in assetsCache)
            {
                object source = result[cache.id];
                CacheService.SetNestedCache(cache.type, ref source, cache.Cache, null, deserializer);
                result[cache.id] = (Object)source;
            }

            return result;
        }

        public List<Transform> DeserializePrefabs(System.Reflection.Assembly[] availableAssemblies)
        {
            var deserializer = PrefabProvider.GetDeserializer(ModFolderPath, availableAssemblies);
            deserializer.IsCurrentlyBuilded = isCurrentlyBuilded;
            List<Transform> result = new List<Transform>(assetsCache.Count);
            deserializer.GetObject = v => assets[v];

            foreach (var prefabBundle in prefabsCache)
            {
                var pr = prefabBundle.ConstructTree(null, deserializer);
                result.Add(pr);
            }

            return result;
        }

        [Button]
        private void ClearDirectoryClass()
        {
            string path = Application.dataPath + PathStorage.BASE_PATH;
            Debug.Log(path);
            if (Directory.Exists(path))
            {        
                Directory.Delete(path, true);
                Directory.CreateDirectory(path);
                Directory.CreateDirectory(Application.dataPath + PathStorage.BASE_PATH_MODELS);
                Directory.CreateDirectory(Application.dataPath + PathStorage.BASE_PATH_TEXTURES);
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif

            }
        }

        private void WriteClass()
        {
            string path = Application.dataPath + PathStorage.BASE_PATH + "/";
            string jsonS = JsonConvert.SerializeObject(this);
            File.WriteAllText(path + PathStorage.BASE_MOD_FILE_DEFINE, jsonS);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
