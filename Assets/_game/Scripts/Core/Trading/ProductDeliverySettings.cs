using System.Collections.Generic;

namespace Core.Trading
{
    public struct ProductDeliverySettings
    {
        public IInventoryOwner Destination;
        public IReadOnlyList<IItemDeliveryService> Services;

        public ProductDeliverySettings(IInventoryOwner destination, IReadOnlyList<IItemDeliveryService> services) : this()
        {
            Destination = destination;
            Services = services;
        }

        public bool IsNull => Destination == null;
    }
}