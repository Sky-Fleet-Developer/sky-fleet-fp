using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            /*itemsCache ??= items.ToDictionary(item => item.guid);

            return itemsCache[guid];*/

            return items.FirstOrDefault(x => x.guid == guid);
        }

        void ParceModItem()
        {

        }
    }

    [System.Serializable]
    public class RiggingItem
    {
        public AssetReference reference;
        public string guid;
        public string mounting;
        [System.NonSerialized] public GameObject loaded;

        public async Task<GameObject> GetBlock()
        {
            if (!loaded) loaded = await AssetManager.Instance.LoadAssetTask<GameObject>(reference, "Block");
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
