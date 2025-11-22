using System;
using System.Collections.Generic;
using Core.Utilities;
using UnityEngine;
using Zenject;

namespace Core.UIStructure.Utilities
{
    public abstract class ThingsListView<TData> : MonoBehaviour, IDragAndDropContainer, IDragCallbacks<ThingView<TData>>
    {
        public Action<DropEventData> OnDropContentEvent;
        private IDragAndDropContainer _parentContainer;
        public virtual void OnDropContent(DropEventData eventData)
        {
            if (ReferenceEquals(eventData.Source, this))
            {
                return;
            }
            OnDropContentEvent?.Invoke(eventData);
        }

        public void SetParentContainer(IDragAndDropContainer container)
        {
            _parentContainer = container;
        }

        public abstract void SetItems(IEnumerable<TData> items);
        public abstract void AddItem(TData data);
        public abstract void RemoveItem(TData data);
        public abstract void RefreshItem(TData data);
        public abstract void Select(TData data);


        public abstract void OnChildDragStart(ThingView<TData> view, Vector2 position);

        public abstract void OnChildDragEnd(ThingView<TData> view);

        public abstract void OnChildDragContinue(ThingView<TData> view, Vector2 delta);
    }

    public abstract class ThingsListView<TData, TView> : ThingsListView<TData> where TView : ThingView<TData>
    {
        [Inject] private DragAndDropService _dragAndDropService;
        [SerializeField] private Transform itemsContainer;
        protected List<TView> Views = new ();
        private TView _thingViewPrefab;
        public readonly MultipleSelectionHandler<TView> SelectionHandler = new ();
        protected List<TData> ThingsData = new();
        private Vector2 _dragPosition;
        protected Dictionary<TData, TView> ViewByData = new();

        protected virtual void Awake()
        {
            _thingViewPrefab = itemsContainer.GetComponentInChildren<TView>();
            DynamicPool.Instance.Return(_thingViewPrefab);
        }

        protected virtual void InitItem(TView item){}
        public override void SetItems(IEnumerable<TData> items)
        {
            ThingsData.Clear();
            ViewByData.Clear();
            int counter = 0;
            foreach (var item in items)
            {
                if (Views.Count == counter)
                {
                    AddItem(item);
                }
                else
                {
                    AddItem(item, Views[counter]);
                }

                counter++;
            }

            for (int i = counter; i < Views.Count; i++)
            {
                SelectionHandler.RemoveTarget(Views[i]);
                DynamicPool.Instance.Return(Views[i]);
            }

            Views.RemoveRange(counter, Views.Count - counter);
        }

        public void Clear()
        {
            for (int i = 0; i < Views.Count; i++)
            {
                SelectionHandler.RemoveTarget(Views[i]);
                DynamicPool.Instance.Return(Views[i]);
            }
            ThingsData.Clear();
            Views.Clear();
        }

        public override void AddItem(TData data)
        {
            var view = DynamicPool.Instance.Get(_thingViewPrefab, itemsContainer);
            SelectionHandler.AddTarget(view);
            Views.Add(view);
            AddItem(data, view);
        }

        private void AddItem(TData data, TView view)
        {
            view.SetData(data);
            ViewByData[data] = view;
            InitItem(view);
            view.SetDragCallbacks(this);
            view.SetContainer(this);
            ThingsData.Add(data);
        }

        public override void RemoveItem(TData data)
        {
            var index = ThingsData.FindIndex(x => ReferenceEquals(x, data));
            var view = ViewByData[data];
            ViewByData.Remove(data);
            SelectionHandler.RemoveTarget(view);
            DynamicPool.Instance.Return(view);
            ThingsData.RemoveAt(index);
            Views.RemoveAt(index);
        }

        public override void RefreshItem(TData data)
        {
            ViewByData[data].RefreshView();
        }

        public override void Select(TData data)
        {
            ViewByData[data].EmitSelection();
        }

        protected virtual void OnDestroy()
        {
            SelectionHandler.Dispose();
        }
        
        public override void OnChildDragStart(ThingView<TData> view, Vector2 position)
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
        
        public override void OnChildDragEnd(ThingView<TData> view)
        {
            if (_dragAndDropService == null)
            {
                return;
            }
            _dragAndDropService.Drop();
        }

        public override void OnChildDragContinue(ThingView<TData> view, Vector2 delta)
        {
            if (_dragAndDropService == null)
            {
                return;
            }
            _dragPosition += delta;
            _dragAndDropService.Move(_dragPosition);
        }
    }
}