using System;
using System.Collections;
using System.Collections.Generic;
using Core.ContentSerializer.HierarchySerializer;
using Core.Explorer.Content;
using Core.UiStructure;
using Core.Utilities;
using UnityEngine;
using UnityEngine.UI;
using AssetBundle = Core.ContentSerializer.ResourceSerializer.AssetBundle;

namespace Runtime.Explorer.ModContent
{
    public class ModInfoViewer : MonoBehaviour
    {
        [SerializeField] private Text nameMod;

        [SerializeField] private Transform contentInfo;

        [SerializeField] private ItemModPropertyUI prefabCategory;

        private LinkedList<ItemModPropertyUI> itemsMod = new LinkedList<ItemModPropertyUI>();

        public void ApplyInfo(Mod mod)
        {
            nameMod.text = mod.name;
            ClearListProperty();

            CreateItemPropetry("Classes: ", ItemModPropertyUI.PropertyType.Header);
            foreach (Type classT in mod.GetClasses())
            {
                CreateItemPropetry(classT.Name, ItemModPropertyUI.PropertyType.Item);
            }
            CreateItemPropetry("Assets: ", ItemModPropertyUI.PropertyType.Header);
            foreach (AssetBundle asset in mod.module.assetsCache)
            {
                var typePath = asset.type.Split(new[] {'.'});
                string assetName = $"({typePath[typePath.Length - 1]}) {asset.name}";
                CreateItemPropetry(assetName, ItemModPropertyUI.PropertyType.Item);
            }
            CreateItemPropetry("Prefabs: ", ItemModPropertyUI.PropertyType.Header);
            foreach (PrefabBundle prefab in mod.module.prefabsCache)
            {
                string prefabName = prefab.name;
                if (prefab.tags.Contains("Block")) prefabName = $"(Block){prefabName}";
                CreateItemPropetry(prefabName, ItemModPropertyUI.PropertyType.Item);
            }
            
        }

        private void CreateItemPropetry(string name, ItemModPropertyUI.PropertyType type)
        {
            ItemModPropertyUI pointer = DynamicPool.Instance.Get(prefabCategory, contentInfo);
            pointer.GetPointer<Text>("Text").text = name;
            pointer.SetPropery(type, "TypeProperty");
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