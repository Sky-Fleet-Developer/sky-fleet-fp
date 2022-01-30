using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.UiStructure;
using Core.UIStructure;
using Core.Utilities;
using Core.Utilities.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Explorer.MainMenu
{
    public class MainMenuService : Service
    {
        public List<UIBlockButton> menus;
        [FoldoutGroup("Style")]
        public ButtonItemPointer buttonSource;
        [FoldoutGroup("Style")]
        public int fontSize = 20;

        [SerializeField] private Transform contentFromFrames;

        private List<ButtonItemPointer> buttons = new List<ButtonItemPointer>();

        public void Start()
        {
            foreach (UIBlockButton uiBlockButton in menus)
            {
                ButtonItemPointer buttonInstance = DynamicPool.Instance.Get(buttonSource, transform);
                uiBlockButton.Apply(buttonInstance, OnBlockWasOpened);
                buttonInstance.SetVisual(fontSize);
                buttons.Add(buttonInstance);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (ButtonItemPointer buttonItemPointer in buttons)
            {
                buttonItemPointer.ResetVisual();
                if (DynamicPool.hasInstance)
                {
                    DynamicPool.Instance.Return(buttonItemPointer);
                }
            }
        }

        protected override void OnBlockFocusChanged(IService block)
        {
            base.OnBlockFocusChanged(block);
            if (block == null)
            {
                gameObject.SetActive(true);
                StartCoroutine(Show());
            }
        }

        private void OnBlockWasOpened(IService[] blocksBase)
        {
            //StartCoroutine(Hide());
            /*Window window = Bearer.CreateWindow(windowPrefab);
            window.transform.parent = contentFromFrames;
            window.Apply(Window.LayoutType.Horizontal, blocksBase);
            FocusOn(window);*/
        }
    
        [System.Serializable]
        public class UIBlockButton
        {
            public Service[] blocks;
            public string description;
            public FontStyle style;
            public TextAnchor alignment;

            private System.Action<IService[]> onBlockWasOpen;

            public void Apply(ButtonItemPointer button, System.Action<IService[]> onBlockWasOpen)
            {
                this.onBlockWasOpen = onBlockWasOpen;
                Action action = OpenBlock;
                button.SetVisual(description, style, alignment, action);
            }

            private void OpenBlock()
            {
                IService[] services = new IService[blocks.Length];
                var window = ServiceIssue.Instance.CreateWindow<FramedWindow>();
                for(int i = 0; i < blocks.Length; i++)
                {
                    services[i] = window.Bearer.Create(blocks[i], window);
                }
                window.Apply(Window.LayoutType.Horizontal, services);
                window.Open();
                onBlockWasOpen?.Invoke(services);
            }
        }
    }
}
