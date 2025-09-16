using System.Collections.Generic;
using Core.Utilities;
using UnityEngine;

namespace Core.UIStructure.Utilities
{
    public abstract class ThingsListView<TData, TView> : MonoBehaviour where TView : ThingView<TData>
    {
        [SerializeField] private Transform itemsContainer;
        protected List<TView> _views = new ();
        private TView _thingViewPrefab;
        public readonly MultipleSelectionHandler<TView> SelectionHandler = new ();
        protected List<TData> _thingsData = new();
        
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
                    _views.Add(DynamicPool.Instance.Get(_thingViewPrefab, itemsContainer));
                    SelectionHandler.AddTarget(_views[counter]);
                    InitItem(_views[counter]);
                }
                _views[counter++].SetData(item);
                _thingsData.Add(item);
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
            var instance = DynamicPool.Instance.Get(_thingViewPrefab, itemsContainer);
            instance.SetData(data);
            _views.Add(instance);
            _thingsData.Add(data);
            SelectionHandler.AddTarget(instance);
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
    }
}