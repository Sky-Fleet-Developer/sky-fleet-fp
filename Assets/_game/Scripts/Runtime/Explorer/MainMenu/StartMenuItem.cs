using System;
using Core.UiStructure;
using Core.UIStructure;
using Core.UIStructure.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Explorer
{
    [Serializable]
    public struct StartMenuItem
    {
        public Service[] blocks;
        public string description;
        [DrawWithUnity] public FontStyle style;
        [DrawWithUnity] public TextAnchor alignment;

        private Action<IService[]> onBlockWasOpen;

        public void Apply(ButtonItemPointer button, Action<IService[]> onBlockWasOpen)
        {
            this.onBlockWasOpen = onBlockWasOpen;
            Action action = OpenBlock;
            button.SetVisual(description, style, alignment, action);
        }

        private void OpenBlock()
        {
            IService[] services = new IService[blocks.Length];
            var window = ServiceIssue.Instance.CreateWindow<FramedWindow>();
            for (int i = 0; i < blocks.Length; i++)
            {
                services[i] = window.Bearer.Create(blocks[i], window);
            }

            window.Apply(Window.LayoutType.Horizontal, services);
            window.Open();
            onBlockWasOpen?.Invoke(services);
        }
    }
}