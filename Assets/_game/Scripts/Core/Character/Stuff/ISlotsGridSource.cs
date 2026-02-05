using System.Collections.Generic;
using Core.Items;
using Core.Trading;

namespace Core.Character.Stuff
{
    public interface ISlotsGridReadonly
    {
        IEnumerable<SlotCell> EnumerateSlots();
        void AddListener(ISlotsGridListener listener);
        void RemoveListener(ISlotsGridListener listener);
    }
    public interface ISlotsGridSource : ISlotsGridReadonly, IItemInstancesSource, IItemsContainerReadonly
    {
    }
}