using System;
using System.Collections.Generic;
using Core.UiStructure;
using Core.UIStructure;
using Core.Utilities;
using Runtime.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Runtime.Explorer.Services
{
    public class MainMenu : Service
    {
        [Inject] private ServiceIssue _serviceIssue;
        public List<StartMenuItem> menus;
        [FoldoutGroup("Style")]
        public ButtonItemPointer buttonSource;
        [FoldoutGroup("Style")]
        public int fontSize = 20;

        [SerializeField] private Transform buttonsRoot;

        [SerializeField] private Transform contentFromFrames;

        private List<ButtonItemPointer> buttons = new List<ButtonItemPointer>();

        private void Start()
        {
            if (!buttonsRoot)
            {
                throw new NullReferenceException("buttons root is empty");
            }
            foreach (StartMenuItem menu in menus)
            {
                InsertMenuButton(menu);
            }
        }

        private void InsertMenuButton(StartMenuItem menu)
        {
            ButtonItemPointer buttonInstance = DynamicPool.Instance.Get(buttonSource, buttonsRoot);
            menu.OnBlockWasOpen = OnBlockWasOpened;
            buttonInstance.SetVisual(menu.description, menu.style, menu.alignment, (Action)(() => OpenBlock(menu)), fontSize);
            buttons.Add(buttonInstance);
        }
        
        private void OpenBlock(StartMenuItem item)
        {
            IService[] services = new IService[item.blocks.Length];
            var window = _serviceIssue.CreateWindow<FramedWindow>();
            for (int i = 0; i < item.blocks.Length; i++)
            {
                services[i] = window.Bearer.Create(item.blocks[i], window);
            }

            window.Apply(Window.LayoutType.Horizontal, services);
            window.Open();
            item.OnBlockWasOpen?.Invoke(services);
        }

        public void AddMenu(StartMenuItem menu)
        {
            menus.Add(menu);
            InsertMenuButton(menu);
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
    }
}
