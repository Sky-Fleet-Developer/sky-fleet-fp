using System.Collections.Generic;
using Core.Items;
using Zenject;

namespace Core.Trading
{
    public interface IInventoryOwner
    {
        string InventoryKey { get; }

        bool IsOwnerOf(ItemInstance item)
        {
            return item.GetOwnership() == InventoryKey;
        }
    }
}