using System;
using System.Collections.Generic;
using Core.Items;

namespace Core.Trading
{
    public interface IItemsContainerMasterHandler : IItemsContainerReadonly
    {
        void PutItem(ItemInstance item);
        bool TryPullItem(ItemSign sign, float amount, out ItemInstance result);
    }
    
    public interface IItemsContainerReadonly : IItemInstancesSource
    {
        string Key { get; }
        IEnumerable<ItemInstance> IItemInstancesSource.EnumerateItems() => GetItems();
        IEnumerable<ItemInstance> GetItems();
        //IEnumerable<ItemInstance> GetItems(string id);
    }
}