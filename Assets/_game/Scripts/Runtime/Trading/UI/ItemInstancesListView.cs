using System;
using Core.Items;
using Core.Trading;
using Core.UIStructure.Utilities;

namespace Runtime.Trading.UI
{
    public class ItemInstancesListView : DraggableThingsListView<ItemInstance, ItemInstanceView>, IInventoryStateListener
    {
        /*public override void AddItem(ItemInstance item)
        {
            var index = ThingsData.FindIndex(x => x.IsEqualsSignOrIdentity(item));
            if (index != -1)
            {
                ThingsData[index].Merge(item);
                Views[index].RefreshView();
                return;
            }
            base.AddItem(item);
        }*/

        public void ItemAdded(ItemInstance item)
        {
            AddItem(item);
        }

        public void ItemMutated(ItemInstance item)
        {
            RefreshItem(item);
        }

        public void ItemRemoved(ItemInstance item)
        {
            RemoveItem(item);
        }
    }
}