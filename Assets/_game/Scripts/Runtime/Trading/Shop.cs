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
    public class Shop : Block, IInteractiveObject, ITradeHandler
    {
        [SerializeField] private string shopId;
        [SerializeField] private ItemsTrigger itemsTrigger;
        public bool EnableInteraction => IsActive;
        public Transform Root => transform;
        public string InventoryKey => shopId;
        public event Action ItemsChanged;
        [Inject] private ShopTable _shopTable;
        [Inject] private BankSystem _bankSystem;
        [Inject] private DiContainer _diContainer;
        private List<IProductDeliveryService> _deliveryServices = new ();
        private IInventoryReadonly _inventory;
        private ShopSettings _shopSettings;
        private void Awake()
        {
            itemsTrigger.OnItemEnter += OnItemEntersTrigger;
            itemsTrigger.OnItemExit += OnItemExitTrigger;
        }

        private void OnItemEntersTrigger(IItemObject iItem)
        {
            ItemsChanged?.Invoke();
        }
        private void OnItemExitTrigger(IItemObject iItem)
        {
            ItemsChanged?.Invoke();
        }

        public override void InitBlock(IStructure structure, Parent parent)
        {
            GetComponentsInChildren(_deliveryServices);
            _deliveryServices.Sort();
            if (_diContainer != null)
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
            }

            base.InitBlock(structure, parent);
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
            foreach (var itemInstance in _inventory.GetItems())
            {
                yield return new TradeItem(itemInstance.Sign, itemInstance.Amount, _shopSettings.GetCost(itemInstance.Sign));
            }
        }

        public IEnumerable<IItemObject> GetItemsInSellZone()
        {
            return itemsTrigger.GetItems;
        }

        public bool RequestInteractive(ICharacterController character, out string data)
        {
            data = string.Empty;
            return true;
        }
    }
}