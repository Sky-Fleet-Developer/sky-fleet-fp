using System;
using System.Collections.Generic;
using Core.Items;

namespace Core.Trading
{
    public interface IPullPutItem
    {
        bool CanPutAnyItem { get => true; }
        bool CanPullAnyItem { get => true; }
        bool TryPutItem(ItemInstance item);
        bool TryPullItem(ItemInstance item, float amount, out ItemInstance result);
    }
    
    public interface IItemsContainerMasterHandler : IItemsContainerReadonly, IPullPutItem
    {
        void Dispose();
    }
    
    public interface IItemsContainerReadonly : IItemInstancesSource
    {
        string Key { get; }
        IEnumerable<ItemInstance> IItemInstancesSource.EnumerateItems() => GetItems();
        IEnumerable<ItemInstance> GetItems();
        bool IsEmpty { get; }
        //IEnumerable<ItemInstance> GetItems(string id);
    }
}