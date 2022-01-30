using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Core.UiStructure;
using Core.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UIStructure
{
    public class Window : Service
    {
        [SerializeField] protected RectTransform content;
        public RectTransform Content => content;

        protected IService[] blocks = new IService[0];

        protected LayoutGroup layoutGroup;
        protected LayoutType currentLayout = LayoutType.None;

        protected override void Awake()
        {
            base.Awake();
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
        
        public void Append(int index, params IService[] blocksForFrame)
        {
            if (blocks == null)
            {
                Apply(currentLayout, blocksForFrame);
            }
            else
            {
                List<IService> newBlocks = blocks.ToList();
                newBlocks.InsertRange(index, blocksForFrame);
                Apply(currentLayout, newBlocks.ToArray());
            }
        }

        public void Apply(LayoutType layoutType, params IService[] blocksForFrame)
        {
            SetLayout(layoutType);
            SetBlocks(blocksForFrame, layoutGroup, layoutType);
        }

        public void SetLayout(LayoutType layoutType)
        {
            if (layoutType != currentLayout)
            {
                if(layoutGroup) Destroy(layoutGroup);
                switch (layoutType)
                {
                    case LayoutType.Horizontal:
                        layoutGroup = content.gameObject.AddComponent<HorizontalLayoutGroup>();
                        break;
                    case LayoutType.Vertical:
                        layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
                        break;
                }
            }
            else
            {
                if(layoutGroup) layoutGroup.enabled = true;
            }
            currentLayout = layoutType;
        }

        private void SetBlocks(IService[] blocksForFrame, LayoutGroup layoutT, LayoutType layoutType)
        {
            blocks = blocksForFrame;
            float height = 0;
            float width = 0;
            if (layoutType == LayoutType.Horizontal)
            {
                foreach (IService block in blocksForFrame)
                {
                    Rect rect = block.RectTransform.rect;
                    width += rect.width;
                    height = Mathf.Max(height, rect.height);
                }
            }
            else
            {
                foreach (IService block in blocksForFrame)
                {
                    Rect rect = block.RectTransform.rect;
                    height += rect.height;
                    width = Mathf.Max(width, rect.width);
                }
            }
            foreach (IService block in blocksForFrame)
            {
                block.Window = this;
                block.RectTransform.localScale = Vector3.one;
                block.RectTransform.SetParent(content);
            }
            RectTransform.sizeDelta = new Vector2(width, height);
        }

        public bool GetBlock<T>(out T block) where T : class, IService
        {
            if (blocks == null)
            {
                block = null;
                return false;
            }

            var type = typeof(T);
            block = (T)blocks.FirstOrDefault(x => x.GetType() == type);
            return block != null;
        }

        public bool GetBlock(out IService block, System.Type type)
        {
            if (blocks == null)
            {
                block = null;
                return false;
            }
            block = blocks.FirstOrDefault(x => x.GetType() == type);
            return block != null;
        }
        
        public void Close()
        {
            Bearer.StartCoroutine(Hide());
        }
        
        public void Open()
        {
            Bearer.StartCoroutine(Show());
        }

        public override IEnumerator Show(BlockSequenceSettings settings = null)
        {
            if (blocks == null) yield return base.Show(settings);
            yield return base.Show(settings);
            foreach (IService block in blocks)
            {
                Bearer.StartCoroutine(block.Show(new EmptySettingsShow()));
            }
        }

        public override IEnumerator Hide(BlockSequenceSettings settings = null)
        {
            if (layoutGroup) layoutGroup.enabled = false;
            if (blocks != null)
            {
                foreach (IService block in blocks)
                {
                    Bearer.StartCoroutine(block.Hide(new EmptySettingsHide()));
                }
            }
            yield return base.Hide(settings);
            blocks = null;
            DynamicPool.Instance.Return(Bearer);
        }

        #region Self layout

        public void Fullscreen()
        {
            RectTransform.anchorMax = Vector2.one;
            RectTransform.anchorMin = Vector2.zero;
            RectTransform.sizeDelta = Vector2.zero;
            RectTransform.anchoredPosition = Vector2.zero;
        }

        #endregion

        private class EmptySettingsShow : BlockSequenceSettings
        {
            public override IEnumerator ApplySequenceSettings(Service block)
            {
                yield break;
            }
        }

        private class EmptySettingsHide : BlockSequenceSettings
        {
            public override IEnumerator ApplySequenceSettings(Service block)
            {
                yield break;
            }
        }
    }
}