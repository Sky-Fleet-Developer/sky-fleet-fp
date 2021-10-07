using System;
using Core.UiStructure;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UIStructure
{
    public class UiFrame : MonoBehaviour
    {
        public Button exitButton;
        
        public IUiBlock block;

        public RectTransform rectTransform { get; private set; }
        private void Awake()
        {
            rectTransform = transform as RectTransform;
            exitButton.onClick.AddListener(OnClickExit);
        }

        public void Apply(IUiBlock target)
        {
            block = target;

            target.Frame = this;
            target.RectTransform.localScale = Vector3.one;

            rectTransform.SetParent(target.RectTransform.parent);

            rectTransform.anchorMax = target.RectTransform.anchorMax;
            rectTransform.anchorMin = target.RectTransform.anchorMin;
            rectTransform.anchoredPosition = target.RectTransform.anchoredPosition;
            rectTransform.sizeDelta = target.RectTransform.sizeDelta;
            target.RectTransform.SetParent(rectTransform);
        }

        private void OnClickExit()
        {
            if (block != null) StartCoroutine(block.Hide());
        }
    }
}