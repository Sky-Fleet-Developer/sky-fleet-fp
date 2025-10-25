using System;
using System.Collections.Generic;
using Core.Configurations;
using Core.Items;
using UnityEngine;
using Zenject;

namespace Core.Trading
{
    [CreateAssetMenu(menuName = "SF/Game/BankSystem")]
    public class BankSystem : ScriptableObject
    {
        [Inject] private IFactory<string, IItemsContainerMasterHandler> _inventoryFactory;
        [Inject] private ShopTable _shopTable;
        [Inject] private ItemsTable _itemsTable;
        private readonly Dictionary<string, IItemsContainerMasterHandler> _inventories = new ();
        
        public IItemsContainerReadonly GetOrCreateInventory(IInventoryOwner owner)
        {
            return GetOrCreateInventoryHandler(owner);
        }

        private IItemsContainerMasterHandler GetOrCreateInventoryHandler(IInventoryOwner owner)
        {
            if (!_inventories.TryGetValue(owner.InventoryKey, out IItemsContainerMasterHandler inventory))
            {
                inventory = _inventoryFactory.Create(owner.InventoryKey);
                _inventories.Add(owner.InventoryKey, inventory);
            }
            return inventory;
        }

        public bool TryPullItem(IInventoryOwner inventoryOwner, ItemSign sign, float amount, out ItemInstance result)
        {
            var handler = GetOrCreateInventoryHandler(inventoryOwner);
            return handler.TryPullItem(sign, amount, out result);
        }
        
        public bool TryPutItem(IInventoryOwner inventoryOwner, ItemInstance item)
        {
            var handler = GetOrCreateInventoryHandler(inventoryOwner);
            inventoryOwner.TakeOwnership(item);
            handler.PutItem(item);
            return true;
        }

        public bool TryMakeDeal(TradeDeal deal)
        {
            List<ItemInstance> pulledItems = new();
            bool success = true;
            var seller = deal.GetSeller();
            var purchaser = deal.GetPurchaser();
            foreach (var tradeItem in deal.GetPurchases())
            {
                if (tradeItem.Item !=  null)
                {
                    try
                    {
                        if (tradeItem.amount <= 0)
                        {
                            continue;
                        }
                        var result = tradeItem.GetSource().PullItem(tradeItem);
                        if (result == null)
                        {
                            Debug.LogError("Item not found");
                        }
                        pulledItems.Add(result);
                        purchaser.TakeOwnership(result);
                        tradeItem.GetDeliveryService().Deliver(result, purchaser);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        success = false;
                    }
                }
            }

            if (!success)
            {
                foreach (ItemInstance item in pulledItems)
                {
                    seller.TakeOwnership(item);
                    TryPutItem(seller, item);
                }

                return false;
            }
            return true;
        }

        public void InitializeShop(string shopId, IInventoryOwner inventoryOwner)
        {
            if (_inventories.ContainsKey(inventoryOwner.InventoryKey))
            {
                return;
            }
            if (_shopTable.TryGetSettings(shopId, out ShopSettings settings))
            {
                var inventory = GetOrCreateInventoryHandler(inventoryOwner);
                foreach (var itemSign in _itemsTable.GetItems())
                {
                    if (settings.IsItemMatch(itemSign))
                    {
                        var item = new ItemInstance(itemSign, 100);
                        inventory.PutItem(item);
                    }
                }
            }
        }
    }
}