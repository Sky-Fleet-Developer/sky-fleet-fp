using System.Collections.Generic;
using Core.Character.Interaction;
using Core.Items;

namespace Core.Trading
{
    public interface ITradeItemsSource
    {
        IEnumerable<TradeItem> GetTradeItems();
        bool TryPullItem(TradeItem item, out ItemInstance result);
        void AddListener(ITradeItemsStateListener listener);
        void RemoveListener(ITradeItemsStateListener listener);
    }
}