using System.Collections;
using System.Collections.Generic;
using Core.Explorer.Content;
using Core.UiStructure;
using Core.Utilities;
using Core.Utilities.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.ModContent
{
    public class ModViewer : UiBlockBase
    {
        [Header("StaticHierarchy")]
        public RectTransform selectionScrollRoot;

        [SerializeField, FoldoutGroup("Sources")] public ButtonItemPointer selectModButton;
        [SerializeField, FoldoutGroup("Sources")] private ModInfoViewer modInfoViewer;

        private LinkedList<ItemPointer> buttons = new LinkedList<ItemPointer>();

        private Mod selected;

        public Mod CurrentMod => selected;

        protected override void Awake()
        {
            base.Awake();
            selected = null;
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
            modInfoViewer.ApplyInfo(mod);
            selected = mod;
        }

        private void InitButtons(List<Mod> mods)
        {
            foreach (var mod in mods)
            {
                var item = DynamicPool.Instance.Get(selectModButton, selectionScrollRoot);
                item.SetVisual(mod.name, (System.Action)(() => ShowMod(mod)), FontStyle.Bold, 18);
                buttons.AddLast(item);
            }
        }

        private void ClearButtons()
        {
            foreach (var itemPointer in buttons)
            {
                DynamicPool.Instance.Return(itemPointer);
            }
            buttons.Clear();
        }
    }
}

