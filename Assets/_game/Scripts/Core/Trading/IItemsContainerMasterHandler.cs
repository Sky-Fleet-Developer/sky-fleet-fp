using System;
using System.Collections.Generic;
using Core.Items;

namespace Core.Trading
{
    public enum PutItemResult { Fail = 0, Partly = 1, Fully = 2 }
    public interface IPullPutItem
    {
        bool CanPutAnyItem { get => true; }
        bool CanPullAnyItem { get => true; }
        
        /// <summary>
        /// Try to put item to container
        /// </summary>
        /// <returns>TRUE if item FULLY plugged in and CANT BE USED anymore, FALSE if any part of item was not used</returns>
        PutItemResult TryPutItem(ItemInstance item);
        bool TryPullItem(ItemInstance item, float amount, out ItemInstance result);
    }
    
    public interface IItemsContainerMasterHandler : IItemsContainerReadonly, IItemInstancesSource
    {
        void Dispose();
    }
    
    public interface IItemsContainerReadonly : IItemInstancesSourceReadonly
    {
        string Key { get; }
        bool IsEmpty { get; }
        //IEnumerable<ItemInstance> GetItems(string id);
    }
}