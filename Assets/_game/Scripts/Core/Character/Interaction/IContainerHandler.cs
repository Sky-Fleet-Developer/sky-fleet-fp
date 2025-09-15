using System.Collections.Generic;
using Core.Items;
using Core.Trading;

namespace Core.Character.Interaction
{
    public interface IContainerHandler : ICharacterHandler, IInventoryOwner
    {
        float MaxVolume { get; }
        float VolumeRemains { get; }
        bool TryPutItem(ItemInstance item);
        bool TryPullItem(ItemSign sign, float amount, out ItemInstance result);
        IReadOnlyList<ItemInstance> GetItems();
        void AddListener(IInventoryStateListener listener);
        void RemoveListener(IInventoryStateListener listener);
    }
}