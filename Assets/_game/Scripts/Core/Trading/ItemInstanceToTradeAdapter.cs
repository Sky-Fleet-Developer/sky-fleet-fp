using System;
using System.Collections.Generic;
using Core.Character.Interaction;
using Core.Configurations;
using Core.Items;
using UnityEngine;
using Zenject;

namespace Core.Trading
{
    public class ItemInstanceToTradeAdapter : IInventoryStateListener, ITradeItemsSource, IDisposable
    {
        [Inject] private ShopTable _shopTable;
        [Inject] private BankSystem _bankSystem;
        private TradeKind _kind;
        private IItemInstancesSource _itemsSource;
        private ShopSettings _shopSettings;
        private Dictionary<string, List<TradeItem>> _assortment = new();
        private List<ITradeItemsStateListener> _listeners = new();
        private string _sourceShop;
        private bool _initialized;

        public ItemInstanceToTradeAdapter(string sourceShop, IItemInstancesSource itemsSource, TradeKind kind)
        {
            _sourceShop = sourceShop;
            _kind = kind;
            _itemsSource = itemsSource;
            _initialized = false;
        }

        public void Initialize()
        {
            if (!_shopTable.TryGetSettings(_sourceShop, out _shopSettings))
            {
                Debug.LogError($"Shop {_sourceShop} does not exists!");
            }
            _itemsSource.AddListener(this);
            foreach (var itemInstance in _itemsSource.EnumerateItems())
            {
                ItemAdded(itemInstance);
            }
            _initialized = true;
        }
        
        public IEnumerable<TradeItem> GetTradeItems()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Adapter is not initialized!");
            }
            foreach (List<TradeItem> assortmentValue in _assortment.Values)
            {
                for (var i = 0; i < assortmentValue.Count; i++)
                {
                    yield return assortmentValue[i];
                }
            }
        }

        public bool TryPullItem(TradeItem item, out ItemInstance result)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Adapter is not initialized!");
            }
            foreach (var tradeItem in _assortment[item.Item.Sign.Id])
            {
                if (tradeItem.Item == item.Item)
                {
                    return _itemsSource.TryPullItem(item.Item, item.amount, out result);
                }
            }

            result = null;
            return false;
        }

        public void AddListener(ITradeItemsStateListener listener)
        {
            Debug.Log($"Adding listener: {listener.GetType()}");
            _listeners.Add(listener);   
        }

        public void RemoveListener(ITradeItemsStateListener listener)
        {
            _listeners.Remove(listener);   
        }
        
        private TradeItem FindTradeItem(ItemInstance item, out int inBucketIndex)
        {
            inBucketIndex = -1;
            TradeItem result = null;
            for (int i = 0; i < _assortment[item.Sign.Id].Count; i++)
            {
                TradeItem tradeItem = _assortment[item.Sign.Id][i];
                if (tradeItem.Item != null) // this is the item instance, we need to find its trade item
                {
                    if (tradeItem.Item == item) // found!
                    {
                        inBucketIndex = i;
                        result = tradeItem;
                        break;
                    }
                }
                else // this is a regular item
                {
                    inBucketIndex = i;
                    result = tradeItem;
                    break;
                }
            }

            return result;
        }
        
        public void ItemAdded(ItemInstance item)
        {
            int cost = _kind == TradeKind.Sell
                ? _shopSettings.GetSellCost(item.Sign)
                : _shopSettings.GetBuyoutCost(item);
            TradeItem tradeItem = new TradeItem(item, cost);
            tradeItem.SetSource(this);
            if (!_assortment.TryGetValue(item.Sign.Id, out List<TradeItem> list))
            {
                list = new ();
                _assortment.Add(item.Sign.Id, list);
            }
            list.Add(tradeItem);
            foreach (var listener in _listeners)
            {
                listener.ItemAdded(tradeItem, _kind);
            }
        }

        public void ItemMutated(ItemInstance item)
        {
            var tradeItem = FindTradeItem(item, out _);
            tradeItem.amount = item.Amount;
            foreach (var listener in _listeners)
            {
                listener.ItemMutated(tradeItem, _kind);
            }
        }

        public void ItemRemoved(ItemInstance item)
        {
            var tradeItem = FindTradeItem(item, out var index);
            _assortment[item.Sign.Id].RemoveAt(index);
            foreach (var listener in _listeners)
            {
                listener.ItemRemoved(tradeItem, _kind);
            }
        }

        public void Dispose()
        {
            _itemsSource.RemoveListener(this);
            _shopTable = null;
            _bankSystem = null;
            _itemsSource = null;
            _shopSettings = null;
            _listeners.Clear();
        }
    }
}