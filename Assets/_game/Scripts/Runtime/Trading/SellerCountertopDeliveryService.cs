using Core.Configurations;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Trading
{
    public class SellerCountertopDeliveryService : MonoBehaviour, IProductDeliveryService
    {
        [Inject] private ItemsTable _tableItems;

        public int Order => transform.GetSiblingIndex();

        public bool TryDeliver(TradeItem item, ProductDeliverySettings deliverySettings, out DeliveredProductInfo deliveredProductInfo)
        {
            if (item.sign.HasTag(ItemSign.LargeTag))
            {
                deliveredProductInfo = null;
                return false;
            }

            deliveredProductInfo = new DeliveredProductInfo();
            deliverySettings.PurchaserInventory.PutItem(item);
            deliveredProductInfo.IsPlacedInInventory = true;
            return true;
        }
    }
}