using System.Collections.Generic;
using System.Linq;
using Core.Configurations;

namespace Core.Trading
{
    public class Inventory : ITradeParticipant
    {
        private List<TradeItem> _items;
        private CostRule[] _costRules;

        public Inventory(List<TradeItem> items)
        {
            _items = items;
        }

        public void PutItem(TradeItem item)
        {
            _items.Add(item);
        }

        public IEnumerable<TradeItem> GetItems()
        {
            return _items;
        }

        public IEnumerable<TradeItem> GetItems(string id)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].sign.Id == id)
                {
                    yield return _items[i];
                }
            }
        }

        public Inventory GetInventory()
        {
            return this;
        }

        public bool TryPullItem(TradeItem tradeItem)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].Equals(tradeItem))
                {
                    bool enough = _items[i].amount >= tradeItem.amount;
                    if (!enough)
                    {
                        return false;
                    }
                    else
                    {
                        if (_items[i].amount == tradeItem.amount)
                        {
                            _items.RemoveAt(i);
                        }
                        else
                        {
                            _items[i].amount -= tradeItem.amount;
                        }

                        return true;
                    }
                }
            }
            return false;
        }
    }
}