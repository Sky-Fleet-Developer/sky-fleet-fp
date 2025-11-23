using System;
using System.Collections.Generic;
using System.Linq;
using Core.Configurations;
using Core.Items;
using UnityEngine;
using Zenject;

namespace Core.Trading
{
    public class Inventory : IItemsContainerMasterHandler
    {
        private BankSystem _bankSystem;
        private string _key;
        private List<ItemInstance> _items;
        private HashSet<IInventoryStateListener> _stateListeners = new();
        public string Key => _key;
        public bool IsEmpty => _items.Count == 0;

        public Inventory(string key, BankSystem bankSystem)
        {
            _key = key;
            _bankSystem = bankSystem;
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

        bool IPullPutItem.TryPutItem(ItemInstance item)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_bankSystem.TryMergeItems(item, _items[i]))
                {
                    ItemMutated(_items[i]);
                    return true;
                }
            }

            _items.Add(item);
            ItemAdded(item);
            return true;
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

        public bool TryPullItem(ItemInstance item, float amount, out ItemInstance result)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].IsEqualsSignOrIdentity(item))
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
        
        public void Dispose()
        {
            _stateListeners.Clear();
            _bankSystem = null;
            _key = null;
        }
    }
}