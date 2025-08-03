using System;
using System.Collections.Generic;
using Core.Trading;
using Core.Utilities;
using UnityEngine;

namespace Runtime.Trading.Ui
{
    public class TradeItemsListView : MonoBehaviour
    {
        [SerializeField] private Transform itemsContainer;
        private List<TradeItemView> views = new ();
        private TradeItemView _itemPrefab;

        private void Awake()
        {
            _itemPrefab = itemsContainer.GetComponentInChildren<TradeItemView>();
            DynamicPool.Instance.Return(_itemPrefab);
        }

        public void SetItems(IEnumerable<TradeItem> items)
        {
            int counter = 0;
            foreach (var item in items)
            {
                if (views.Count == counter)
                {
                    views.Add(DynamicPool.Instance.Get(_itemPrefab, itemsContainer));
                }
                views[counter++].SetData(item);
            }

            for (int i = counter; i < views.Count; i++)
            {
                DynamicPool.Instance.Return(views[i]);
            }

            views.RemoveRange(counter, views.Count - counter);
        }
    }
}