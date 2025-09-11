using System;
using Core.Items;

namespace Core.Trading
{
    public interface IProductDeliveryService : IComparable<IProductDeliveryService>
    {
        int Order { get; }
        bool TryDeliver(ItemInstance item, ProductDeliverySettings deliverySettings, out DeliveredProductInfo deliveredProductInfo);
        int IComparable<IProductDeliveryService>.CompareTo(IProductDeliveryService other)
        {
            return Order.CompareTo(other.Order);
        }
    }
}