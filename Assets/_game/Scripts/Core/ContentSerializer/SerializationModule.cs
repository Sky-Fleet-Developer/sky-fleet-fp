using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Utilities;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using Object = UnityEngine.Object;

namespace ContentSerializer
{
    [System.Serializable]
    public class SerializationModule
    {
        [JsonIgnore]
        public GameObject[] PrefabsToSerialize;

        [JsonIgnore]
        public List<Object> AssetsToSerialize;

        public List<PrefabBundle> prefabsHash;
        public List<AssetBundle> assetsHash;

        [JsonIgnore]
        public List<Transform> prefabs;

        [JsonIgnore]
        [ShowInInspector] public Dictionary<int, Object> assets;
        
        public string ModFolderPath { get; set; }

        [Button]
        public void SerializeAll()
        {
            AssetsToSerialize = new List<Object>();

            prefabsHash = SerializePrefabs();
            assetsHash = SerializeAssets();

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
        }

        public async Task<Dictionary<int, Object>> DeserializeAssets(System.Reflection.Assembly[] availableAssemblies)
        {
            var deserializer = PrefabProvider.GetDeserializer(ModFolderPath, availableAssemblies);
            Dictionary<int, Object> result = new Dictionary<int, Object>(assetsHash.Count);
            for (var i = 0; i < assetsHash.Count; i++)
            {
                var type = deserializer.GetTypeByName(assetsHash[i].name);
                if (HashService.AssetCreators.TryGetValue(type, out var creator))
                {
                    var instance = await creator.CreateInstance(assetsHash[i].name, assetsHash[i].Hash, deserializer);
                    result.Add(assetsHash[i].id, instance);
                }
                else
                {
                    result.Add(assetsHash[i].id, (Object)System.Activator.CreateInstance(type));
                }
            }
            deserializer.GetObject = v => result[v];

            foreach (var hash in assetsHash)
            {
                object source = result[hash.id];
                HashService.SetNestedHash(hash.name, ref source, hash.Hash, null, deserializer);
                result[hash.id] = (Object)source;
            }

            return result;
        }

        public List<Transform> DeserializePrefabs(System.Reflection.Assembly[] availableAssemblies)
        {
            var deserializer = PrefabProvider.GetDeserializer(ModFolderPath, availableAssemblies);
            List<Transform> result = new List<Transform>(assetsHash.Count);
            deserializer.GetObject = v => assets[v];

            foreach (var prefabBundle in prefabsHash)
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
