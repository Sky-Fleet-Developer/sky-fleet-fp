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

        public TradeDeal(ITradeParticipant purchaser)
        {
            _purchaser = purchaser;
        }

        public int GetPaymentAmount()
        {
            int counter = 0;
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                counter += _itemsToPurchase[i].Cost * _itemsToPurchase[i].Amount;
            }
            for (var i = 0; i < _itemsToSell.Count; i++)
            {
                counter -= _itemsToSell[i].Cost * _itemsToSell[i].Amount;
            }

            return counter;
        }

        public void Accept(ITradeParticipant seller)
        {
            _seller = seller;
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