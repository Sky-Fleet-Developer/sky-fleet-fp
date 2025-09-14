using System;
using System.Collections.Generic;
using Core.Items;
using Core.Trading;

namespace Core.Character.Interaction
{
    public interface ITradeItemsStateListener
    {
        void ItemAdded(TradeItem item);
        void ItemMutated(TradeItem item);
        void ItemRemoved(TradeItem item);
    }
    public interface ITradeHandler : ICharacterHandler, IInventoryOwner
    {
        //event Action ItemsChanged;
        bool TryMakeDeal(TradeDeal deal, out Transaction transaction);
        IEnumerable<TradeItem> GetTradeItems();
        IEnumerable<IItemObject> GetItemsInSellZone(); // TODO: replace to infinite list interface
        void AddListener(ITradeItemsStateListener listener);
        void RemoveListener(ITradeItemsStateListener listener);
    }
}