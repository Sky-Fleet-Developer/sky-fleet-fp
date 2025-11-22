using System;
using System.Collections.Generic;
using Core.Items;
using UnityEngine;
using Zenject;

namespace Core.Trading
{
    public class TradeDeal : IDisposable
    {
        [Inject] private BankSystem _bankSystem;
        private List<TradeItem> _itemsToPurchase;
        private ITradeParticipant _seller;
        private ITradeParticipant _purchaser;
        public IEnumerable<TradeItem> GetPurchases() => _itemsToPurchase;
        public ITradeParticipant GetPurchaser() => _purchaser;
        public ITradeParticipant GetSeller() => _seller;
        
        public TradeDeal(ITradeParticipant purchaser, ITradeParticipant seller)
        {
            _purchaser = purchaser;
            _seller = seller;
            _itemsToPurchase = new();
        }

        public bool SetPurchaseItemAmount(TradeItem item, float amount)
        {
            if (amount > item.amount.Value)
            {
                return false;
            }

            if (amount == 0)
            {
                _itemsToPurchase.Remove(item);
                return true;
            }
            
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                if (_itemsToPurchase[i].Item == item.Item)
                {
                    _itemsToPurchase[i].amount.Value = amount;
                    _itemsToPurchase[i].SetDeliveryService(item.GetDeliveryService());
                    _itemsToPurchase[i].SetSource(item.GetSource());
                    return true;
                }
            }

            if (item.Item.TryGetContainerKey(out var key) && !_bankSystem.GetOrCreateInventory(key).IsEmpty)
            {
                return false;
            }
            var tradeItem = new TradeItem(item.Item, amount, item.Cost);
            tradeItem.SetDeliveryService(item.GetDeliveryService());
            tradeItem.SetSource(item.GetSource());
            _itemsToPurchase.Add(tradeItem);
            return true;
        }

        public void UpdateDeliveryService(TradeItem item)
        {
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                if (_itemsToPurchase[i].Item == item.Item)
                {
                    _itemsToPurchase[i].SetDeliveryService(item.GetDeliveryService());
                }
            }
        }

        /*public void RemoveFromCart(TradeItem item, int amountToRemove, out bool isItemCompletelyRemoved)
        {
            isItemCompletelyRemoved = false;
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                if (_itemsToPurchase[i] == item)
                {
                    if (_itemsToPurchase[i].amount > amountToRemove)
                    {
                        _itemsToPurchase[i].amount -= amountToRemove;
                        isItemCompletelyRemoved = false;
                    }
                    else
                    {
                        _itemsToPurchase.RemoveAt(i);
                        isItemCompletelyRemoved = true;
                    }
                    break;
                }
            }
        }*/

        public int GetPaymentAmount()
        {
            int counter = 0;
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                counter += Mathf.FloorToInt(_itemsToPurchase[i].Cost * _itemsToPurchase[i].amount.Value + 0.5f);
            }

            return counter;
        }

        public float GetTotalVolume()
        {
            float volume = 0;
            foreach (var tradeItem in _itemsToPurchase)
            {
                volume += tradeItem.Sign.GetSingleVolume() * tradeItem.amount.Value;
            }
            return volume;
        }


        public void Dispose()
        {
            _itemsToPurchase = null;
            _seller = null;
            _purchaser = null;
        }
    }
}