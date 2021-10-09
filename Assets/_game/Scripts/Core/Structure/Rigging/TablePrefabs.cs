using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Core.ContentSerializer.Bundles;
using Core.Explorer.Content;
using Core.Utilities;
using Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Structure.Rigging
{
    [CreateAssetMenu(menuName = "Tables/Prefabs table")]
    public class TablePrefabs : SingletonAsset<TablePrefabs>
    {
        public List<RemotePrefabItem> items;
        private Dictionary<string, RemotePrefabItem> itemsCache;
        
        public RemotePrefabItem GetItem(string guid)
        {
            itemsCache ??= items.ToDictionary(item => item.guid);

            return itemsCache[guid];
        }

        public void ExtractBlocksFromMod(Mod mod)
        {
            var tags = GameData.PrivateData.remotePrefabsTags;
            foreach (var prefab in mod.module.Cache)
            {
                string remotePrefabTag = string.Empty;
                foreach (string tag in tags)
                {
                    if (prefab.tags.Contains(tag)) remotePrefabTag = tag;
                }

                if (remotePrefabTag == string.Empty) continue;

                int idx = prefab.tags.IndexOf(remotePrefabTag);
                var newItem = new RemotePrefabItem(idx, (PrefabBundle) prefab, mod);
                itemsCache.Add(newItem.guid, newItem);
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
                if (!loaded) loaded = await AssetManager.Instance.LoadAssetTask<GameObject>(reference, "Block");
            }
            else if (bundleReference != null)
            {
                loaded = (GameObject)await mod.module.GetAsset(bundleReference, mod.assemblies);
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
                    tags = tablePrefabCache.Tags;
                }

                string val = "--";
                val = val.Insert(Time.frameCount % 3, "*");
                return val;
            }
        }
#endif

    }
}
