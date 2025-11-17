using System.Collections.Generic;
using Core.Items;
using Core.Trading;

namespace Core.Character.Stuff
{
    public interface ISlotsGridSource : IItemInstancesSource
    {
        IEnumerable<SlotCell> EnumerateSlots();
        ItemInstance PullItem(SlotCell slot, float amount);
        void AddListener(ISlotsGridListener listener);
        void RemoveListener(ISlotsGridListener listener);
    }
}