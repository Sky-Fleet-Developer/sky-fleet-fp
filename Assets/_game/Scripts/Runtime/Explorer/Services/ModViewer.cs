using System.Collections.Generic;
using Core.Explorer.Content;
using Core.UiStructure;
using Core.UIStructure.Utilities;
using Core.Utilities;
using Runtime.Explorer.ModContent;
using Runtime.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Explorer.Services
{
    public class ModViewer : Service
    {
        [Header("StaticHierarchy")]
        public RectTransform selectionScrollRoot;

        [SerializeField, FoldoutGroup("Sources")] public ButtonItemPointer selectModButton;
        [SerializeField, FoldoutGroup("Sources")] private ModInfoViewer modInfoViewer;

        private LinkedList<ItemPointer> buttons = new LinkedList<ItemPointer>();

        private Mod selected;

        public Mod CurrentMod => selected;

        private LinkedList<Mod> maskMods;

        protected override void Awake()
        {
            base.Awake();
            selected = null;
            ModReader.OnModsLoaded(OnModsInit);
        }

        public void SetMaskMod(LinkedList<Mod> mods)
        {
            maskMods = mods;
            UpdateMask();
        }

        public void ClearMask()
        {
            maskMods = null;
            UpdateMask();
        }

        private void UpdateMask()
        {
            if (maskMods == null || maskMods.Count == 0)
            {
                foreach (ItemPointer item in buttons)
                {
                    item.gameObject.SetActive(true);
                }
            }
            else
            {
                foreach (ItemPointer item in buttons)
                {
                    bool isNo = true;
                    foreach (Mod mod in maskMods)
                    {
                        if (item.name == mod.name)
                        {
                            item.gameObject.SetActive(false);
                            isNo = false;
                            break;
                        }
                    }
                    if(isNo)
                    {
                        item.gameObject.SetActive(true);
                    }
                }
            }
        }


        private void OnModsInit(List<Mod> mods)
        {
            ClearButtons();
            InitButtons(mods);
        }

        /// <summary>
        /// show all mod properties - dll types, assets, prefabs and other
        /// </summary>
        private void ShowMod(Mod mod)
        {
            modInfoViewer.ApplyInfo(mod);
            selected = mod;
        }

        private void InitButtons(List<Mod> mods)
        {
            foreach (Mod mod in mods)
            {
                ButtonItemPointer item = DynamicPool.Instance.Get(selectModButton, selectionScrollRoot);
                item.SetVisual(mod.name, (System.Action)(() => ShowMod(mod)), FontStyle.Bold, 18);
                item.name = mod.name;
                buttons.AddLast(item);
            }
        }

        private void ClearButtons()
        {
            foreach (ItemPointer itemPointer in buttons)
            {
                itemPointer.SetVisual((System.Action)(() => { }));
                DynamicPool.Instance.Return(itemPointer);
            }
            buttons.Clear();
        }
    }
}

