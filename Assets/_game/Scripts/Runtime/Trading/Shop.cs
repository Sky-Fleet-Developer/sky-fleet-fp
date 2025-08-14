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
        public Inventory Inventory => _inventory;
        public event Action ItemsChanged;
        [Inject] private ShopTable _shopTable;
        [Inject] private ItemsTable _itemsTable;
        [Inject] private DiContainer _diContainer;
        private Inventory _inventory;
        private List<IProductDeliveryService> _deliveryServices = new ();

        private void Awake()
        {
            itemsTrigger.OnItemEnter += OnItemEntersTrigger;
            itemsTrigger.OnItemExit += OnItemExitTrigger;
        }

        private void OnItemEntersTrigger(IItemInstance item)
        {
            ItemsChanged?.Invoke();
        }
        private void OnItemExitTrigger(IItemInstance item)
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

                List<TradeItem> assortment = new List<TradeItem>();
                if (_shopTable.TryGetSettings(shopId, out ShopSettings settings))
                {
                    foreach (var itemSign in _itemsTable.GetItems())
                    {
                        if (settings.IsItemMatch(itemSign))
                        {
                            assortment.Add(new TradeItem(itemSign, 3, settings.GetCost(itemSign)));
                        }
                    }
                }

                _inventory = new Inventory(assortment);
            }

            base.InitBlock(structure, parent);
        }

        public bool TryMakeDeal(TradeDeal deal, out Transaction transaction)
        {
            var deliverySettings = new ProductDeliverySettings { PurchaserInventory = deal.GetPurchaser().GetInventory() };
            List<DeliveredProductInfo> deliveredProductInfo = new ();
            foreach (var tradeItem in deal.GetPurchases())
            {
                if (!deal.GetSeller().GetInventory().TryPullItem(tradeItem))
                {
                    Debug.LogError($"Cant pull item. Id:{tradeItem.sign.Id}");
                    continue;
                }
                bool isDelivered = false;
                for (var i = 0; i < _deliveryServices.Count; i++)
                {
                    if (_deliveryServices[i].TryDeliver(tradeItem, deliverySettings, out DeliveredProductInfo info))
                    {
                        deliveredProductInfo.Add(info);
                        isDelivered = true;
                        break;
                    }
                }

                if (!isDelivered)
                {
                    Debug.LogError($"Item was not delivered. Id:{tradeItem.sign.Id}");
                }
            }
            transaction = new Transaction(deal, deliveredProductInfo);
            return true;
        }

        public IEnumerable<IItemInstance> GetItemsInSellZone()
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