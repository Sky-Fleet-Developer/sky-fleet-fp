using System;
using System.Collections.Generic;
using System.Linq;
using Core.Trading;
using Core.UIStructure.Utilities;
using Core.Utilities;
using UnityEngine;

namespace Runtime.Trading.Ui
{
    public class TradeItemsListView : MonoBehaviour, ISelectionListener<TradeItemView>
    {
        [SerializeField] private Transform itemsContainer;
        private List<TradeItemView> views = new ();
        private TradeItemView _itemPrefab;
        public readonly ListSelectionHandler<TradeItemView> SelectionHandler = new ();
        
        private void Awake()
        {
            _itemPrefab = itemsContainer.GetComponentInChildren<TradeItemView>();
            DynamicPool.Instance.Return(_itemPrefab);
            SelectionHandler.AddListener(this);
        }

        public void SetItems(IEnumerable<TradeItem> items)
        {
            int counter = 0;
            foreach (var item in items)
            {
                if (views.Count == counter)
                {
                    views.Add(DynamicPool.Instance.Get(_itemPrefab, itemsContainer));
                    SelectionHandler.AddTarget(views[counter]);
                }
                views[counter++].SetData(item);
            }

            for (int i = counter; i < views.Count; i++)
            {
                SelectionHandler.RemoveTarget(views[i]);
                DynamicPool.Instance.Return(views[i]);
            }

            views.RemoveRange(counter, views.Count - counter);
        }

        public void AddItem(TradeItem item)
        {
            var instance = DynamicPool.Instance.Get(_itemPrefab, itemsContainer);
            instance.SetData(item);
            views.Add(instance);
            SelectionHandler.AddTarget(instance);
        }
        public void RemoveItem(TradeItem item)
        {
            var index = views.FindIndex(x => x.Data.sign.Id == item.sign.Id);
            SelectionHandler.RemoveTarget(views[index]);
            DynamicPool.Instance.Return(views[index]);
        }

        public void RefreshItem(TradeItem item)
        {
            var view = views.First(x => x.Data.sign.Id == item.sign.Id);
            view.SetData(item);
        }

        public void OnSelectionChanged(TradeItemView prev, TradeItemView next)
        {
            if (prev)
            {
                prev.SetSelectionState(false);
            }

            if (next)
            {
                next.SetSelectionState(true);
            }
        }

        private void OnDestroy()
        {
            SelectionHandler.Dispose();
        }
    }
}