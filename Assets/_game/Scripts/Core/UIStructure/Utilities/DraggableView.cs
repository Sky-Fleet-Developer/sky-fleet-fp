using System;
using UnityEngine;

namespace Core.UIStructure.Utilities
{
    public class DraggableView : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        public void Align(IDraggable draggable)
        {
            var rtr = draggable.RectTransform;
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