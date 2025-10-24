using System;
using System.Collections.Generic;
using Core.Character.Interaction;
using Core.Trading;
using Core.UIStructure.Utilities;

namespace Runtime.Trading.UI
{
    public class TradeItemsListView : ThingsListView<TradeItem, TradeItemView>, ITradeItemsStateListener
    {
        public TradeItemKind kindFilter;
        private ProductDeliverySettings _deliverySettings;
        public event Action<TradeItem, float> OnItemInCardAmountChanged;

        protected override void InitItem(TradeItemView item)
        {
            item.SetDeliverySettings(_deliverySettings);
            item.SetInCardAmountChangedCallback(ItemInCardAmountChanged);
        }

        private void ItemInCardAmountChanged(TradeItem item, float amount)
        {
            OnItemInCardAmountChanged?.Invoke(item, amount);
        }

        public void SetDeliverySettings(ProductDeliverySettings settings)
        {
            _deliverySettings = settings;
        }
        
        /*public override void AddItem(TradeItem item)
        {
            var index = _thingsData.FindIndex(x => x.Sign.Id == item.Sign.Id);
            if (index != -1)
            {
                _thingsData[index].amount = item.amount;
                _views[index].RefreshView();
                return;
            }
            base.AddItem(item);
        }*/

        private bool IsKindValid(TradeItemKind kind)
        {
            return kindFilter.HasFlag(kind);
        }
        
        public void ItemAdded(TradeItem item, TradeItemKind kind)
        {
            if(!IsKindValid(kind)) return;
            AddItem(item);
        }

        public void ItemMutated(TradeItem item, TradeItemKind kind)
        {
            if(!IsKindValid(kind)) return;
            RefreshItem(item);
        }

        public void ItemRemoved(TradeItem item, TradeItemKind kind)
        {
            if(!IsKindValid(kind)) return;
            RemoveItem(item);
        }
    }
}