using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.ContentSerializer.Bundles;
using Core.Explorer.Content;
using Core.Utilities;
using Runtime;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Structure.Rigging
{
    [CreateAssetMenu(menuName = "Tables/Prefabs table")]
    public class TablePrefabs : SingletonAsset<TablePrefabs>
    {
        public List<RemotePrefabItem> items;
        private Dictionary<string, RemotePrefabItem> itemsCache;
#if UNITY_EDITOR
        [Space(20), SerializeField] private AddPrefabSettings[] autoSearchSettings;

        [Button]
        private void SearchPrefabsInFolders()
        {
            foreach (AddPrefabSettings autoSearchSetting in autoSearchSettings)
            {
                foreach ((string fullName, List<string> tags) in autoSearchSetting.GetPaths())
                {
                    string correctedPath = "Assets" + fullName.Replace(Application.dataPath.Replace('/', '\\'), "");
                    var target = AssetDatabase.LoadAssetAtPath<GameObject>(correctedPath);
                    if(target == null) continue;
                    var exist = items.FirstOrDefault(x => x.GetReferenceInEditor() == target);
                    if (exist == null)
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target.GetInstanceID(), out string guid,
                            out long localId);
                        RemotePrefabItem newItem = new RemotePrefabItem(new AssetReference(guid));
                        newItem.tags = autoSearchSetting.tags.Clone();
                        newItem.tags.AddRange(tags);
                        items.Add(newItem);
                    }
                    else
                    {
                        exist.tags = autoSearchSetting.tags.Clone();
                        exist.tags.AddRange(tags);
                    }
                }
            }
        }
        #endif

        private Dictionary<string, RemotePrefabItem> ConvertItems()
        {
            var duplicateKeys = items
                .GroupBy(item => item.guid)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            foreach (string key in duplicateKeys)
            {
                Debug.LogError($"Duplicate key: {key}");
            }
            return items.ToDictionary(item => item.guid);
        }
        
        public RemotePrefabItem GetItem(string guid)
        {
            itemsCache ??= ConvertItems();
            if (!itemsCache.TryGetValue(guid, out var value))
            {
                throw new KeyNotFoundException($"Cant find guid {guid}");
            }
            return value;
        }

        public void ExtractBlocksFromMod(Mod mod)
        {
            List<string> tags = GameData.PrivateData.remotePrefabsTags;
            foreach (Bundle prefab in mod.module.Cache)
            {
                string remotePrefabTag = string.Empty;
                foreach (string tag in tags)
                {
                    if (prefab.tags.Contains(tag)) remotePrefabTag = tag;
                }

                if (remotePrefabTag == string.Empty) continue;

                int idx = prefab.tags.IndexOf(remotePrefabTag);
                RemotePrefabItem newItem = new RemotePrefabItem(idx, (PrefabBundle) prefab, mod);
                itemsCache.Add(newItem.guid, newItem);
            }
        }
        [System.Serializable]
        private class AddPrefabSettings
        {
            [SerializeField, FolderPath(AbsolutePath = true)] private string pathToSearchFolder;
            [SerializeField] public List<string> tags;

            public IEnumerable<(string fullName, List<string> tags)> GetPaths()
            {
                DirectoryInfo origin = new DirectoryInfo(pathToSearchFolder);
                List<string> path = new List<string>();
                foreach (FileInfo fileInfo in origin.GetFiles("*.prefab"))
                {
                    yield return (fileInfo.FullName, path);
                }

                foreach (DirectoryInfo nestedDirectory in GetNestedDirectories(origin, path))
                {
                    foreach (FileInfo fileInfo in nestedDirectory.GetFiles("*.prefab"))
                    {
                        yield return (fileInfo.FullName, path);
                    }
                }
            }

            private IEnumerable<DirectoryInfo> GetNestedDirectories(DirectoryInfo directoryInfo, List<string> path)
            {
                foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
                {
                    string lowerCaseName = directory.Name.ToLower();
                    path.Add(lowerCaseName);
                    yield return directory;
                    foreach (DirectoryInfo nestedDirectory in GetNestedDirectories(directory, path))
                    {
                        yield return nestedDirectory;
                    }
                    path.Remove(lowerCaseName);

                }
            }
        }
    }

    [System.Serializable]
    public class RemotePrefabItem
    {
        [SerializeField] private AssetReference reference;
        private PrefabBundle bundleReference = null;
        private Mod mod;
        public string guid;
        public List<string> tags;
        [System.NonSerialized] public GameObject loaded;

        public RemotePrefabItem(AssetReference reference)
        {
            this.reference = reference;
        }
        public RemotePrefabItem(int tagIdx, PrefabBundle prefab, Mod mod)
        {
            this.mod = mod;
            bundleReference = prefab;
            guid = prefab.tags[tagIdx + 1];
            tags = prefab.tags.Clone();
        }

        public async Task<GameObject> LoadPrefab()
        {
            if (reference != null)
            {
#if UNITY_EDITOR
                return reference.editorAsset as GameObject;
#else
                if (!loaded) loaded = await AssetManager.Instance.LoadAssetTask<GameObject>(reference, "Block");
#endif
            }
            else if (bundleReference != null)
            {
                loaded = (GameObject)await mod.module.GetAsset(bundleReference, mod.AllAssemblies);
            }

            return loaded;
        }

#if  UNITY_EDITOR
        public ITablePrefab tablePrefabCache;

        [ShowInInspector] public string refresher
        {
            get
            {
                if (reference != null && reference.editorAsset != null)
                {
                    if (tablePrefabCache == null || reference.editorAsset as GameObject != tablePrefabCache.transform.gameObject)
                    {
                        if((reference.editorAsset as GameObject).TryGetComponent(out ITablePrefab tablePrefab))
                        {
                            tablePrefabCache = tablePrefab;
                        }
                        else
                        {
                            tablePrefabCache = null;
                        }
                    }
                    else
                    {
                        tablePrefabCache = null;
                    }
                }

                if (tablePrefabCache != null)
                {
                    guid = tablePrefabCache.Guid;
                    tags ??= new List<string>();
                    tags.AddRange(tablePrefabCache.Tags);
                }

                string val = "--";
                val = val.Insert(Time.frameCount % 3, "*");
                return val;
            }
        }

        public GameObject GetReferenceInEditor()
        {
            return reference.editorAsset as GameObject;
        }
#endif

    }
}
