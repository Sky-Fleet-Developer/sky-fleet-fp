using System;
using System.Linq;
using System.Collections;
using Core.UiStructure;
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

        protected override void Awake()
        {
            base.Awake();
            exitButton.onClick.AddListener(OnClickExit);
        }

        /*public struct BlockForFrame
        {
            public IUiBlock target;
        }*/

        public enum TypeLayout
        {
            Horizontal = 0,
            Vertical = 1,
        }

        public void Apply(TypeLayout type, params IUiBlock[] blocksForFrame)
        {
            /*block = target;

            target.Frame = this;
            target.RectTransform.localScale = Vector3.one;

            rectTransform.SetParent(target.RectTransform.parent);

            rectTransform.anchorMax = target.RectTransform.anchorMax;
            rectTransform.anchorMin = target.RectTransform.anchorMin;
            rectTransform.anchoredPosition = target.RectTransform.anchoredPosition;
            rectTransform.sizeDelta = target.RectTransform.sizeDelta;
            target.RectTransform.SetParent(rectTransform);*/

            if (blocks != null)
                throw new NotImplementedException();

            if (layout != null)
            {
                Destroy(layout);
            }
            if (type == TypeLayout.Horizontal)
            {
                layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            else if (type == TypeLayout.Vertical)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            SetBlocks(blocksForFrame, layout, type);
        }

        private void SetBlocks(IUiBlock[] blocksForFrame, LayoutGroup layoutT, TypeLayout type)
        {
            blocks = blocksForFrame;
            float height = 0;
            float width = 0;
            if (type == TypeLayout.Horizontal)
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
            StartCoroutine(Hide());
        }

        public override IEnumerator Show(BlockSequenceSettings settings = null)
        {
            if (blocks == null) return base.Show(settings);
            foreach (IUiBlock block in blocks)
            {
                Structure.StartCoroutine(block.Show(new EmptySettingsShow()));
            }
            return base.Show(settings);
        }

        public override IEnumerator Hide(BlockSequenceSettings settings = null)
        {
            if (blocks == null) return base.Hide(settings);

            foreach (IUiBlock block in blocks)
            {
                Structure.StartCoroutine(block.Hide(new EmptySettingsHide()));
            }
            return base.Hide(settings);
        }

        private class EmptySettingsShow : BlockSequenceSettings
        {
            public override IEnumerator ApplySequenceSettings(UiBlockBase block)
            {
                yield return new WaitForSecondsRealtime(block.showTransition.length);
            }
        }

        private class EmptySettingsHide : BlockSequenceSettings
        {
            public override IEnumerator ApplySequenceSettings(UiBlockBase block)
            {
                yield return new WaitForSecondsRealtime(block.hideTransition.length);
            }
        }
    }
}