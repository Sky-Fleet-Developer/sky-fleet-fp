using Core.Trading;

namespace Core.Character.Interaction
{
    public interface IPickUpHandler : ICharacterHandler
    {
        void PickUpTo(IInventoryOwner inventoryOwner);
    }
}