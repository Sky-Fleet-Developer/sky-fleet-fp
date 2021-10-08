using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Core.ContentSerializer.HierarchySerializer;
using Core.Explorer.Content;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Structure.Rigging
{
    [CreateAssetMenu(menuName = "Tables/Rigging table")]
    public class TableRigging : SingletonAsset<TableRigging>
    {
        public List<RiggingItem> items;
        private Dictionary<string, RiggingItem> itemsCache;
        
        public RiggingItem GetItem(string guid)
        {
            itemsCache ??= items.ToDictionary(item => item.guid);

            return itemsCache[guid];
        }

        public void GetBlocksFromMod(Mod mod)
        {
            foreach (var prefab in mod.module.Cache)
            {
                if (prefab.tags.Contains("Block"))
                {
                    int idx = prefab.tags.IndexOf("Block");
                    var newItem = new RiggingItem(idx, (PrefabBundle)prefab, mod);
                    itemsCache.Add(newItem.guid, newItem);
                }
            }
        }
    }

    [System.Serializable]
    public class RiggingItem
    {
        [SerializeField] private AssetReference reference;
        private PrefabBundle bundleReference = null;
        private Mod mod;
        public string guid;
        public string mounting;
        [System.NonSerialized] public GameObject loaded;
        
        public RiggingItem(int tagIdx, PrefabBundle prefab, Mod mod)
        {
            this.mod = mod;
            bundleReference = prefab;
            guid = prefab.tags[tagIdx + 1];
            mounting = prefab.tags[tagIdx + 2];
        }

        public async Task<GameObject> GetBlock()
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
        public IBlock blockCache;

        [ShowInInspector] public string refresher
        {
            get
            {
                if (reference != null && reference.editorAsset != null)
                {
                    if (blockCache == null || reference.editorAsset as GameObject != blockCache.transform.gameObject)
                    {
                        if((reference.editorAsset as GameObject).TryGetComponent(out IBlock block))
                        {
                            blockCache = block;
                        }
                        else
                        {
                            blockCache = null;
                        }
                    }
                    else
                    {
                        blockCache = null;
                    }
                }

                if (blockCache != null)
                {
                    guid = blockCache.Guid;
                    mounting = blockCache.MountingType;
                }

                string val = "--";
                val = val.Insert(Time.frameCount % 3, "*");
                return val;
            }
        }
#endif

    }
}
