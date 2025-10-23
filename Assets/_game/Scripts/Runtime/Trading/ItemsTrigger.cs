using System;
using System.Collections.Generic;
using Core.Items;
using UnityEngine;

namespace Runtime.Trading
{
    public class ItemsTrigger : MonoBehaviour
    {
        private Dictionary<IItemObject, HashSet<Collider>> _items = new();
        private Dictionary<ItemInstance, IItemObject> _objectByInstance = new();
        public event Action<IItemObject> OnItemEnter;
        public event Action<IItemObject> OnItemExit;
        public IEnumerable<IItemObject> GetItems() => _items.Keys;
        public bool TryGetItem(ItemInstance instance, out IItemObject item) => _objectByInstance.TryGetValue(instance, out item);
        
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
                    OnItemExit?.Invoke(item);
                }
            }
        }
    }
}