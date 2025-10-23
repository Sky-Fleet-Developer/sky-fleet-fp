using Core.Configurations;
using Core.Data;
using Core.Items;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Trading
{
    public class SellerCountertopDeliveryService : MonoBehaviour, IProductDeliveryService
    {
        [Inject(Optional = true)] private BankSystem _bankSystem;

        public int Order => transform.GetSiblingIndex();

        public bool TryDeliver(ItemInstance item, ProductDeliverySettings deliverySettings, out DeliveredProductInfo deliveredProductInfo)
        {
            if (item.Sign.HasTag(ItemSign.LiquidTag) || item.GetVolume() > GameData.Data.shopMaxAmountToInventoryDelivery)
            {
                deliveredProductInfo = null;
                return false;
            }

            if (_bankSystem.TryPutItem(deliverySettings.Purchaser, item))
            {
                deliveredProductInfo = new DeliveredProductInfo();
                deliveredProductInfo.IsPlacedInInventory = true;
                return true;
            }
            
            deliveredProductInfo = null;
            return false;
        }
    }
}