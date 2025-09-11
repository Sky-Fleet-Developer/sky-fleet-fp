using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Trading
{
    public class TradeDeal : IDisposable
    {
        private List<TradeItem> _itemsToPurchase;
        private List<TradeItem> _itemsToSell;
        private IInventoryOwner _seller;
        private IInventoryOwner _purchaser;

        public IEnumerable<TradeItem> GetPurchases() => _itemsToPurchase;
        public IEnumerable<TradeItem> GetSells() => _itemsToSell;
        public IInventoryOwner GetPurchaser() => _purchaser;
        public IInventoryOwner GetSeller() => _seller;
        
        public TradeDeal(IInventoryOwner purchaser, IInventoryOwner seller)
        {
            _purchaser = purchaser;
            _seller = seller;
            _itemsToPurchase = new();
            _itemsToSell = new();
        }

        public bool SetInCartItemAmount(TradeItem item, float amount, out TradeItem innerItem)
        {
            if (amount > item.amount)
            {
                innerItem = null;
                return false;
            }
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                if (_itemsToPurchase[i].Sign.Equals(item.Sign))
                {
                    _itemsToPurchase[i].amount = amount;
                    innerItem = _itemsToPurchase[i];
                    return true;
                }
            }

            var tradeItem = new TradeItem(item.Sign, amount, item.cost);
            _itemsToPurchase.Add(tradeItem);
            innerItem = tradeItem;
            return true;
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

        public void AddToSell(TradeItem item, int amount)
        {
            for (var i = 0; i < _itemsToSell.Count; i++)
            {
                if (_itemsToSell[i].Sign.Equals(item.Sign))
                {
                    _itemsToSell[i].amount += amount;
                    return;
                }
            }

            var tradeItem = new TradeItem(item.Sign, amount, item.cost);
            _itemsToSell.Add(tradeItem);            
        }

        public int GetPaymentAmount()
        {
            int counter = 0;
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                counter += Mathf.CeilToInt(_itemsToPurchase[i].cost * _itemsToPurchase[i].amount + 0.5f);
            }
            for (var i = 0; i < _itemsToSell.Count; i++)
            {
                counter -= Mathf.FloorToInt(_itemsToSell[i].cost * _itemsToSell[i].amount);
            }

            return counter;
        }


        public void Dispose()
        {
            _itemsToPurchase = null;
            _itemsToSell = null;
            _seller = null;
            _purchaser = null;
        }
    }
}