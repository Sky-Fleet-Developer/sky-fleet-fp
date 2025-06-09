using System.Collections;
using System.Collections.Generic;
using Core.ContentSerializer;
using Core.ContentSerializer.Bundles;
using Core.Explorer.Content;
using Core.Structure;
using Core.Structure.Rigging;
using Core.UiStructure;
using Core.UIStructure.Utilities;
using Core.Utilities;
using Runtime.UI;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;
using AssetBundle = Core.ContentSerializer.Bundles.AssetBundle;

namespace Runtime.Explorer.ModContent
{
    public class ModInfoViewer : MonoBehaviour
    {
        [SerializeField] private Text nameMod;

        [SerializeField] private Transform contentInfo;

        [SerializeField] private StringItemPointer stringItemPrefab;
        [SerializeField] private ButtonItemPointer buttonItemPrefab;

        private LinkedList<StringItemPointer> stringPointers = new LinkedList<StringItemPointer>();
        private LinkedList<ButtonItemPointer> buttonPointers = new LinkedList<ButtonItemPointer>();

        private Mod mod;
        
        public void ApplyInfo(Mod mod)
        {
            this.mod = mod;
            nameMod.text = mod.name;
            ClearListProperty();

            CreateItemProperty("Classes: ", StringItemPointer.PropertyType.Header);
            foreach (System.Type classT in mod.GetClasses())
            {
                string className = classT.Name;
                if (classT.InheritsFrom(typeof(IBlock))) className = $"(Block)\n{className}";
                CreateItemProperty(className, StringItemPointer.PropertyType.Item);
            }
            CreateItemProperty("Assets: ", StringItemPointer.PropertyType.Header);

            LinkedList<PrefabBundle> prefabs = new LinkedList<PrefabBundle>();
            LinkedList<AssetBundle> assets = new LinkedList<AssetBundle>();
            
            foreach (Bundle bundle in mod.module.Cache)
            {
                switch (bundle)
                {
                    case PrefabBundle prefab:
                        prefabs.AddLast(prefab);
                        break;
                    
                    case AssetBundle asset:
                        assets.AddLast(asset);
                        break;
                }
            }
            
            foreach (AssetBundle asset in assets)
            {
                string[] typePath = asset.type.Split(new[] {'.'});
                string assetName = $"({typePath[typePath.Length - 1]})\n{asset.name}";
                CreateItemProperty(assetName, StringItemPointer.PropertyType.Item);
            }
            CreateItemProperty("Prefabs: ", StringItemPointer.PropertyType.Header);
            foreach (PrefabBundle prefab in prefabs)
            {
                string prefabName = prefab.name;
                if (prefab.tags.Contains("Block")) prefabName = $"(Block)\n{prefabName}";
                //CreateItemProperty(prefabName, StringItemPointer.PropertyType.Item);
                ButtonItemPointer pointer = DynamicPool.Instance.Get(buttonItemPrefab, contentInfo);
                pointer.SetVisual(prefabName, (System.Action)(() => ShowPrefab(prefab)), TextAnchor.MiddleRight, 20, FontStyle.Bold);
                buttonPointers.AddLast(pointer);
            }
            
        }

        private GameObject previewInstance;
        private async void ShowPrefab(PrefabBundle bundle)
        {
            if(previewInstance) Destroy(previewInstance);
            Object obj = await mod.module.GetAsset(bundle, mod.AllAssemblies);
            GameObject prefab = (GameObject) obj;
            previewInstance = Instantiate(prefab);
            previewInstance.gameObject.SetActive(true);
        }

        private void CreateItemProperty(string name, StringItemPointer.PropertyType type)
        {
            StringItemPointer pointer = DynamicPool.Instance.Get(stringItemPrefab, contentInfo);
            pointer.GetPointer<Text>("Text").text = name;
            pointer.SetVisual(type);
            stringPointers.AddLast(pointer);
        }

        private void ClearListProperty()
        {
            foreach (StringItemPointer itemPointer in stringPointers)
            {
                DynamicPool.Instance.Return(itemPointer);
            }
            stringPointers.Clear();
            foreach (ButtonItemPointer itemPointer in buttonPointers)
            {
                DynamicPool.Instance.Return(itemPointer);
            }
            buttonPointers.Clear();
        }


    }
}