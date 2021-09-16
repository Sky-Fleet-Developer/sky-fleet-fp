using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Structure.Rigging
{
    [CreateAssetMenu(menuName = "Tables/Rigging table")]
    public class TableRigging : SingletonAsset<TableRigging>
    {
        public List<RiggingItem> items;
        private Dictionary<string, RiggingItem> itemsHash;
        
        public RiggingItem GetItem(string guid)
        {
            itemsHash ??= items.ToDictionary(item => item.guid);

            return itemsHash[guid];
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
        public IBlock blockHash;
        [ShowInInspector] public string refresher
        {
            get
            {
                if (reference != null && reference.editorAsset != null)
                {
                    if (blockHash == null || reference.editorAsset as GameObject != blockHash.transform.gameObject)
                    {
                        if((reference.editorAsset as GameObject).TryGetComponent(out IBlock block))
                        {
                            blockHash = block;
                        }
                        else
                        {
                            blockHash = null;
                        }
                    }
                    else
                    {
                        blockHash = null;
                    }
                }

                if (blockHash != null)
                {
                    guid = blockHash.Guid;
                    mounting = blockHash.MountingType;
                }

                string val = "--";
                val = val.Insert(Time.frameCount % 3, "*");
                return val;
            }
        }
#endif

    }
}
