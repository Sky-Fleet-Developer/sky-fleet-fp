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
        [JsonIgnore, NonSerialized] public List<GameObject> PrefabsToSerialize = new List<GameObject>();

        [JsonIgnore, NonSerialized] public List<Object> AssetsToSerialize = new List<Object>();

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
        public void SerializeAll(string pathToSave)
        {
            string path = pathToSave.Replace("DATA_PATH", Application.dataPath) + "/";
            
            AssetsToSerialize = new List<Object>();

            isCurrentlyBuilded = true;

            Cache = new List<Bundle>();
            prefabsCache = new List<PrefabBundle>();
            assetsCache = new List<AssetBundle>();
            
            foreach (PrefabBundle serializePrefab in SerializePrefabs(path))
            {
                Cache.Add(serializePrefab);
                prefabsCache.Add(serializePrefab);
            }
            foreach (AssetBundle serializeAsset in SerializeAssets(path))
            {
                Cache.Add(serializeAsset);
                assetsCache.Add(serializeAsset);
            }
            
            WriteClass(path);
        }

        public List<PrefabBundle> SerializePrefabs(string pathToSave)
        {
            Serializer serializer = ModProvider.GetSerializer();
            serializer.ModFolderPath = pathToSave;
            serializer.DetectedObjectReport = v =>
            {
                int id = v.GetInstanceID();
                if (AssetsToSerialize.FirstOrDefault(x => x.GetInstanceID() == id) != null) return;
                AssetsToSerialize.Add(v);
            };

            return serializer.GetBundlesFor(PrefabsToSerialize);
        }

        public List<AssetBundle> SerializeAssets(string pathToSave)
        {
            Serializer serializer = ModProvider.GetSerializer();
            serializer.ModFolderPath = pathToSave;
            List<Object> collector = AssetsToSerialize.Clone();
            List<AssetBundle> result = new List<AssetBundle>();

            serializer.DetectedObjectReport = v =>
            {
                int id = v.GetInstanceID();
                if (AssetsToSerialize.FirstOrDefault(x => x.GetInstanceID() == id) != null) return;
                AssetsToSerialize.Add(v);
                // ReSharper disable once AccessToModifiedClosure
                collector.Add(v);
            };

            while (collector.Count > 0)
            {
                List<Object> temp = collector.Clone();
                collector = new List<Object>();
                result.AddRange(serializer.GetBundlesFor(temp));
            }

            return result;
        }

        public async Task<Object> GetAsset(Bundle bundle, System.Reflection.Assembly[] availableAssemblies)
        {
            if (deserializedAssets.TryGetValue(bundle.id, out Object value)) return value;
            if (deserializationTasks.TryGetValue(bundle.id, out Task<Object> currentTask)) return await currentTask;

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
            Deserializer deserializer = ModProvider.GetDeserializer(ModFolderPath, availableAssemblies);
            deserializer.IsCurrentlyBuilded = isCurrentlyBuilded;
            
            Type type = deserializer.GetTypeByName(bundle.type);
            Object instance;
            if (CacheService.AssetCreators.TryGetValue(type, out IAssetCreator creator))
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
            Deserializer deserializer = ModProvider.GetDeserializer(ModFolderPath, availableAssemblies);
            deserializer.IsCurrentlyBuilded = isCurrentlyBuilded;
            
            deserializer.GetObject = v => GetObject(v, availableAssemblies);

            Transform instance = await bundle.ConstructTree(null, deserializer);

            return instance.gameObject;
        }
        
        private Task<Object> GetObject(int id, System.Reflection.Assembly[] availableAssemblies)
        {
            return GetAsset(Cache.FirstOrDefault(x => x.id == id), availableAssemblies);
        }

        /*[Button]
        private void ClearDirectoryClass(string pathToSave)
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
        }*/

        private void WriteClass(string path)
        {
            string jsonS = JsonConvert.SerializeObject(this);
            File.WriteAllText(path + PathStorage.BASE_MOD_FILE_DEFINE, jsonS);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
