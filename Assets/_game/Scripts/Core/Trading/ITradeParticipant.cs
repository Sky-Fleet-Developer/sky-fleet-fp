using System.Collections.Generic;

namespace Core.Trading
{
    public interface ITradeParticipant
    {
        IEnumerable<TradeItem> GetItems();
        IEnumerable<TradeItem> GetItems(string id);
        Inventory GetInventory();
    }
}