using System;
using System.Collections.Generic;
using Core.UiStructure;
using Core.UIStructure.Utilities;
using Core.Utilities;
using Runtime.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Explorer.Services
{
    public class MainMenu : Service
    {
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
            menu.Apply(buttonInstance, OnBlockWasOpened);
            buttonInstance.SetVisual(fontSize);
            buttons.Add(buttonInstance);
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
