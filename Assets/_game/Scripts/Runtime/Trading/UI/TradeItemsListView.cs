using System;
using Core.Character.Interaction;
using Core.Trading;
using Core.UIStructure.Utilities;

namespace Runtime.Trading.UI
{
    public class TradeItemsListView : ThingsListView<TradeItem, TradeItemView>, ITradeItemsStateListener
    {
        public event Action<TradeItem, float> OnItemInCardAmountChanged;

        protected override void InitItem(TradeItemView item)
        {
            item.SetInCardAmountChangedCallback(ItemInCardAmountChanged);
        }

        private void ItemInCardAmountChanged(TradeItem item, float amount)
        {
            OnItemInCardAmountChanged?.Invoke(item, amount);
        }

        public override void AddItem(TradeItem item)
        {
            var index = _thingsData.FindIndex(x => x.Sign.Id == item.Sign.Id);
            if (index != -1)
            {
                _thingsData[index].amount = item.amount;
                _views[index].RefreshView();
                return;
            }
            base.AddItem(item);
        }

        public void ItemAdded(TradeItem item)
        {
            AddItem(item);
        }

        public void ItemMutated(TradeItem item)
        {
            RefreshItem(item);
        }

        public void ItemRemoved(TradeItem item)
        {
            RemoveItem(item);
        }
    }
}