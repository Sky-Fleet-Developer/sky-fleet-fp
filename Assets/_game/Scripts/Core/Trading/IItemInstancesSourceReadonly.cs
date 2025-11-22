using System.Collections.Generic;
using Core.Items;

namespace Core.Trading
{
    public interface IItemInstancesSourceReadonly
    {
        IEnumerable<ItemInstance> GetItems();
        void AddListener(IInventoryStateListener listener);
        void RemoveListener(IInventoryStateListener listener);
    }
}