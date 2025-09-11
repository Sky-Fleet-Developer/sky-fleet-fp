
using System;
using System.Collections.Generic;
using Core.Items;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Trading
{
    public class TradeItem : IEquatable<TradeItem>, IDisposable
    {
        public float amount;
        [SerializeField] private ItemSign _sign;
        public ItemSign Sign => _sign;
        public int cost;
        public bool IsConstantMass => Sign.HasTag(ItemSign.MassTag);

        public TradeItem(ItemSign itemSign, float amount, int cost)
        {
            _sign = itemSign;
            this.amount = IsConstantMass ? Mathf.CeilToInt(amount) : amount;
            this.cost = cost;
        }

        public float GetVolume()
        {
            return Sign.GetSingleVolume() * amount;
        }
        
        public bool Equals(TradeItem other)
        {
            if (other == null) return false;
            if (!Sign.Equals(other.Sign)) return false;
            if (ReferenceEquals(this, other)) return true;
            return cost == other.cost;
        }

        public void Dispose()
        {
            _sign = null;
        }
    }

    public static class ItemExtension
    {
        public static IEnumerable<ItemInstance> MakeInstances(this TradeItem tradeItem, float maxStackSize)
        {
            if (!tradeItem.Sign.HasTag(ItemSign.LiquidTag))
            {
                int loops = (int)(tradeItem.amount / maxStackSize);
                for (int i = 0; i < loops; i++)
                {
                    yield return new ItemInstance(tradeItem.Sign, maxStackSize);
                }
                yield return new ItemInstance(tradeItem.Sign, tradeItem.amount - loops * maxStackSize);
            }
            else
            {
                yield return new ItemInstance(tradeItem.Sign, tradeItem.amount);
            }
        }
        public static IEnumerable<ItemInstance> DetachStacks(this ItemInstance tradeItem, float stackSize)
        {
            if (!tradeItem.Sign.HasTag(ItemSign.LiquidTag))
            {
                while (tradeItem.Amount > stackSize)
                {
                    yield return tradeItem.Detach(stackSize);
                }
                yield return tradeItem;
            }
            else
            {
                yield return tradeItem;
            }
        }
    }
}