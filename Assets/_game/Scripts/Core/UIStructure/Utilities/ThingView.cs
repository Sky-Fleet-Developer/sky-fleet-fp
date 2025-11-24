using System;
using UnityEngine;

namespace Core.UIStructure.Utilities
{
    public interface IDragCallbacks<in TView>
    {
        void OnChildDragStart(TView view, Vector2 position);
        void OnChildDragEnd(TView view);
        void OnChildDragContinue(TView view, Vector2 delta);
    }

    public abstract class ThingView<T> : MonoBehaviour, ISelectionTarget, IMultipleSelectionTarget
    {
        private RectTransform _rectTransform;

        public RectTransform RectTransform
        {
            get
            {
                _rectTransform ??= (RectTransform)transform;
                return _rectTransform;
            }
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
    }
}