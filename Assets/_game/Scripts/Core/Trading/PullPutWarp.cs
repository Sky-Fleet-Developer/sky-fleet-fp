using System.Collections.Generic;
using Core.Items;

namespace Core.Trading
{
    public partial class BankSystem
    {
        private class PullPutWarp : IItemInstancesSource
        {
            private IItemsContainerReadonly _inventory;
            private BankSystem _bankSystem;

            public PullPutWarp(IItemsContainerReadonly inventory, BankSystem bankSystem)
            {
                _bankSystem = bankSystem;
                _inventory = inventory;
            }
            public IEnumerable<ItemInstance> GetItems()
            {
                return _inventory.GetItems();
            }

            public void AddListener(IInventoryStateListener listener)
            {
                _inventory.AddListener(listener);
            }

            public void RemoveListener(IInventoryStateListener listener)
            {
                _inventory.RemoveListener(listener);
            }

            public bool TryPutItem(ItemInstance item)
            {
                return _bankSystem.TryPutItem(_inventory.Key, item);
            }

            public bool TryPullItem(ItemInstance item, float amount, out ItemInstance result)
            {
                return _bankSystem.TryPullItem(_inventory.Key, item, amount, out result);
            }
        }

    }
}