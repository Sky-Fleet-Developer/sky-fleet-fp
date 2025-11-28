using Core.Items;

namespace Core.Trading.Tests
{
    public class FakeDeliveryService : IItemDeliveryService
    {
        private BankSystem _bankSystem;

        public FakeDeliveryService(BankSystem bankSystem)
        {
            _bankSystem = bankSystem;
        }
        public int Order => -1;
        public PutItemResult Deliver(ItemInstance item, IInventoryOwner destination)
        {
            if (!IsCanDeliver(item.Sign, destination))
            {
                return PutItemResult.Fail;
            }

            return _bankSystem.TryPutItem(destination.InventoryKey, item);
        }

        public bool IsCanDeliver(ItemSign item, IInventoryOwner destination)
        {
            return !item.HasTag(ItemSign.LiquidTag);
        }

        public string NameToView => null;
        public string IconKey => null;
    }
}