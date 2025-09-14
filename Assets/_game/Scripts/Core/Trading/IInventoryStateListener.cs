using Core.Items;

namespace Core.Trading
{
    public interface IInventoryStateListener
    {
        void ItemAdded(ItemInstance item);
        void ItemMutated(ItemInstance item);
        void ItemRemoved(ItemInstance item);
    }
}