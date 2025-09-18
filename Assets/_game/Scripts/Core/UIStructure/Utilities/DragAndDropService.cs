using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zenject;

namespace Core.UIStructure.Utilities
{
    public interface IDraggable
    {
        RectTransform RectTransform { get; }
        void OnDropTo(IDropHandler destination);
    }

    public interface IDropHandler : IEventSystemHandler
    {
        void OnDropContent(DropEventData eventData);
    }

    public class DropEventData : BaseEventData
    {
        public DropEventData(EventSystem eventSystem, IReadOnlyList<IDraggable> content) : base(eventSystem)
        {
            Content = content;
        }

        public IReadOnlyList<IDraggable> Content;
    }
    
    public class DragAndDropService : MonoBehaviour, ILoadAtStart, IInstallerWithContainer
    {
        [SerializeField] private RectTransform container;
        [SerializeField] private DraggableView draggablePlaceholder;
        [SerializeField] private InputAction cancelDragInput;
        private readonly List<IDraggable> _cache = new (10);
        private readonly List<DraggableView> _draggablesViews = new (10);
        private List<RaycastResult> _raycastResults = new ();
        private Vector2 _initPosition;
        private Vector2 _position;
        public bool IsDragNow => _cache.Count > 0;

        public void BeginDrag(Vector2 initPosition, IEnumerable<IDraggable> draggables)
        {
            if (_cache.Count > 0)
            {
                _cache.Clear();
            }
            _cache.AddRange(draggables);
            BeginDragPrivate(initPosition);
        }
        
        public void BeginDrag(Vector2 initPosition, IDraggable draggable)
        {
            if (_cache.Count > 0)
            {
                _cache.Clear();
            }
            _cache.Add(draggable);
            BeginDragPrivate(initPosition);
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

        private void BeginDragPrivate(Vector2 initPosition)
        {
            _position = initPosition;
            _initPosition = initPosition;
            cancelDragInput.Enable();
            while (_draggablesViews.Count < _cache.Count)
            {
                DraggableView instance = Instantiate(draggablePlaceholder, container);
                _draggablesViews.Add(instance);
            }

            for (var i = 0; i < _cache.Count; i++)
            {
                _draggablesViews[i].gameObject.SetActive(true);
                _draggablesViews[i].Align(_cache[i]);
            }
        }
        
        private void RaycastAndTryDrop()
        {
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position = _position}, _raycastResults);
            var eventData = new DropEventData(EventSystem.current, _cache);
            foreach (var raycastResult in _raycastResults)
            {
                if (raycastResult.gameObject.TryGetComponent(out IDropHandler dropHandler))
                {
                    dropHandler.OnDropContent(eventData);
                    if (eventData.used)
                    {
                        foreach (var draggable in _cache)
                        {
                            draggable.OnDropTo(dropHandler);
                        }

                        break;
                    }
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
            _cache.Clear();
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