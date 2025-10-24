using System;
using System.Collections.Generic;
using Core.Items;
using Core.Trading;

namespace Core.Character.Interaction
{
    [Flags]
    public enum TradeItemKind
    {
        Sell = 1, Buyout = 2
    }
    public interface ITradeItemsStateListener
    {
        void ItemAdded(TradeItem item, TradeItemKind kind);
        void ItemMutated(TradeItem item, TradeItemKind kind);
        void ItemRemoved(TradeItem item, TradeItemKind kind);
    }
    public interface ITradeHandler : ICharacterHandler, IInventoryOwner
    {
        //event Action ItemsChanged;
        IEnumerable<TradeItem> GetTradeItems(); // TODO: replace to infinite list interface
        ITradeItemsSource GetCargoZoneItemsSource();
        ItemInstanceToTradeAdapter GetAdapterToCustomerItems(IInventoryOwner customer);
        IReadOnlyList<IItemDeliveryService> GetDeliveryServices();
        void AddListener(ITradeItemsStateListener listener);
        void RemoveListener(ITradeItemsStateListener listener);
        int GetBuyoutPrice(ItemInstance itemInstance);
    }
}