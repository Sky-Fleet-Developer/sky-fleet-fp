using System.Collections.Generic;

namespace Core.Trading
{
    /// <summary>
    /// Interface for make action with item before trade will be performed
    /// </summary>
    public interface ITradePreprocessor
    {
        IEnumerable<TradeItem> ProcessItem(TradeItem item);
        void ProcessCost(TradeItem item);
    }
}