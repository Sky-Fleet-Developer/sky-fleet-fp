using System;
using System.Collections.Generic;
using Core.Character;
using Core.Character.Interaction;
using Core.Configurations;
using Core.Items;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Trading
{
    [Serializable]
    public class Shop : Block, IInteractiveObject, ITradeHandler, IInventoryStateListener
    {
        [SerializeField] private string shopId;
        [SerializeField] private ItemsTrigger itemsTrigger;
        public bool EnableInteraction => IsActive;
        public Transform Root => transform;
        public string InventoryKey => shopId;
        //public event Action ItemsChanged;
        [Inject] private ShopTable _shopTable;
        [Inject] private BankSystem _bankSystem;
        [Inject] private DiContainer _diContainer;
        private List<IProductDeliveryService> _deliveryServices = new ();
        private IItemsContainerReadonly _inventory;
        private ShopSettings _shopSettings;
        private Dictionary<string, TradeItem> _assortment = new();
        private HashSet<ITradeItemsStateListener> _itemsListeners = new();

        private void Awake()
        {
            itemsTrigger.OnItemEnter += OnItemEntersTrigger;
            itemsTrigger.OnItemExit += OnItemExitTrigger;
        }

        private void OnItemEntersTrigger(IItemObject iItem)
        {
            //ItemsChanged?.Invoke();
        }
        private void OnItemExitTrigger(IItemObject iItem)
        {
            //ItemsChanged?.Invoke();
        }

        public override void InitBlock(IStructure structure, Parent parent)
        {
            GetComponentsInChildren(_deliveryServices);
            _deliveryServices.Sort();
            if (_diContainer != null && _inventory == null)
            {
                for (var i = 0; i < _deliveryServices.Count; i++)
                {
                    _diContainer.Inject(_deliveryServices[i]);
                }

                _bankSystem.InitializeShop(shopId, this);
                _inventory = _bankSystem.GetOrCreateInventory(this);
                if (!_shopTable.TryGetSettings(shopId, out _shopSettings))
                {
                    Debug.LogError($"Shop {shopId} does not exists!");
                }
                _inventory.AddListener(this);
                foreach (var itemInstance in _inventory.GetItems())
                {
                    _assortment.Add(itemInstance.Sign.Id, new TradeItem(itemInstance.Sign, itemInstance.Amount, _shopSettings.GetCost(itemInstance.Sign)));
                }
            }

            base.InitBlock(structure, parent);
        }

        private void OnDestroy()
        {
            _inventory?.RemoveListener(this);
        }

        public bool TryMakeDeal(TradeDeal deal, out Transaction transaction)
        {
            var deliverySettings = new ProductDeliverySettings { Purchaser = deal.GetPurchaser() };
            List<DeliveredProductInfo> deliveredProductInfo = new ();
            foreach (var tradeItem in deal.GetPurchases())
            {
                if (!_bankSystem.TryPullItem(this, tradeItem.Sign, tradeItem.amount, out ItemInstance result))
                {
                    Debug.LogError($"Cant pull item. Id:{tradeItem.Sign.Id}");
                    continue;
                }

                bool isDelivered = false;
                for (var i = 0; i < _deliveryServices.Count; i++)
                {
                    if (_deliveryServices[i].TryDeliver(result, deliverySettings, out DeliveredProductInfo info))
                    {
                        deliveredProductInfo.Add(info);
                        isDelivered = true;
                        break;
                    }
                }

                if (!isDelivered)
                {
                    Debug.LogError($"Item was not delivered. Id:{tradeItem.Sign.Id}");
                }
            }

            transaction = new Transaction(deal, deliveredProductInfo);
            transaction.FinilizeAsync();
            return true;
        }

        public IEnumerable<TradeItem> GetTradeItems()
        {
            return _assortment.Values;
        }

        public IEnumerable<IItemObject> GetItemsInSellZone()
        {
            return itemsTrigger.GetItems;
        }

        public void AddListener(ITradeItemsStateListener listener)
        {
            _itemsListeners.Add(listener);
        }

        public void RemoveListener(ITradeItemsStateListener listener)
        {
            _itemsListeners.Remove(listener);
        }

        public bool RequestInteractive(ICharacterController character, out string data)
        {
            data = string.Empty;
            return true;
        }

        void IInventoryStateListener.ItemAdded(ItemInstance item)
        {
            var tradeItem = new TradeItem(item.Sign, item.Amount, _shopSettings.GetCost(item.Sign));
            _assortment[item.Sign.Id] = tradeItem;
            foreach (var tradeItemsListener in _itemsListeners)
            {
                tradeItemsListener.ItemAdded(tradeItem);
            }
        }

        void IInventoryStateListener.ItemMutated(ItemInstance item)
        {
            var tradeItem = _assortment[item.Sign.Id];
            tradeItem.amount = item.Amount;
            foreach (var tradeItemsListener in _itemsListeners)
            {
                tradeItemsListener.ItemMutated(tradeItem);
            }
        }

        void IInventoryStateListener.ItemRemoved(ItemInstance item)
        {
            var tradeItem = _assortment[item.Sign.Id];
            _assortment.Remove(item.Sign.Id);
            foreach (var tradeItemsListener in _itemsListeners)
            {
                tradeItemsListener.ItemRemoved(tradeItem);
            }
        }
    }
}