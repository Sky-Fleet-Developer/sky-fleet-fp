using System;
using System.Collections.Generic;
using Core.Trading;

namespace Core.Character.Interaction
{
    public interface ITradeHandler : ICharacterHandler
    {
        Inventory Inventory { get; }
        event Action ItemsChanged;
        bool TryMakeDeal(TradeDeal deal, out Transaction transaction);
    }
}