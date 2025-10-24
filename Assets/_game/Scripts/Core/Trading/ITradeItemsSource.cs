using System.Collections.Generic;
using Core.Character.Interaction;
using Core.Items;

namespace Core.Trading
{
    public interface ITradeItemsSource
    {
        IEnumerable<TradeItem> GetTradeItems();
        ItemInstance PullItem(TradeItem item);
        void AddListener(ITradeItemsStateListener listener);
        void RemoveListener(ITradeItemsStateListener listener);
    }
}