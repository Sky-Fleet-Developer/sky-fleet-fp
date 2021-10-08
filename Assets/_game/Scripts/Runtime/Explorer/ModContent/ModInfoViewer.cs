using System;
using System.Collections;
using System.Collections.Generic;
using Core.ContentSerializer;
using Core.ContentSerializer.HierarchySerializer;
using Core.Explorer.Content;
using Core.Structure.Rigging;
using Core.UiStructure;
using Core.Utilities;
using Core.Utilities.UI;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;
using AssetBundle = Core.ContentSerializer.ResourceSerializer.AssetBundle;

namespace Runtime.Explorer.ModContent
{
    public class ModInfoViewer : MonoBehaviour
    {
        [SerializeField] private Text nameMod;

        [SerializeField] private Transform contentInfo;

        [SerializeField] private StringItemPointer prefabCategory;

        private LinkedList<StringItemPointer> itemsMod = new LinkedList<StringItemPointer>();

        public void ApplyInfo(Mod mod)
        {
            nameMod.text = mod.name;
            ClearListProperty();

            CreateItemPropetry("Classes: ", StringItemPointer.PropertyType.Header);
            foreach (Type classT in mod.GetClasses())
            {
                string className = classT.Name;
                if (classT.InheritsFrom(typeof(IBlock))) className = $"(Block)\n{className}";
                CreateItemPropetry(className, StringItemPointer.PropertyType.Item);
            }
            CreateItemPropetry("Assets: ", StringItemPointer.PropertyType.Header);

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
                var typePath = asset.type.Split(new[] {'.'});
                string assetName = $"({typePath[typePath.Length - 1]})\n{asset.name}";
                CreateItemPropetry(assetName, StringItemPointer.PropertyType.Item);
            }
            CreateItemPropetry("Prefabs: ", StringItemPointer.PropertyType.Header);
            foreach (PrefabBundle prefab in prefabs)
            {
                string prefabName = prefab.name;
                if (prefab.tags.Contains("Block")) prefabName = $"(Block)\n{prefabName}";
                CreateItemPropetry(prefabName, StringItemPointer.PropertyType.Item);
            }
            
        }

        private void CreateItemPropetry(string name, StringItemPointer.PropertyType type)
        {
            StringItemPointer pointer = DynamicPool.Instance.Get(prefabCategory, contentInfo);
            pointer.GetPointer<Text>("Text").text = name;
            pointer.SetVisual(type);
            itemsMod.AddLast(pointer);
        }

        private void ClearListProperty()
        {
            foreach (var itemPointer in itemsMod)
            {
                DynamicPool.Instance.Return(itemPointer);
            }
            itemsMod.Clear();
        }


    }
}