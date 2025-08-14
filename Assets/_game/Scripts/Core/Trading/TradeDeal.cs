using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Trading
{
    public class TradeDeal : IDisposable
    {
        private List<TradeItem> _itemsToPurchase;
        private List<TradeItem> _itemsToSell;
        private ITradeParticipant _seller;
        private ITradeParticipant _purchaser;

        public IEnumerable<TradeItem> GetPurchases() => _itemsToPurchase;
        public IEnumerable<TradeItem> GetSells() => _itemsToSell;
        public ITradeParticipant GetPurchaser() => _purchaser;
        public ITradeParticipant GetSeller() => _seller;
        
        public TradeDeal(ITradeParticipant purchaser, ITradeParticipant seller)
        {
            _purchaser = purchaser;
            _seller = seller;
            _itemsToPurchase = new();
            _itemsToSell = new();
        }

        public bool SetInCartItemAmount(TradeItem item, int amount, out TradeItem innerItem)
        {
            if (amount > item.amount)
            {
                innerItem = null;
                return false;
            }
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                if (_itemsToPurchase[i].sign.Equals(item.sign))
                {
                    _itemsToPurchase[i].amount = amount;
                    innerItem = _itemsToPurchase[i];
                    return true;
                }
            }

            var tradeItem = new TradeItem();
            tradeItem.sign = item.sign;
            tradeItem.cost = item.cost;
            tradeItem.amount = amount;
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
                if (_itemsToSell[i].sign == item.sign)
                {
                    _itemsToSell[i].amount += amount;
                    return;
                }
            }

            var tradeItem = new TradeItem();
            tradeItem.sign = item.sign;
            tradeItem.cost = item.cost;
            tradeItem.amount = amount;
            _itemsToSell.Add(tradeItem);            
        }

        public int GetPaymentAmount()
        {
            int counter = 0;
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                counter += _itemsToPurchase[i].cost * _itemsToPurchase[i].amount;
            }
            for (var i = 0; i < _itemsToSell.Count; i++)
            {
                counter -= _itemsToSell[i].cost * _itemsToSell[i].amount;
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