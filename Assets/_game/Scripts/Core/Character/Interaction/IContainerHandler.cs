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
        bool TryPullItem(ItemInstance item, float amount, out ItemInstance result);
        IEnumerable<ItemInstance> GetItems();
        void AddListener(IInventoryStateListener listener);
        void RemoveListener(IInventoryStateListener listener);
    }
}