using System.Collections.Generic;
using System.Linq;
using Core.Configurations;
using Core.Items;
using UnityEngine;

namespace Core.Trading
{
    public class Inventory : IInventoryMasterHandler
    {
        private string _key;
        private List<ItemInstance> _items;
        private CostRule[] _costRules;

        public string Key => _key;
        
        public Inventory(string key)
        {
            _key = key;
            _items = new List<ItemInstance>();
        }

        void IInventoryMasterHandler.PutItem(ItemInstance item)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].Sign.Equals(item))
                {
                    _items[i].Merge(item);
                    return;
                }
            }

            _items.Add(item);
        }

        public IEnumerable<ItemInstance> GetItems()
        {
            return _items;
        }

        public IEnumerable<ItemInstance> GetItems(string id)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].Sign.Id == id)
                {
                    yield return _items[i];
                }
            }
        }

        bool IInventoryMasterHandler.TryPullItem(ItemSign item, float amount, out ItemInstance result)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].Sign.Equals(item))
                {
                    bool enough = _items[i].Amount >= amount;
                    if (!enough)
                    {
                        result = null;
                        return false;
                    }
                    else
                    {
                        if (Mathf.Approximately(_items[i].Amount, amount))
                        {
                            _items.RemoveAt(i);
                            result = _items[i];
                        }
                        else
                        {
                            result = _items[i].Detach(amount);
                        }

                        return true;
                    }
                }
            }
            result = null;
            return false;
        }
    }
}