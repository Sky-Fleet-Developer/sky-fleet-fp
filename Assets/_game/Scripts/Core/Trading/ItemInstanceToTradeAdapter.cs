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
        private TradeItemKind _kind;
        private IItemInstancesSource _itemsSource;
        private ShopSettings _shopSettings;
        private Dictionary<string, List<TradeItem>> _assortment = new();
        private List<ITradeItemsStateListener> _listeners = new();
        private string _sourceShop;

        public ItemInstanceToTradeAdapter(string sourceShop, IItemInstancesSource itemsSource, TradeItemKind kind)
        {
            _sourceShop = sourceShop;
            _kind = kind;
            _itemsSource = itemsSource;
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
        }
        
        public IEnumerable<TradeItem> GetTradeItems()
        {
            foreach (List<TradeItem> assortmentValue in _assortment.Values)
            {
                for (var i = 0; i < assortmentValue.Count; i++)
                {
                    yield return assortmentValue[i];
                }
            }
        }

        public ItemInstance PullItem(TradeItem item)
        {
            foreach (var tradeItem in _assortment[item.Item.Sign.Id])
            {
                if (tradeItem.Item == item.Item)
                {
                    return _itemsSource.PullItem(item.Item, item.amount);
                }
            }

            return null;
        }

        public void AddListener(ITradeItemsStateListener listener)
        {
            _listeners.Add(listener);   
        }

        public void RemoveListener(ITradeItemsStateListener listener)
        {
            _listeners.Remove(listener);   
        }
        
        private TradeItem FindTradeItem(ItemInstance item)
        {
            TradeItem result = null;
            foreach (TradeItem tradeItem in _assortment[item.Sign.Id])
            {
                if (tradeItem.Item != null) // this is the item instance, we need to find its trade item
                {
                    if (tradeItem.Item == item) // found!
                    {
                        result = tradeItem;
                        break;
                    }
                }
                else // this is a regular item
                {
                    result = tradeItem;
                    break;
                }
            }

            return result;
        }
        
        public void ItemAdded(ItemInstance item)
        {
            int cost = _kind == TradeItemKind.Sell
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
            var tradeItem = FindTradeItem(item);
            tradeItem.amount = item.Amount;
            foreach (var listener in _listeners)
            {
                listener.ItemMutated(tradeItem, _kind);
            }
        }

        public void ItemRemoved(ItemInstance item)
        {
            var tradeItem = FindTradeItem(item);
            _assortment[item.Sign.Id].Remove(tradeItem);
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