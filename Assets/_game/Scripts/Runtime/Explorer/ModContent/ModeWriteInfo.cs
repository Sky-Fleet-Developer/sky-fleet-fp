using System;
using System.Collections;
using System.Collections.Generic;
using Core.Explorer.Content;
using Core.UiStructure;
using Core.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.ModContent
{
    public class ModeWriteInfo : MonoBehaviour
    {
        [SerializeField] private Text nameMod;

        [SerializeField] private Transform contentInfo;

        [SerializeField] private ItemModPropertyUI prefabCategory;

        private LinkedList<ItemModPropertyUI> itemsMod = new LinkedList<ItemModPropertyUI>();

        public void WriteInfoMod(Mod mod)
        {
            nameMod.text = mod.name;
            ClearListProperty();

            CreateItemPropetry("Classes: ", ItemModPropertyUI.PropertyType.Header);
            foreach (Type classT in mod.GetClasses())
            {
                CreateItemPropetry(classT.Name, ItemModPropertyUI.PropertyType.Item);
            }
            CreateItemPropetry("Assets: ", ItemModPropertyUI.PropertyType.Header);
            foreach (string name in mod.GetAssetsNames())
            {
                CreateItemPropetry(name, ItemModPropertyUI.PropertyType.Item);
            }
            CreateItemPropetry("Prefabs: ", ItemModPropertyUI.PropertyType.Header);
            foreach (string name in mod.GetPrefabsNames())
            {
                CreateItemPropetry(name, ItemModPropertyUI.PropertyType.Item);
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