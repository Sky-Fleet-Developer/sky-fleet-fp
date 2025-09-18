using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.UIStructure.Utilities
{
    public interface IDragCallbacks<TView>
    {
        void OnChildDragStart(TView view, Vector2 position);
        void OnChildDragEnd(TView view);
        void OnChildDragContinue(TView view, Vector2 delta);
    }
    public abstract class ThingView<T> : MonoBehaviour, ISelectionTarget, IMultipleSelectionTarget, IDraggable, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        private RectTransform _rectTransform;
        private IDragCallbacks<ThingView<T>> _dragCallbacks;

        public RectTransform RectTransform
        {
            get
            {
                _rectTransform ??= (RectTransform)transform;
                return _rectTransform;
            }
        }

        public void SetDragCallbacks(IDragCallbacks<ThingView<T>> callbacks)
        {
            _dragCallbacks = callbacks;
        }
        public bool IsSelected { get; private set; }
        public abstract T Data { get; }
        public int Order => transform.GetSiblingIndex();
        public Action<IMultipleSelectionTarget, MultipleSelectionModifiers> OnInput { get; set; }
        public Action<ISelectionTarget> OnSelected { get; set; }
        public virtual void Selected()
        {
            IsSelected = true;
        }
        public virtual void Deselected()
        {
            IsSelected = false;
        }

        public abstract void SetData(T data);
        public abstract void RefreshView();
        public abstract void EmitSelection();
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragCallbacks.OnChildDragStart(this, eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragCallbacks.OnChildDragEnd(this);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            _dragCallbacks.OnChildDragContinue(this, eventData.delta);
        }

        public void OnDropTo(IDropHandler destination)
        {
        }
    }
}