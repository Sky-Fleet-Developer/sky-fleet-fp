using System;
using System.Collections;
using System.Collections.Generic;
using Core.Character;
using Core.Character.Interaction;
using Core.Configurations;
using Core.Items;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Trading;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

namespace Runtime.Trading
{
    
    [Serializable]
    public class Shop : Block, IInteractiveObject, ITradeHandler, ITradeItemsStateListener
    {
        [SerializeField] private string shopId;
        [SerializeField] private ItemsTrigger itemsTrigger;
        public bool EnableInteraction => IsActive;
        public Transform Root => transform;
        string IInventoryOwner.InventoryKey => shopId;
        string IWalletOwner.WalletKey => shopId;

        //public event Action ItemsChanged;
        [Inject(Optional = true)] private ShopTable _shopTable;
        [Inject(Optional = true)] private BankSystem _bankSystem;
        [Inject] private DiContainer _diContainer;
        private ItemInstanceToTradeAdapter _inventoryTradeAdapter;
        private ItemInstanceToTradeAdapter _sellZoneTradeAdapter;
        private List<IItemDeliveryService> _deliveryServices = new ();
        private ShopSettings _shopSettings;
        private HashSet<ITradeItemsStateListener> _itemsListeners = new();
       
        public override void InitBlock(IStructure structure, Parent parent)
        {
            GetComponentsInChildren(_deliveryServices);
            _deliveryServices.Sort();
            _deliveryServices.Insert(0, new PutToInventoryDeliveryService());
            if (_diContainer != null)
            {
                for (var i = 0; i < _deliveryServices.Count; i++)
                {
                    _diContainer.Inject(_deliveryServices[i]);
                }

                _bankSystem.InitializeShop(shopId, this);
                if (!_shopTable.TryGetSettings(shopId, out _shopSettings))
                {
                    Debug.LogError($"Shop {shopId} does not exists!");
                }
                _diContainer.Inject(itemsTrigger);
                _inventoryTradeAdapter = new ItemInstanceToTradeAdapter(shopId, _bankSystem.GetPullPutWarp(((IInventoryOwner)this).InventoryKey), TradeKind.Sell);
                _diContainer.Inject(_inventoryTradeAdapter);
                _inventoryTradeAdapter.Initialize();
                _inventoryTradeAdapter.AddListener(this);
                _sellZoneTradeAdapter = new ItemInstanceToTradeAdapter(shopId, itemsTrigger, TradeKind.Buyout);
                _diContainer.Inject(_sellZoneTradeAdapter);
                _sellZoneTradeAdapter.Initialize();
            }

            base.InitBlock(structure, parent);
        }

        private void OnDestroy()
        {
            _inventoryTradeAdapter.Dispose();
        }

        public IEnumerable<TradeItem> GetTradeItems()
        {
            foreach (var tradeItem in _inventoryTradeAdapter.GetTradeItems())
            {
                yield return tradeItem;
            }
        }

        public ITradeItemsSource GetCargoZoneItemsSource()
        {
            return _sellZoneTradeAdapter;
        }

        public ItemInstanceToTradeAdapter GetAdapterToCustomerItems(IInventoryOwner customer)
        {
            var adapter = new ItemInstanceToTradeAdapter(shopId, _bankSystem.GetPullPutWarp(customer.InventoryKey), TradeKind.Buyout);
            _diContainer.Inject(adapter);
            adapter.Initialize();
            return adapter;
        }

        public IReadOnlyList<IItemDeliveryService> GetDeliveryServices()
        {
            return _deliveryServices;
        }

        public void AddListener(ITradeItemsStateListener listener)
        {
            _itemsListeners.Add(listener);
        }

        public void RemoveListener(ITradeItemsStateListener listener)
        {
            _itemsListeners.Remove(listener);
        }

        public int GetBuyoutPrice(ItemInstance itemInstance)
        {
            return _shopSettings.GetBuyoutCost(itemInstance);
        }

        public bool RequestInteractive(ICharacterController character, out string data)
        {
            data = string.Empty;
            return true;
        }

        public void ItemAdded(TradeItem item, TradeKind kind)
        {
            foreach (var tradeItemsListener in _itemsListeners)
            {
                tradeItemsListener.ItemAdded(item, kind);
            }
        }

        public void ItemMutated(TradeItem item, TradeKind kind)
        {
            foreach (var tradeItemsListener in _itemsListeners)
            {
                tradeItemsListener.ItemMutated(item, kind);
            }
        }

        public void ItemRemoved(TradeItem item, TradeKind kind)
        {
            foreach (var tradeItemsListener in _itemsListeners)
            {
                tradeItemsListener.ItemRemoved(item, kind);
            }
        }
    }
}