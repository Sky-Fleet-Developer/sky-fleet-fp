using System;
using System.Collections.Generic;
using System.Linq;
using Core.Trading;
using Core.UIStructure.Utilities;
using Core.Utilities;
using UnityEngine;

namespace Runtime.Trading.UI
{
    public class TradeItemsListView : MonoBehaviour
    {
        [SerializeField] private Transform itemsContainer;
        private List<TradeItemView> _views = new ();
        private TradeItemView _itemPrefab;
        public readonly ListSelectionHandler<TradeItemView> SelectionHandler = new ();
        private List<TradeItem> _items = new();
        public event Action<TradeItem, float> OnItemInCardAmountChanged; 
        
        private void Awake()
        {
            _itemPrefab = itemsContainer.GetComponentInChildren<TradeItemView>();
            DynamicPool.Instance.Return(_itemPrefab);
        }

        public void SetItems(IEnumerable<TradeItem> items)
        {
            _items.Clear();
            int counter = 0;
            foreach (var item in items)
            {
                if (_views.Count == counter)
                {
                    _views.Add(DynamicPool.Instance.Get(_itemPrefab, itemsContainer));
                    SelectionHandler.AddTarget(_views[counter]);
                    _views[counter].SetInCardAmountChangedCallback(ItemInCardAmountChanged);
                }
                _views[counter++].SetData(item);
                _items.Add(item);
            }

            for (int i = counter; i < _views.Count; i++)
            {
                SelectionHandler.RemoveTarget(_views[i]);
                DynamicPool.Instance.Return(_views[i]);
            }

            _views.RemoveRange(counter, _views.Count - counter);
        }

        private void ItemInCardAmountChanged(TradeItem item, float amount)
        {
            OnItemInCardAmountChanged?.Invoke(item, amount);
        }

        public void Clear()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                SelectionHandler.RemoveTarget(_views[i]);
                DynamicPool.Instance.Return(_views[i]);
            }
            _items.Clear();
            _views.Clear();
        }

        public void AddItem(TradeItem item)
        {
            var index = _items.FindIndex(x => x.sign.Id == item.sign.Id);
            if (index != -1)
            {
                _items[index].amount = item.amount;
                _views[index].RefreshView();
                return;
            }
            var instance = DynamicPool.Instance.Get(_itemPrefab, itemsContainer);
            instance.SetData(item);
            _views.Add(instance);
            _items.Add(item);
            SelectionHandler.AddTarget(instance);
        }
        
        public void RemoveItem(TradeItem item)
        {
            var index = _items.FindIndex(x => x == item);
            SelectionHandler.RemoveTarget(_views[index]);
            DynamicPool.Instance.Return(_views[index]);
            _items.RemoveAt(index);
            _views.RemoveAt(index);
        }

        public void RefreshItem(TradeItem item)
        {
            var view = _views.First(x => x.Data.sign.Id == item.sign.Id);
            view.RefreshView();
        }


        public void Select(TradeItem item)
        {
            var view = _views.First(x => x.Data.sign.Id == item.sign.Id);
            view.OnSelect(null);
        }

        private void OnDestroy()
        {
            SelectionHandler.Dispose();
        }
    }
}