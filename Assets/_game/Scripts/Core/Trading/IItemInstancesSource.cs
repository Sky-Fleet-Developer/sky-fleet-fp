using System.Collections;
using System.Collections.Generic;
using Core.Items;

namespace Core.Trading
{
    public interface IItemInstancesSource : IPullPutItem
    {
        IEnumerable<ItemInstance> EnumerateItems();
        void AddListener(IInventoryStateListener listener);
        void RemoveListener(IInventoryStateListener listener);
    }
}