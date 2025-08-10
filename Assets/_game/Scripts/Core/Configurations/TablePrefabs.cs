using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.ContentSerializer.Bundles;
using Core.Data;
using Core.Explorer.Content;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Configurations
{
    [CreateAssetMenu(menuName = "Configs/Prefabs table")]
    public class TablePrefabs : SingletonAsset<TablePrefabs>
    {
        [SerializeField] private PrefabProcessor[] preprocessors;
        public List<RemotePrefabItem> items;
        private Dictionary<string, RemotePrefabItem> itemsCache;
        [Button]
        public void MakePrefabsPreprocess()
        {
            foreach (var prefabProcessDataStore in preprocessors)
            {
                prefabProcessDataStore.ProcessPrefabs(items);
            }
        }
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
            var duplicates = items
                .GroupBy(item => item.guid)
                .Where(group => group.Count() > 1);
                //.Select(group => group.Key);

#if UNITY_EDITOR
            foreach (var duplication in duplicates)
            {
                Debug.LogError($"Duplications: {duplication.Key}");
                foreach (var remotePrefabItem in duplication)
                {
                    Debug.LogError($"Duplicated: {remotePrefabItem.GetReferenceInEditor().name}");
                }
            }
#endif

            return items.ToDictionary(item => item.guid);
        }
        
        public RemotePrefabItem GetItem(string guid)
        {
            itemsCache ??= ConvertItems();
            if (!itemsCache.TryGetValue(guid, out var value))
            {
                Debug.LogException(new KeyNotFoundException($"Cant find guid {guid}"));
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
            [SerializeField, FolderPath(AbsolutePath = false)] private string pathToSearchFolder;
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
}
