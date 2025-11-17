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
    public class BankSystem : ScriptableObject
    {
        [SerializeField] private WalletSource walletSource;
#if UNITY_EDITOR
        [SerializeField] private bool needSaveChanges; 
        [ShowInInspector] private IEnumerable<Wallet> CurrentWallets => _wallets.Values;
#endif
        [Inject] private IInventoryFactory _inventoryFactory;
        [Inject] private ShopTable _shopTable;
        [Inject] private ItemsTable _itemsTable;
        private readonly Dictionary<string, IItemsContainerMasterHandler> _inventories = new ();
        private readonly Dictionary<string, Wallet> _wallets = new ();
        
        [Serializable]
        private class WalletSource
        {
            [Serializable]
            private class WalletData
            {
                public string id;
                public int balance;
            }
            [SerializeField] private List<WalletData> data = new();
            
            public Wallet LoadWallet(string id)
            {
                foreach (var walletData in data)
                {
                    if (walletData.id == id)
                    {
                        return new Wallet(walletData.id, walletData.balance);
                    }
                }
                return new Wallet(id, 0);
            }

            public void SaveWallet(Wallet wallet)
            {
                foreach (var walletData in data)
                {
                    if (walletData.id == wallet.WalletKey)
                    {
                        walletData.balance = wallet.GetBalance();
                        return;
                    }
                }
                data.Add(new WalletData {id = wallet.WalletKey, balance = wallet.GetBalance()});
            }
        }

        public int GetWalletBalance(IWalletOwner owner)
        {
            return GetOrCreateWallet(owner).GetBalance();
        }
        
        public IItemsContainerReadonly GetOrCreateInventory(string key)
        {
            return GetOrCreateInventoryHandler(key);
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
                        var item = new ItemInstance(itemSign, 100);
                        inventory.PutItem(item);
                    }
                }
            }
        }
        
        public bool TryPullItem(string key, ItemSign sign, float amount, out ItemInstance result)
        {
            var handler = GetOrCreateInventoryHandler(key);
            return handler.TryPullItem(sign, amount, out result);
        }
       
        public bool TryPutItem(string key, ItemInstance item)
        {
            var handler = GetOrCreateInventoryHandler(key);
            item.SetOwnership(key);
            handler.PutItem(item);
            return true;
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
                        if (tradeItem.amount <= 0)
                        {
                            continue;
                        }
                        var result = tradeItem.GetSource().PullItem(tradeItem);
                        if (result == null)
                        {
                            Debug.LogError("Item not found");
                        }
                        else
                        {
                            pulledItems.Add(result);
                            result.SetOwnership(purchaser.InventoryKey);
                            tradeItem.GetDeliveryService().Deliver(result, purchaser);
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
                    TryPutItem(seller.InventoryKey, item);
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