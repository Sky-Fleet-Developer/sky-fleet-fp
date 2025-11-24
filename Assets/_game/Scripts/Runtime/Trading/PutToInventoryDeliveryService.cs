using Core.Configurations;
using Core.Data;
using Core.Items;
using Core.Localization;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Trading
{
    public class PutToInventoryDeliveryService : IItemDeliveryService
    {
        [Inject(Optional = true)] private BankSystem _bankSystem;

        public int Order => -1;
        public PutItemResult Deliver(ItemInstance item, IInventoryOwner destination)
        {
            if (!IsCanDeliver(item.Sign, destination))
            {
                return PutItemResult.Fail;
                //throw new System.Exception("Can't put item to inventory");
            }

            return _bankSystem.TryPutItem(destination.InventoryKey, item);
        }

        public bool IsCanDeliver(ItemSign item, IInventoryOwner destination)
        {
            return !item.HasTag(ItemSign.LiquidTag);
        }
        
        public string NameToView => LocalizationService.Localize($"put-to-inventory-delivery_name");
        public string IconKey => "ui_put-to-inventory-delivery_icon";
    }
}