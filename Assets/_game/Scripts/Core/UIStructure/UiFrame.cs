using System.Linq;
using System.Collections;
using Core.UiStructure;
using Core.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UIStructure
{
    public class UiFrame : UiBlockBase
    {

        [SerializeField] private Button exitButton;

        [SerializeField] private RectTransform content;

        private IUiBlock[] blocks;

        private LayoutGroup layout;
        private LayoutType currentLayout = LayoutType.None;

        protected override void Awake()
        {
            base.Awake();
            exitButton.onClick.AddListener(OnClickExit);
        }

        /*public struct BlockForFrame
        {
            public IUiBlock target;
        }*/

        public enum LayoutType
        {
            None = -1,
            Horizontal = 0,
            Vertical = 1,
        }

        public void Apply(LayoutType layoutType, params IUiBlock[] blocksForFrame)
        {
            SetLayout(layoutType);
            SetBlocks(blocksForFrame, layout, layoutType);
        }

        private void SetLayout(LayoutType layoutType)
        {
            if (layoutType != currentLayout)
            {
                Destroy(layout);
                switch (layoutType)
                {
                    case LayoutType.Horizontal:
                        layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
                        break;
                    case LayoutType.Vertical:
                        layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                        break;
                }
            }
            else
            {
                layout.enabled = true;
            }
            currentLayout = layoutType;
        }

        private void SetBlocks(IUiBlock[] blocksForFrame, LayoutGroup layoutT, LayoutType layoutType)
        {
            blocks = blocksForFrame;
            float height = 0;
            float width = 0;
            if (layoutType == LayoutType.Horizontal)
            {
                foreach (IUiBlock block in blocksForFrame)
                {
                    Rect rect = block.RectTransform.rect;
                    width += rect.width;
                    height = Mathf.Max(height, rect.height);
                }
            }
            else
            {
                foreach (IUiBlock block in blocksForFrame)
                {
                    Rect rect = block.RectTransform.rect;
                    height += rect.height;
                    width = Mathf.Max(width, rect.width);
                }
            }
            foreach (IUiBlock block in blocksForFrame)
            {
                block.Frame = this;
                block.RectTransform.localScale = Vector3.one;
                block.RectTransform.SetParent(content);
            }
            RectTransform.sizeDelta = new Vector2(width, height);
        }

        public bool GetBlock<T>(out T block) where T : IUiBlock
        {
            block = (T)blocks.FirstOrDefault(x => x.GetType() == typeof(T));
            return block != null;
        }

        public bool GetBlock<T>(out T block, System.Type type) where T : IUiBlock
        {
            block = (T)blocks.FirstOrDefault(x => x.GetType() == type);
            return block != null;
        }

        private void OnClickExit()
        {
            Close();
        }

        public void Close()
        {
            Structure.StartCoroutine(Hide());
        }

        public override IEnumerator Show(BlockSequenceSettings settings = null)
        {
            if (blocks == null) yield return base.Show(settings);
            yield return base.Show(settings);
            foreach (IUiBlock block in blocks)
            {
                Structure.StartCoroutine(block.Show(new EmptySettingsShow()));
            }
        }

        public override IEnumerator Hide(BlockSequenceSettings settings = null)
        {
            if (layout) layout.enabled = false;
            if (blocks == null)
            {
                yield return base.Hide(settings);
            }
            else
            {
                yield return base.Hide(settings);
                foreach (IUiBlock block in blocks)
                {
                    Structure.StartCoroutine(block.Hide(new EmptySettingsHide()));
                }
                foreach (IUiBlock uiBlock in blocks)
                {
                    uiBlock.RectTransform.SetParent(transform.parent);   
                }
            }
            DynamicPool.Instance.Return(this);
        }

        private class EmptySettingsShow : BlockSequenceSettings
        {
            public override IEnumerator ApplySequenceSettings(UiBlockBase block)
            {
                yield break;
            }
        }

        private class EmptySettingsHide : BlockSequenceSettings
        {
            public override IEnumerator ApplySequenceSettings(UiBlockBase block)
            {
                yield break;
            }
        }
    }
}