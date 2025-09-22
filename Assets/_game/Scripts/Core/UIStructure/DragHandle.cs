using System;
using Core.Utilities.AsyncAwaitUtil.Source;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.UIStructure
{
    public class DragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float topPadding;
        [SerializeField] private float bottomPadding;
        [SerializeField] private float leftPadding;
        [SerializeField] private float rightPadding;
        private Canvas _canvas;
        private Rect _borders;
        private Rect _rect;

        private async void OnEnable()
        {
            await new WaitForEndOfFrame();
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _borders = _canvas.pixelRect;
            _rect = target.rect;
            _rect.center += (Vector2)target.position;
            _rect.xMax += rightPadding;
            _rect.xMin -= leftPadding;
            _rect.yMax += topPadding;
            _rect.yMin -= bottomPadding;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Move(eventData.delta);
            ProcessBorders();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ProcessBorders();
        }

        private void Move(Vector2 delta)
        {
            target.anchoredPosition += delta;
            _rect.center += delta;
        }

        private void ProcessBorders()
        {
            if (_rect.xMin < _borders.xMin)
            {
                Move(Vector2.right * (_borders.xMin - _rect.xMin));
            }
            if (_rect.xMax > _borders.xMax)
            {
                Move(Vector2.right * (_borders.xMax - _rect.xMax));
            }
            if (_rect.yMax > _borders.yMax)
            {
                Move(Vector2.up * (_borders.yMax - _rect.yMax));
            }
            if (_rect.yMin < _borders.yMin)
            {
                Move(Vector2.up * (_borders.yMin - _rect.yMin));
            }
        }
    }
}