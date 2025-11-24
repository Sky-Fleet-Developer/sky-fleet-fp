using System;
using UnityEngine;
using Zenject;

namespace Core.UIStructure.Utilities
{
    public abstract class DraggableThingsListView<TData, TView> : ThingsListView<TData, TView>, IDragAndDropContainer, IDragCallbacks<DraggableThingView<TData>> where TData : IDraggableItem where TView : DraggableThingView<TData>
    {
        [Inject] private DragAndDropService _dragAndDropService;
        private Vector2 _dragPosition;
        public Action<DropEventData> OnDropContentEvent;
        public virtual void OnDropContent(DropEventData eventData)
        {
            if (ReferenceEquals(eventData.Source, this))
            {
                return;
            }
            OnDropContentEvent?.Invoke(eventData);
        }

        private IDragAndDropContainer _parentContainer;
        public void SetParentContainer(IDragAndDropContainer container)
        {
            _parentContainer = container;
        }
        
        public virtual void OnChildDragStart(DraggableThingView<TData> view, Vector2 position)
        {
            if (_dragAndDropService == null)
            {
                return;
            }
            _dragPosition = position;
            if (view.IsSelected)
            {
                _dragAndDropService.BeginDrag(position, this, SelectionHandler.Selected);
            }
            else
            {
                _dragAndDropService.BeginDrag(position, view);
            }
        }
        
        public virtual void OnChildDragEnd(DraggableThingView<TData> view)
        {
            if (_dragAndDropService == null)
            {
                return;
            }
            _dragAndDropService.Drop();
        }

        public virtual void OnChildDragContinue(DraggableThingView<TData> view, Vector2 delta)
        {
            if (_dragAndDropService == null)
            {
                return;
            }
            _dragPosition += delta;
            _dragAndDropService.Move(_dragPosition);
        }

        protected override void InitItem(TView item)
        {
            base.InitItem(item);
            item.SetDragCallbacks(this);
            item.SetContainer(this);
        }
    }
}