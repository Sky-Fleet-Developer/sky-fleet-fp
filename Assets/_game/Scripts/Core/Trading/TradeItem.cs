
using System;
using UnityEngine;

namespace Core.Trading
{
    [Serializable]
    public class TradeItem : IDisposable
    {
        public int amount;
        public ItemSign sign;
        public int cost;
        public TradeItem(){}

        public TradeItem(ItemSign itemSign)
        {
            sign = itemSign;
        }
        public TradeItem(ItemSign itemSign, int amount, int cost)
        {
            sign = itemSign;
            this.amount = amount;
            this.cost = cost;
        }


        public void Dispose()
        {
            sign = null;
        }
    }
}