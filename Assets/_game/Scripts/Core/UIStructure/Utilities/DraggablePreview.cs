using System;
using UnityEngine;

namespace Core.UIStructure.Utilities
{
    public class DraggablePreview : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        public void Align(IDraggableView iDraggableView)
        {
            var rtr = iDraggableView.RectTransform;
            var rightTopAngle = rtr.TransformPoint(rtr.rect.size);
            var leftBottomAngle = rtr.TransformPoint(-rtr.rect.size);
            var center = (rightTopAngle + leftBottomAngle) * 0.5f;
            var size = (rightTopAngle - leftBottomAngle) * 0.5f;
            _rectTransform.position = center;
            _rectTransform.sizeDelta = size;
        }

        public void Move(Vector2 offset)
        {
            _rectTransform.anchoredPosition += offset;
        }
    }
}