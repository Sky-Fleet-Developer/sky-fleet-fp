using System.Collections.Generic;
using Core.Items;
using Core.Trading;

namespace Core.Character.Interaction
{
    public interface ITradeItemsStateListener
    {
        void ItemAdded(TradeItem item, TradeKind kind);
        void ItemMutated(TradeItem item, TradeKind kind);
        void ItemRemoved(TradeItem item, TradeKind kind);
    }
    public interface ITradeHandler : ICharacterHandler, ITradeParticipant
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