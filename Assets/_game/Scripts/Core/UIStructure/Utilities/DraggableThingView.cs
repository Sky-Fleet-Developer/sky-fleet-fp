using UnityEngine.EventSystems;

namespace Core.UIStructure.Utilities
{
    public abstract class DraggableThingView<T> : ThingView<T>, IDraggableView, IDragHandler, IBeginDragHandler, IEndDragHandler where T : IDraggableItem
    {
        private bool _isDragging;
        public bool IsDragging => _isDragging;
        private IDragCallbacks<DraggableThingView<T>> _dragCallbacks;
        public IDragAndDropContainer MyContainer { get; private set; }
        public IDraggableItem MyDraggableItem => Data;
        public void SetContainer(IDragAndDropContainer container)
        {
            MyContainer = container;
        }
        public void SetDragCallbacks(IDragCallbacks<DraggableThingView<T>> callbacks)
        {
            _dragCallbacks = callbacks;
        }
        
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            _dragCallbacks.OnChildDragStart(this, eventData.position);
            _isDragging = true;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            _dragCallbacks.OnChildDragEnd(this);
            _isDragging = false;
        }
        
        public virtual void OnDrag(PointerEventData eventData)
        {
            _dragCallbacks.OnChildDragContinue(this, eventData.delta);
        }
    }
}