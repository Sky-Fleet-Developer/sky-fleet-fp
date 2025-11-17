using System;
using System.Collections.Generic;
using System.Linq;
using Core.Configurations;
using Core.Items;
using UnityEngine;

namespace Core.Trading
{
    public class Inventory : IItemsContainerMasterHandler
    {
        private string _key;
        private List<ItemInstance> _items;
        private CostRule[] _costRules;
        private HashSet<IInventoryStateListener> _stateListeners = new();
        public string Key => _key;
        
        public Inventory(string key)
        {
            _key = key;
            _items = new List<ItemInstance>();
        }

        public void AddListener(IInventoryStateListener listener)
        {
            _stateListeners.Add(listener);
        }

        public void RemoveListener(IInventoryStateListener listener)
        {
            _stateListeners.Remove(listener);
        }

        private void ItemAdded(ItemInstance item)
        {
            foreach (var listener in _stateListeners)
            {
                listener.ItemAdded(item);
            }
        }
        private void ItemMutated(ItemInstance item)
        {
            foreach (var listener in _stateListeners)
            {
                listener.ItemMutated(item);
            }
        }
        private void ItemRemoved(ItemInstance item)
        {
            foreach (var listener in _stateListeners)
            {
                listener.ItemRemoved(item);
            }
        }

        void IItemsContainerMasterHandler.PutItem(ItemInstance item)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].Sign.Equals(item.Sign))
                {
                    _items[i].Merge(item);
                    ItemMutated(_items[i]);
                    return;
                }
            }
            
            _items.Add(item);
            ItemAdded(item);
        }

        public IEnumerable<ItemInstance> GetItems()
        {
            return _items;
        }

        /*public IEnumerable<ItemInstance> GetItems(string id)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].Sign.Id == id)
                {
                    yield return _items[i];
                }
            }
        }*/
        
        ItemInstance IItemInstancesSource.PullItem(ItemInstance item, float amount)
        {
            if (((IItemsContainerMasterHandler)this).TryPullItem(item.Sign, amount, out var result))
            {
                return result;
            }
            return null;
        }

        bool IItemsContainerMasterHandler.TryPullItem(ItemSign item, float amount, out ItemInstance result)
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
                            result = _items[i];
                            _items.RemoveAt(i);
                            ItemRemoved(result);
                        }
                        else
                        {
                            result = _items[i].Detach(amount);
                            ItemMutated(_items[i]);
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