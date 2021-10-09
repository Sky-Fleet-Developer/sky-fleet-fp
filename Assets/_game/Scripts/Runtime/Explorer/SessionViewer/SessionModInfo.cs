using System.Collections.Generic;
using System.Linq;
using Core.Explorer.Content;
using Core.Utilities;
using Core.Utilities.UI;
using UnityEngine;

namespace Runtime.Explorer.SessionViewer
{
    public class SessionModInfo : MonoBehaviour
    {
        [SerializeField] private ButtonItemSelectablePointer buttonItemPrefab;
        [SerializeField] private Transform content;

        private LinkedList<ItemMod> itemMods = new LinkedList<ItemMod>();

        private ItemMod currectTakeItem;

        public Mod GetSelectMod => currectTakeItem?.KeepMod;


        private class ItemMod
        {
            public ButtonItemSelectablePointer UIPointer;
            public Mod KeepMod;

            public ItemMod(ButtonItemSelectablePointer UIPointer, Mod KeepMod)
            {
                this.KeepMod = KeepMod;
                this.UIPointer = UIPointer;
            }
        }

        private void Awake()
        {
            currectTakeItem = null;
        }

        public void UpdateListMods(LinkedList<Mod> mods)
        {
            ClearList();
            currectTakeItem = null;
            foreach(Mod mod in mods)
            {
                CreateItemMod(mod, itemMods);
            }
        }

        public void RemoveModFromList(Mod mod)
        {
            ItemMod item = itemMods.Where((x) => x.KeepMod == mod ).FirstOrDefault();
            if(item != null)
            {
                DynamicPool.Instance.Return(item.UIPointer);
                item.UIPointer.IsSelected = false;
                if(item == currectTakeItem)
                {
                    currectTakeItem = null;
                }
                itemMods.Remove(item);
                item = null;
            }
        }

        public void AddModToList(Mod mod)
        {
            CreateItemMod(mod, itemMods);
        }

        private void CreateItemMod(Mod mod, LinkedList<ItemMod> putOnList)
        {
            ButtonItemSelectablePointer keepMod = DynamicPool.Instance.Get(buttonItemPrefab, content);
            ItemMod item = new ItemMod(keepMod, mod);
            keepMod.SetVisual( mod.name, (System.Action)(() => CallSelectMod(item)));
            putOnList.AddLast(item);
        }

        private void CallSelectMod(ItemMod item)
        {
            if(currectTakeItem != null)
            {
                currectTakeItem.UIPointer.IsSelected = false;
            }

            currectTakeItem = item;
            item.UIPointer.IsSelected = true;
        }

        private void ClearList()
        {
            foreach(ItemMod item in itemMods)
            {
                DynamicPool.Instance.Return(item.UIPointer);
                item.UIPointer.IsSelected = false;

            }
            itemMods.Clear();
        }
    }
}