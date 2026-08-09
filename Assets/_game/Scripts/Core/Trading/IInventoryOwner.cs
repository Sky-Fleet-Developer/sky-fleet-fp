using Core.Items;

namespace Core.Trading
{
    public interface IInventoryOwner
    {
        string InventoryKey { get; }

        bool IsOwnerOf(ItemInstance item)
        {
            return item.GetOwnership() == InventoryKey;
        }
    }
}