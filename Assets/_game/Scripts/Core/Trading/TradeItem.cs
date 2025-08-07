
using System;
using UnityEngine;

namespace Core.Trading
{
    [Serializable]
    public class TradeItem : IEquatable<TradeItem>, IDisposable
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

        public bool Equals(TradeItem other)
        {
            if (other == null) return false;
            if (!sign.Equals(other.sign)) return false;
            if (ReferenceEquals(this, other)) return true;
            return cost == other.cost;
        }

        public void Dispose()
        {
            sign = null;
        }
    }
}