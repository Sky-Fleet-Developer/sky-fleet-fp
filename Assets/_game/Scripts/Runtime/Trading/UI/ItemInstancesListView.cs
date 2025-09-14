using System;
using Core.Items;
using Core.Trading;
using Core.UIStructure.Utilities;

namespace Runtime.Trading.UI
{
    public class ItemInstancesListView : ThingsListView<ItemInstance, ItemInstanceView>, IInventoryStateListener
    {
        public override void AddItem(ItemInstance item)
        {
            var index = _thingsData.FindIndex(x => x.Sign.Id == item.Sign.Id);
            if (index != -1)
            {
                _thingsData[index].Merge(item);
                _views[index].RefreshView();
                return;
            }
            base.AddItem(item);
        }

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