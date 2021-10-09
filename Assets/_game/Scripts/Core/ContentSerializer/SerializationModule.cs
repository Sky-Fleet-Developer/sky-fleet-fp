using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.ContentSerializer.Bundles;
using Core.ContentSerializer.Providers;
using Core.Utilities;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;
using AssetBundle = Core.ContentSerializer.Bundles.AssetBundle;

namespace Core.ContentSerializer
{
    [System.Serializable]
    public class SerializationModule
    {
        [JsonIgnore, ShowInInspector]
        private GameObject[] PrefabsToSerialize = new GameObject[0];

        [JsonIgnore, ShowInInspector]
        private List<Object> AssetsToSerialize =  new List<Object>();

        [JsonIgnore, HideInInspector]
        public List<Bundle> Cache
        {
            get
            {
                if (cache != null) return cache;
                cache = new List<Bundle>(prefabsCache.Count + assetsCache.Count);
                cache.AddRange(prefabsCache);
                cache.AddRange(assetsCache);
                return cache;
            }
            set => cache = value;
        }
        [JsonIgnore, NonSerialized]
        public List<Bundle> cache;
        
        [JsonRequired, ShowInInspector]
        private List<PrefabBundle> prefabsCache = new List<PrefabBundle>();
        [JsonRequired, ShowInInspector]
        private List<AssetBundle> assetsCache = new List<AssetBundle>();

        [JsonIgnore, ShowInInspector]
        private Dictionary<int, Object> deserializedAssets = new Dictionary<int, Object>();
        [JsonIgnore, ShowInInspector]
        private Dictionary<int, Task<Object>> deserializationTasks = new Dictionary<int, Task<Object>>();

        public string ModFolderPath { get; set; }
        private bool isCurrentlyBuilded = false;
        
        [Button]
        public void SerializeAll()
        {
            AssetsToSerialize = new List<Object>();

            isCurrentlyBuilded = true;

            Cache = new List<Bundle>();
            prefabsCache = new List<PrefabBundle>();
            assetsCache = new List<AssetBundle>();
            
            foreach (var serializePrefab in SerializePrefabs())
            {
                Cache.Add(serializePrefab);
                prefabsCache.Add(serializePrefab);
            }
            foreach (var serializeAsset in SerializeAssets())
            {
                Cache.Add(serializeAsset);
                assetsCache.Add(serializeAsset);
            }
            
            WriteClass();
        }

        public List<PrefabBundle> SerializePrefabs()
        {
            var serializer = ModProvider.GetSerializer();
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
            var serializer = ModProvider.GetSerializer();
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
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            return DeserializeAll(assemblies);
        }
        
        public async Task DeserializeAll(System.Reflection.Assembly[] availableAssemblies)
        {
            /*deserializedAssets = await DeserializeAssets(availableAssemblies);
            deserializedPrefabs = await DeserializePrefabs(availableAssemblies);
            isCurrentlyBuilded = false;*/
        }

        public async Task<Object> GetAsset(Bundle bundle, System.Reflection.Assembly[] availableAssemblies)
        {
            if (deserializedAssets.TryGetValue(bundle.id, out var value)) return value;
            if (deserializationTasks.TryGetValue(bundle.id, out var currentTask)) return await currentTask;

            Task<Object> task = null;
            if (bundle is AssetBundle asset)
            {
                task = DeserializeAsset(asset, availableAssemblies);
            }

            if (bundle is PrefabBundle prefab)
            {
                task = DeserializePrefab(prefab, availableAssemblies);
            }

            if (task == null) throw new System.Exception("Unexpected bundle type!");

            deserializationTasks.Add(bundle.id, task);
            await task;
            deserializedAssets.Add(bundle.id, task.Result);
            deserializationTasks.Remove(bundle.id);
            return task.Result;
        }
        
        private async Task<Object> DeserializeAsset(AssetBundle bundle, System.Reflection.Assembly[] availableAssemblies)
        {
            var deserializer = ModProvider.GetDeserializer(ModFolderPath, availableAssemblies);
            deserializer.IsCurrentlyBuilded = isCurrentlyBuilded;
            
            var type = deserializer.GetTypeByName(bundle.type);
            Object instance;
            if (CacheService.AssetCreators.TryGetValue(type, out var creator))
            {
                instance = await creator.CreateInstance(bundle.type, bundle.Cache, deserializer);
            }
            else
            {
                instance = (Object) System.Activator.CreateInstance(type);
            }
            
            deserializer.GetObject = v => GetObject(v, availableAssemblies);

            await deserializer.Behaviour.SetNestedCache(bundle.type, instance, bundle.Cache, null);

            return instance;
        }

        public async Task<Object> DeserializePrefab(PrefabBundle bundle, System.Reflection.Assembly[] availableAssemblies)
        {
            var deserializer = ModProvider.GetDeserializer(ModFolderPath, availableAssemblies);
            deserializer.IsCurrentlyBuilded = isCurrentlyBuilded;
            
            deserializer.GetObject = v => GetObject(v, availableAssemblies);

            var instance = await bundle.ConstructTree(null, deserializer);

            return instance.gameObject;
        }
        
        private Task<Object> GetObject(int id, System.Reflection.Assembly[] availableAssemblies)
        {
            return GetAsset(Cache.FirstOrDefault(x => x.id == id), availableAssemblies);
        }

       /* public async Task<Dictionary<int, Object>> DeserializeAssets(System.Reflection.Assembly[] availableAssemblies)
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

            deserializer.GetObject = GetObject;

            foreach (var cache in assetsCache)
            {
                object source = result[cache.id];
                CacheService.SetNestedCache(cache.type, source, cache.Cache, null, deserializer);
                result[cache.id] = (Object)source;
            }

            return result;

            async Task<Object> GetObject(int id)
            {
                return result[id];
            }
        }*/

        /*public async Task<Dictionary<int, Transform>> DeserializePrefabs(System.Reflection.Assembly[] availableAssemblies)
        {
            var deserializer = PrefabProvider.GetDeserializer(ModFolderPath, availableAssemblies);
            deserializer.IsCurrentlyBuilded = isCurrentlyBuilded;
            Dictionary<int, Transform> result = new Dictionary<int, Transform>(assetsCache.Count);
            deserializer.GetObject = GetObject;

            foreach (var prefabBundle in prefabsCache)
            {
                var pr = await prefabBundle.ConstructTree(null, deserializer);
                result.Add(prefabBundle.id, pr);
            }

            return result;
            async Task<Object> GetObject(int id)
            {
                return deserializedAssets[id];
            }
        }*/

        [Button]
        private void ClearDirectoryClass()
        {
            string path = Application.dataPath + PathStorage.BASE_MODS_PATH;
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
            string path = Application.dataPath + PathStorage.BASE_MODS_PATH + "/";
            string jsonS = JsonConvert.SerializeObject(this);
            File.WriteAllText(path + PathStorage.BASE_MOD_FILE_DEFINE, jsonS);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
