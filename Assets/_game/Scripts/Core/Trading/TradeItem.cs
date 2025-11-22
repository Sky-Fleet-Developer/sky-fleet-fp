
using System;
using System.Collections.Generic;
using Core.Items;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Trading
{
    public class TradeItem : IEquatable<TradeItem>, IDisposable
    {
        public ReactiveProperty<float> amount = new();
        private ItemInstance _item;
        private IItemDeliveryService _deliveryService;
        private ITradeItemsSource _source;
        private int _cost;
        public ItemSign Sign => _item.Sign;
        public ItemInstance Item => _item;
        public int Cost => _cost;
        public IItemDeliveryService GetDeliveryService() => _deliveryService;
        public ITradeItemsSource GetSource() => _source;
        public bool IsConstantMass => Sign.HasTag(ItemSign.MassTag);

        public TradeItem(ItemInstance itemInstance, float amount, int cost)
        {
            _item = itemInstance;
            this.amount.Value = IsConstantMass ? Mathf.CeilToInt(amount) : amount;
            this._cost = cost;
        }

        public TradeItem(ItemInstance itemInstance, int cost)
        {
            _item = itemInstance;
            amount.Value = itemInstance.Amount;
            this._cost = cost;
        }

        public void SetDeliveryService(IItemDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }
        public void SetSource(ITradeItemsSource source) => _source = source;

        public float GetVolume()
        {
            return Sign.GetSingleVolume() * amount.Value;
        }
        
        public bool Equals(TradeItem other)
        {
            if (other == null) return false;
            if (!Sign.Equals(other.Sign)) return false;
            if (ReferenceEquals(this, other)) return true;
            if(_item.Equals(other._item)) return true;
            return _cost == other._cost;
        }

        public void Dispose()
        {
            _item = null;
        }
    }

    public static class ItemExtension
    {
        /*public static IEnumerable<ItemInstance> MakeInstances(this TradeItem tradeItem, float maxStackSize)
        {
            if (!tradeItem.Sign.HasTag(ItemSign.LiquidTag))
            {
                int loops = (int)(tradeItem.amount.Value / maxStackSize);
                for (int i = 0; i < loops; i++)
                {
                    yield return new ItemInstance(tradeItem.Sign, maxStackSize);
                }
                yield return new ItemInstance(tradeItem.Sign, tradeItem.amount.Value - loops * maxStackSize);
            }
            else
            {
                yield return new ItemInstance(tradeItem.Sign, tradeItem.amount.Value);
            }
        }*/
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