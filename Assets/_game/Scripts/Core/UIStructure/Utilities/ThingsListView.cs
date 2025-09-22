﻿using System;
using System.Collections.Generic;
using Core.Utilities;
using UnityEngine;
using Zenject;

namespace Core.UIStructure.Utilities
{
    public abstract class ThingsListView<TData, TView> : MonoBehaviour, IDragAndDropContainer, IDragCallbacks<ThingView<TData>> where TView : ThingView<TData>
    {
        [Inject] private DragAndDropService _dragAndDropService;
        [SerializeField] private Transform itemsContainer;
        protected List<TView> _views = new ();
        private TView _thingViewPrefab;
        public readonly MultipleSelectionHandler<TView> SelectionHandler = new ();
        protected List<TData> _thingsData = new();
        private Vector2 _dragPosition;
        private bool _isDragNow;
        public Action<DropEventData> OnDropContentEvent;

        protected virtual void Awake()
        {
            _thingViewPrefab = itemsContainer.GetComponentInChildren<TView>();
            DynamicPool.Instance.Return(_thingViewPrefab);
        }

        protected virtual void InitItem(TView item){}
        public void SetItems(IEnumerable<TData> items)
        {
            _thingsData.Clear();
            int counter = 0;
            foreach (var item in items)
            {
                if (_views.Count == counter)
                {
                    AddItem(item);
                }
                else
                {
                    AddItem(item, _views[counter]);
                }

                counter++;
            }

            for (int i = counter; i < _views.Count; i++)
            {
                SelectionHandler.RemoveTarget(_views[i]);
                DynamicPool.Instance.Return(_views[i]);
            }

            _views.RemoveRange(counter, _views.Count - counter);
        }

        public void Clear()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                SelectionHandler.RemoveTarget(_views[i]);
                DynamicPool.Instance.Return(_views[i]);
            }
            _thingsData.Clear();
            _views.Clear();
        }

        public virtual void AddItem(TData data)
        {
            var view = DynamicPool.Instance.Get(_thingViewPrefab, itemsContainer);
            _views.Add(view);
            AddItem(data, view);
        }

        private void AddItem(TData data, TView view)
        {
            view.SetData(data);
            InitItem(view);
            view.SetDragCallbacks(this);
            view.SetContainer(this);
            _thingsData.Add(data);
            SelectionHandler.AddTarget(view);
        }

        public virtual void RemoveItem(TData data)
        {
            var index = _thingsData.FindIndex(x => ReferenceEquals(x, data));
            SelectionHandler.RemoveTarget(_views[index]);
            DynamicPool.Instance.Return(_views[index]);
            _thingsData.RemoveAt(index);
            _views.RemoveAt(index);
        }

        public virtual void RefreshItem(TData data)
        {
            var index = _thingsData.FindIndex(x => ReferenceEquals(x, data));
            _views[index].RefreshView();
        }

        public virtual void Select(TData data)
        {
            var index = _thingsData.FindIndex(x => ReferenceEquals(x, data));
            _views[index].EmitSelection();
        }

        protected virtual void OnDestroy()
        {
            SelectionHandler.Dispose();
        }

        public void OnChildDragStart(ThingView<TData> view, Vector2 position)
        {
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

        public void OnChildDragEnd(ThingView<TData> view)
        {
            _dragAndDropService.Drop();
        }

        public void OnChildDragContinue(ThingView<TData> view, Vector2 delta)
        {
            _dragPosition += delta;
            _dragAndDropService.Move(_dragPosition);
        }

        void IDragAndDropContainer.OnDropContent(DropEventData eventData)
        {
            OnDropContentEvent?.Invoke(eventData);
        }
    }
}