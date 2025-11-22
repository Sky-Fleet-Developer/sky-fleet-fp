using Core.Character.Stuff;
using Core.Items;
using Core.UIStructure.Utilities;

namespace Runtime.Trading.UI
{
    public class SlotContainerListView : ItemInstancesListView
    {
        private SlotCell _slotCell;

        public override void OnDropContent(DropEventData eventData)
        {
            foreach (var draggable in eventData.Content)
            {
                if (draggable is ThingView<ItemInstance> thingView && thingView.Data == _slotCell.Item)
                {
                    return;
                }
            }

            base.OnDropContent(eventData);
        }

        public void Init(SlotCell cell)
        {
            _slotCell = cell;
            SetItems(cell.GetItems());
        }
    }
}