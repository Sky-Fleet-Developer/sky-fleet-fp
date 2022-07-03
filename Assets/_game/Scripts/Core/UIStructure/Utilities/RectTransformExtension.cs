using UnityEngine;

namespace Core.UIStructure.Utilities
{
    public static class RectTransformExtension
    {
        public static void Fullscreen(this RectTransform rectTransform)
        {
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
