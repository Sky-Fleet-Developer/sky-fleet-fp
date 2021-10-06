using System.Collections;
using System.Collections.Generic;
using Core.Explorer.Content;
using Core.UiStructure;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.ModContent
{
    public class ModViewer : UiBlockBase
    {
        [Header("StaticHierarchy")]
        public RectTransform selectionScrollRoot;
        
        [FoldoutGroup("Sources")]
        public ItemPointer selectModButton;


        private List<ItemPointer> buttons = new List<ItemPointer>();
        protected override void Awake()
        {
            base.Awake();
            
            ModReader.OnModsLoaded(OnModsInit);
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
            Debug.Log($"Select mod {mod.name}");
        }

        private void InitButtons(List<Mod> mods)
        {
            foreach (var mod in mods)
            {
                var item = DynamicPool.Instance.Get(selectModButton, selectionScrollRoot);
                var nameText = item.GetPointer<Text>("Name");
                nameText.text = mod.name;
                var button = item.GetPointer<Button>("this");
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ShowMod(mod));
            }
        }

        private void ClearButtons()
        {
            if (buttons.Count > 0)
            {
                foreach (var itemPointer in buttons)
                {
                    DynamicPool.Instance.Return(itemPointer);
                }
                buttons = new List<ItemPointer>();
            }
        }
    }
}

