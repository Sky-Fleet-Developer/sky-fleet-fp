using System;
using System.Collections.Generic;
using Core.Trading;

namespace Core.Character.Interaction
{
    public interface ITradeHandler : ICharacterHandler
    {
        IEnumerable<TradeItem> GetItems();
        event Action ItemsChanged;
        bool TryMakeDeal(TradeDeal deal, out Transaction transaction);
    }
}