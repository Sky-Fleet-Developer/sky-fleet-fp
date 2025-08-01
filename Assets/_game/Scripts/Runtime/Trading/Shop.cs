using System;
using System.Collections.Generic;
using Core.Character.Interaction;
using Core.Trading;
using UnityEngine;

namespace Runtime.Trading
{
    [Serializable]
    public class Shop : ITradeHandler
    {
        [SerializeField] private string shopId;
        
        private TradeItem[] _assortment;
        public IEnumerable<TradeItem> GetItems() => _assortment;

        public event Action ItemsChanged;
        public bool TryMakeDeal(TradeDeal deal, out Transaction transaction)
        {
            transaction = null;
            return true;
        }
    }
}