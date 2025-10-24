using System.Collections;
using System.Collections.Generic;
using Core.Items;

namespace Core.Trading
{
    public interface IItemInstancesSource
    {
        IEnumerable<ItemInstance> EnumerateItems();
        ItemInstance PullItem(ItemInstance item, float amount);
        void AddListener(IInventoryStateListener listener);
        void RemoveListener(IInventoryStateListener listener);
    }
}