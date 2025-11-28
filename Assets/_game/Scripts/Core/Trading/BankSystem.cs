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
        [Inject] private IShopDataSource _shopDataSource;
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

        public void UnbindInventoryToContainerSettings(string inventoryKey) => _containerBindings.Remove(inventoryKey);

        public IItemInstancesSource GetPullPutWarp(string inventoryKey) => new PullPutWarp(GetOrCreateInventory(inventoryKey), this);

        public void DissolveEmptyInventory(string inventoryKey)
        {
            if (_inventories.TryGetValue(inventoryKey, out IItemsContainerMasterHandler inventory) && inventory.IsEmpty)
            {
                _inventories.Remove(inventoryKey);
                inventory.Dispose();
            }
        }
        
        public void InitializeShop(string shopId, string inventoryKey)
        {
            if (_inventories.ContainsKey(inventoryKey))
            {
                return;
            }
            if (_shopDataSource.TryGetSettings(shopId, out ShopSettings settings))
            {
                var inventory = GetOrCreateInventoryHandler(inventoryKey);
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
       
        public PutItemResult TryPutItem(string key, ItemInstance item)
        {
            var handler = GetOrCreateInventoryHandler(key);
            float volume = _massAndVolumeCalculator.GetVolume(handler);
            if (_containerBindings.TryGetValue(key, out var containerInfo))
            {
                if (!containerInfo.IsItemMatch(item, volume))
                {
                    return PutItemResult.Fail;
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
            int deliveredItemsCost = 0;
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
                            int cost = Mathf.FloorToInt(tradeItem.Cost * tradeItem.amount.Value + 0.5f);
                            var deliverResult = tradeItem.GetDeliveryService().Deliver(result, purchaser);
                            pulledItems.RemoveAt(pulledItems.Count - 1);
                            if (deliverResult == PutItemResult.Fully)
                            {
                                deliveredItemsCost += cost;
                                continue;
                            }
                            if (TryPutItem(seller.InventoryKey, result) != PutItemResult.Fully)
                            {
                                Debug.LogError($"Can't put item back: {result.Sign.Id} ({result.Amount})");
                            }
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
                    if (TryPutItem(seller.InventoryKey, item) == PutItemResult.Fail)
                    {
                        Debug.LogError("Can't put item back");
                    }
                }

                return false;
            }
            PutCurrencyToWallet(seller, deliveredItemsCost);
            int change = paymentAmount - deliveredItemsCost;
            if (change > 0)
            {
                PutCurrencyToWallet(purchaser, change);
            }

            return true;
        }

        public bool TryMergeItems(ItemInstance disposable, ItemInstance destination)
        {
            if (!disposable.Sign.Equals(destination.Sign))
            {
                return false;
            }
            
            if (destination.IsContainer) // Merge containers is complex. It both should be empty before merge.
            {
                if (!TryPrepareContainersForMerge(disposable, destination))
                {
                    return false;
                }
            }

            destination.Merge(disposable);
            return true;
        }

        private bool TryPrepareContainersForMerge(ItemInstance disposable, ItemInstance destination)
        {
            bool canMergeA = true;
            bool canMergeB = true;
            string inventoryToDissolve = null;
            if (disposable.TryGetProperty(ItemSign.IdentifiableTag, out var propertyA))
            {
                var key = propertyA.values[ItemProperty.IdentifiableInstance_Identifier].stringValue;
                if (_inventories.TryGetValue(key, out var inventoryA))
                {
                    if (!inventoryA.IsEmpty)
                    {
                        canMergeA = false;
                    }
                    else
                    {
                        inventoryToDissolve = inventoryA.Key;
                    }
                }
            }

            if (destination.TryGetProperty(ItemSign.IdentifiableTag, out var propertyB))
            {
                var key = propertyB.values[ItemProperty.IdentifiableInstance_Identifier].stringValue;
                if (_inventories.TryGetValue(key, out var inventoryB))
                {
                    if (!inventoryB.IsEmpty)
                    {
                        canMergeB = false;
                    }
                }
            }
                
            if (!canMergeA || !canMergeB) return false;
            if (inventoryToDissolve != null)
            {
                DissolveEmptyInventory(inventoryToDissolve);
            }

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
        
        // For testing
        internal Wallet TestCreateWallet(string key, int currency) => _wallets[key] = new Wallet(key, currency);

        internal void TestDeleteInventory(string key)
        {
            _inventories[key].Dispose();
            _inventories.Remove(key);
        }
        internal void TestDeleteWallet(string key) => _wallets.Remove(key);
        
        #if UNITY_EDITOR
        [Button]
        private void ClearWalletsCache()
        {
            _wallets.Clear();
        }
        #endif
    }
}