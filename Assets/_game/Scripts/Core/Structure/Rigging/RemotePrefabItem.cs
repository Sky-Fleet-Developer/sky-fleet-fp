using System.Collections.Generic;
using System.Threading.Tasks;
using Core.ContentSerializer.Bundles;
using Core.Explorer.Content;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Structure.Rigging
{
    [System.Serializable]
    public class RemotePrefabItem
    {
        [SerializeField] private AssetReference reference;
        private PrefabBundle bundleReference = null;
        private Mod mod;
        public string guid;
        public List<string> tags;
        [System.NonSerialized] public GameObject loaded;

#if UNITY_EDITOR
        public RemotePrefabItem(AssetReference reference)
        {
            this.reference = reference;
            var prefabGameObject = (reference.editorAsset as GameObject);
            if (prefabGameObject)
            {
                var block = prefabGameObject.GetComponent<IBlock>();
                if (block != null)
                {
                    guid = block.Guid;
                }
            }
        }
#endif
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