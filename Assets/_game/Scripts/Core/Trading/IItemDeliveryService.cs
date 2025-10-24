using System;
using System.Collections.Generic;
using Core.Items;

namespace Core.Trading
{
    public interface IItemDeliveryService : IDescriptionView, IComparable<IItemDeliveryService>
    {
        int Order { get; } 
        void Deliver(ItemInstance item, IInventoryOwner destination);
        bool IsCanDeliver(ItemSign item, IInventoryOwner destination);
        int IComparable<IItemDeliveryService>.CompareTo(IItemDeliveryService other)
        {
            return Order.CompareTo(other.Order);
        }
    }
}