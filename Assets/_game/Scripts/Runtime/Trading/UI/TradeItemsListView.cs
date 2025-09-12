using System;
using Core.Trading;
using Core.UIStructure.Utilities;

namespace Runtime.Trading.UI
{
    public class TradeItemsListView : ThingsListView<TradeItem, TradeItemView>
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
    }
}