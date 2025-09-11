
using System;
using System.Collections.Generic;
using Core.Items;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Trading
{
    [Serializable]
    public class TradeItem : IEquatable<TradeItem>, IDisposable
    {
        public float amount;
        public ItemSign sign;
        public int cost;
        public bool IsConstantMass => sign.HasTag(ItemSign.MassTag);
        public TradeItem(){}

        public TradeItem(ItemSign itemSign)
        {
            sign = itemSign;
        }
        public TradeItem(ItemSign itemSign, float amount, int cost)
        {
            sign = itemSign;
            this.amount = IsConstantMass ? Mathf.CeilToInt(amount) : amount;
            this.cost = cost;
        }

        public float GetVolume()
        {
            return sign.GetSingleVolume() * amount;
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

    public static class TradeItemExtension
    {
        public static IEnumerable<ItemInstance> MakeInstances(this TradeItem tradeItem)
        {
            if (!tradeItem.sign.HasTag(ItemSign.LiquidTag))
            {
                float stackSize = tradeItem.sign.GetStackSize();
                int loops = (int)(tradeItem.amount / stackSize);
                for (int i = 0; i < loops; i++)
                {
                    yield return new ItemInstance(tradeItem.sign, stackSize);
                }
                yield return new ItemInstance(tradeItem.sign, tradeItem.amount - loops * stackSize);
            }
            else
            {
                yield return new ItemInstance(tradeItem.sign, tradeItem.amount);
            }
        }
    }
}