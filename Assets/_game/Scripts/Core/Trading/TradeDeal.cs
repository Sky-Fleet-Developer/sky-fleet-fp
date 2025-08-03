using System;
using System.Collections.Generic;

namespace Core.Trading
{
    public class TradeDeal : IDisposable
    {
        private List<TradeItem> _itemsToPurchase;
        private List<TradeItem> _itemsToSell;
        private ITradeParticipant _seller;
        private ITradeParticipant _purchaser;

        public TradeDeal(ITradeParticipant purchaser, ITradeParticipant seller)
        {
            _purchaser = purchaser;
            _seller = seller;
            _itemsToPurchase = new();
            _itemsToSell = new();
        }

        public void AddToPurchase(ItemSign item, int amount)
        {
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                if (_itemsToPurchase[i].sign == item)
                {
                    _itemsToPurchase[i].amount += amount;
                    return;
                }
            }

            var tradeItem = new TradeItem();
            tradeItem.sign = item;
            tradeItem.cost = _seller.GetCost(item);
            tradeItem.amount = amount;
            _itemsToPurchase.Add(tradeItem);            
        }

        public void AddToSell(ItemSign item, int amount)
        {
            for (var i = 0; i < _itemsToSell.Count; i++)
            {
                if (_itemsToSell[i].sign == item)
                {
                    _itemsToSell[i].amount += amount;
                    return;
                }
            }

            var tradeItem = new TradeItem();
            tradeItem.sign = item;
            tradeItem.cost = _seller.GetCost(item);
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

        public void Accept()
        {
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