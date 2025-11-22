using System;
using System.Collections.Generic;
using System.Linq;
using Core.Character.Stuff;
using Core.Configurations;
using Core.Items;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Core.Trading
{
    [CreateAssetMenu(menuName = "SF/Game/BankSystem")]
    public partial class BankSystem : ScriptableObject
    {
        [SerializeField] private WalletSource walletSource;
#if UNITY_EDITOR
        [SerializeField] private bool needSaveChanges; 
        [ShowInInspector] private IEnumerable<Wallet> CurrentWallets => _wallets.Values;
#endif
        [Inject] private IInventoryFactory _inventoryFactory;
        [Inject] private ShopTable _shopTable;
        [Inject] private ItemsTable _itemsTable;
        [Inject] private IItemInstanceFactory _itemInstanceFactory;
        [Inject] private IMassAndVolumeCalculator _massAndVolumeCalculator;
        private readonly Dictionary<string, IItemsContainerMasterHandler> _inventories = new ();
        private readonly Dictionary<string, Wallet> _wallets = new ();
        private readonly Dictionary<string, ContainerInfo> _containerBindings = new ();

        public int GetWalletBalance(IWalletOwner owner)
        {
            return GetOrCreateWallet(owner).GetBalance();
        }
        
        public IItemsContainerReadonly GetOrCreateInventory(string key)
        {
            return GetOrCreateInventoryHandler(key);
        }
        
        public void BindInventoryToContainerSettings(string inventoryKey, string containerId) => _containerBindings[inventoryKey] = _itemsTable.GetContainer(containerId);

        public IItemInstancesSource GetPullPutWarp(string inventoryKey) => new PullPutWarp(GetOrCreateInventory(inventoryKey), this);

        public void DissolveEmptyInventory(string inventoryKey)
        {
            if (_inventories.TryGetValue(inventoryKey, out IItemsContainerMasterHandler inventory) && inventory.IsEmpty)
            {
                _inventories.Remove(inventoryKey);
                inventory.Dispose();
            }
        }
        
        public void InitializeShop(string shopId, IInventoryOwner inventoryOwner)
        {
            if (_inventories.ContainsKey(inventoryOwner.InventoryKey))
            {
                return;
            }
            if (_shopTable.TryGetSettings(shopId, out ShopSettings settings))
            {
                var inventory = GetOrCreateInventoryHandler(inventoryOwner.InventoryKey);
                foreach (var itemSign in _itemsTable.GetItems())
                {
                    if (settings.IsItemMatch(itemSign))
                    {
                        var item = _itemInstanceFactory.Create(itemSign, 100);
                        inventory.TryPutItem(item);
                    }
                }
            }
        }
        
        public bool TryPullItem(string key, ItemInstance item, float amount, out ItemInstance result)
        {
            var handler = GetOrCreateInventoryHandler(key);
            return handler.TryPullItem(item, amount, out result);
        }
       
        public bool TryPutItem(string key, ItemInstance item)
        {
            var handler = GetOrCreateInventoryHandler(key);
            float volume = _massAndVolumeCalculator.GetVolume(handler);
            if (_containerBindings.TryGetValue(key, out var containerInfo))
            {
                if (!containerInfo.IsItemMatch(item, volume))
                {
                    return false;
                }
            }
            item.SetOwnership(key);
            return handler.TryPutItem(item);
        }

        public bool TryMakeDeal(TradeDeal deal)
        {
            List<ItemInstance> pulledItems = new();
            bool success = true;
            var seller = deal.GetSeller();
            var purchaser = deal.GetPurchaser();
            int paymentAmount = deal.GetPaymentAmount();
            if(!TryTakeCurrencyFromWallet(purchaser, paymentAmount)) return false;
            foreach (var tradeItem in deal.GetPurchases())
            {
                if (tradeItem.Item !=  null)
                {
                    try
                    {
                        if (tradeItem.amount.Value <= 0)
                        {
                            continue;
                        }
                        if (tradeItem.GetSource().TryPullItem(tradeItem, out var result))
                        {
                            pulledItems.Add(result);
                            result.SetOwnership(purchaser.InventoryKey);
                            tradeItem.GetDeliveryService().Deliver(result, purchaser);
                        }
                        else
                        {
                            Debug.LogError("Item not found");
                        }
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
                PutCurrencyToWallet(purchaser, paymentAmount);
                foreach (ItemInstance item in pulledItems)
                {
                    item.SetOwnership(seller.InventoryKey);
                    if (!TryPutItem(seller.InventoryKey, item))
                    {
                        Debug.LogError("Can't put item back");
                    }
                }

                return false;
            }
            PutCurrencyToWallet(seller, paymentAmount);
            return true;
        }
        
        private IItemsContainerMasterHandler GetOrCreateInventoryHandler(string key)
        {
            if (!_inventories.TryGetValue(key, out IItemsContainerMasterHandler inventory))
            {
                inventory = _inventoryFactory.CreateInventory(key);
                _inventories.Add(key, inventory);
            }
            return inventory;
        }
        
        private Wallet GetOrCreateWallet(IWalletOwner owner)
        {
            if (!_wallets.TryGetValue(owner.WalletKey, out Wallet wallet))
            {
                wallet = walletSource.LoadWallet(owner.WalletKey);
                _wallets.Add(owner.WalletKey, wallet);
            }
            return wallet;
        }

        private bool TryTakeCurrencyFromWallet(IWalletOwner owner, int amount)
        {
            var wallet = GetOrCreateWallet(owner);
            bool success = wallet.TryTakeCurrency(amount);
            if (success)
            {
#if UNITY_EDITOR
                if (!needSaveChanges) return true;
#endif
                walletSource.SaveWallet(wallet);
            }
            return success;
        }

        private void PutCurrencyToWallet(IWalletOwner owner, int amount)
        {
            var wallet = GetOrCreateWallet(owner);
            wallet.PutCurrency(amount);
#if UNITY_EDITOR
            if (!needSaveChanges) return;
#endif
            walletSource.SaveWallet(wallet);
        }
        
        #if UNITY_EDITOR
        [Button]
        private void ClearWalletsCache()
        {
            _wallets.Clear();
        }
        #endif
    }
}