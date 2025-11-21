using System;
using System.Collections.Generic;
using Core.Items;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Trading
{
    public class ItemsTrigger : MonoBehaviour, IItemInstancesSource
    {
        [Inject] private IItemFactory _itemFactory;
        private Dictionary<IItemObject, HashSet<Collider>> _items = new();
        private Dictionary<ItemInstance, IItemObject> _objectByInstance = new();
        public event Action<IItemObject> OnItemEnter;
        public event Action<IItemObject> OnItemExit;
        public IEnumerable<IItemObject> GetItems() => _items.Keys;
        public bool TryGetItem(ItemInstance instance, out IItemObject item) => _objectByInstance.TryGetValue(instance, out item);
        private List<IInventoryStateListener> _listeners = new();
        IEnumerable<ItemInstance> IItemInstancesSource.EnumerateItems() => _objectByInstance.Keys;

        public bool CanPutAnyItem => false;

        public bool TryPutItem(ItemInstance item)
        {
            Debug.LogError("You trying to put item inside trigger");
            return false;
        }

        public bool TryPullItem(ItemInstance item, float amount, out ItemInstance result)
        {
            if (!_objectByInstance.TryGetValue(item, out var obj))
            {
                result = null;
                return false;
            }

            if (obj.SourceItem.Amount > amount)
            {
                result = obj.SourceItem.Detach(amount);
                foreach (var listener in _listeners)
                {
                    listener.ItemMutated(obj.SourceItem);
                }
                return true;
            }
            if(Mathf.Approximately(obj.SourceItem.Amount, amount))
            {
                if (obj is IItemObjectHandle itemHandle)
                {
                    _itemFactory.Deconstruct(itemHandle);
                }
                _items[obj].Clear();
                _items.Remove(obj);
                _objectByInstance.Remove(item);
                result = obj.SourceItem;
                foreach (var listener in _listeners)
                {
                    listener.ItemRemoved(item);
                }
                return true;
            }
            result = null;
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<IItemObject>();
            if (item is { SourceItem: not null })
            {
                if (!_items.TryGetValue(item, out HashSet<Collider> colliders))
                {
                    colliders = new HashSet<Collider>();
                    _items.Add(item, colliders);
                    _objectByInstance.Add(item.SourceItem, item);
                    foreach (var listener in _listeners)
                    {
                        listener.ItemAdded(item.SourceItem);
                    }
                    OnItemEnter?.Invoke(item);
                }
                colliders.Add(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var item = other.GetComponentInParent<IItemObject>();
            if (item is { SourceItem: not null } && _items.TryGetValue(item, out HashSet<Collider> colliders))
            {
                colliders.Remove(other);
                if (colliders.Count == 0)
                {
                    _objectByInstance.Remove(item.SourceItem);
                    _items.Remove(item);
                    foreach (var listener in _listeners)
                    {
                        listener.ItemRemoved(item.SourceItem);
                    }
                    OnItemExit?.Invoke(item);
                }
            }
        }


        public void AddListener(IInventoryStateListener listener)
        {
            Debug.Log($"Adding listener: {listener.GetType()}");
            _listeners.Add(listener);
        }

        public void RemoveListener(IInventoryStateListener listener)
        {
            Debug.Log($"Removed listener: {listener.GetType()}");
            _listeners.Remove(listener);
        }
    }
}