using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Boot_strapper;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zenject;

namespace Core.UIStructure.Utilities
{
    public interface IDraggableItem
    {
        int Order { get; }
    }
    public interface IDraggableView
    {
        RectTransform RectTransform { get; }
        IDraggableItem MyDraggableItem { get; }
        IDragAndDropContainer MyContainer { get; }
    }

    public interface IDragAndDropContainer : IEventSystemHandler
    {
        void OnDropContent(DropEventData eventData);
        IDragAndDropContainer ParentContainer { get => null; }
    }

    public class DropEventData : BaseEventData
    {
        public DropEventData(EventSystem eventSystem, IDragAndDropContainer source, List<IDraggableItem> content) : base(eventSystem)
        {
            Content = content;
            Source = source;
        }

        public readonly List<IDraggableItem> Content;
        public readonly IDragAndDropContainer Source;
    }
    
    public class DragAndDropService : MonoBehaviour, ILoadAtStart, IMyInstaller
    {
        [SerializeField] private RectTransform container;
        [SerializeField] private DraggablePreview draggablePlaceholder;
        [SerializeField] private InputAction cancelDragInput;
        private readonly List<IDraggableItem> _cacheItems = new (10);
        private readonly List<IDraggableView> _cacheViews = new (10);
        private readonly List<DraggablePreview> _draggablesViews = new (10);
        private List<RaycastResult> _raycastResults = new ();
        private Vector2 _initPosition;
        private Vector2 _position;
        private IDragAndDropContainer _source;
        public bool IsDragNow => _cacheItems.Count > 0;

        public void BeginDrag(Vector2 initPosition, IDragAndDropContainer source, IEnumerable<IDraggableView> draggables)
        {
            if (_cacheItems.Count > 0)
            {
                _cacheItems.Clear();
                _cacheViews.Clear();
            }

            foreach (var draggableView in draggables.OrderBy(x => x.MyDraggableItem.Order))
            {
                _cacheItems.Add(draggableView.MyDraggableItem);
                _cacheViews.Add(draggableView);
            }
            BeginDragPrivate(source, initPosition);
        }
        
        public void BeginDrag(Vector2 initPosition, IDraggableView draggableView)
        {
            if (_cacheItems.Count > 0)
            {
                _cacheItems.Clear();
                _cacheViews.Clear();
            }
            _cacheItems.Add(draggableView.MyDraggableItem);
            _cacheViews.Add(draggableView);
            BeginDragPrivate(draggableView.MyContainer, initPosition);
        }
        
        public void Move(Vector2 position)
        {
            Vector2 delta = position - _position;
            _position += delta;
            foreach (var draggable in _draggablesViews)
            {
                draggable.Move(delta);
            }
        }

        public void Drop()
        {
            RaycastAndTryDrop();
            OnEndDrag();
        }

        public Task Load()
        {
            cancelDragInput.performed += CancelDrag;
            return Task.CompletedTask;
        }

        private void OnDestroy()
        {
            cancelDragInput.performed -= CancelDrag;
            cancelDragInput.Dispose();
        }

        private void BeginDragPrivate(IDragAndDropContainer source, Vector2 initPosition)
        {
            _source = source;
            _position = initPosition;
            _initPosition = initPosition;
            cancelDragInput.Enable();
            while (_draggablesViews.Count < _cacheItems.Count)
            {
                DraggablePreview instance = Instantiate(draggablePlaceholder, container);
                _draggablesViews.Add(instance);
            }

            for (var i = 0; i < _cacheViews.Count; i++)
            {
                _draggablesViews[i].gameObject.SetActive(true);
                _draggablesViews[i].Align(_cacheViews[i]);
            }
        }
        
        private void RaycastAndTryDrop()
        {
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position = _position}, _raycastResults);
            var eventData = new DropEventData(EventSystem.current, _source, _cacheItems);
            foreach (var raycastResult in _raycastResults)
            {
                ExecuteEvents.ExecuteHierarchy<IDragAndDropContainer>(raycastResult.gameObject, eventData,
                    (a, b) => a.OnDropContent(b as DropEventData));
                /*if (raycastResult.gameObject.TryGetComponent(out IDragAndDropContainer dropHandler))
                {
                    dropHandler.OnDropContent(eventData);
                    if (eventData.used)
                    {
                        _source = null;
                        return;
                    }
                }*/
                if (eventData.used)
                {
                    _source = null;
                    return;
                }
            }
        }

        private void OnEndDrag()
        {
            cancelDragInput.Disable();
            foreach (var draggable in _draggablesViews)
            {
                draggable.gameObject.SetActive(false);
            }
            _cacheItems.Clear();
            _cacheViews.Clear();
        }
        

        private void CancelDrag(InputAction.CallbackContext obj)
        {
            OnEndDrag();
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<DragAndDropService>().FromInstance(this);
        }
    }
}