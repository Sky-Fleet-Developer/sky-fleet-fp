using System;
using System.Collections.Generic;
using Core.Items;
using UnityEngine;

namespace Runtime.Trading
{
    public class ItemsTrigger : MonoBehaviour
    {
        private Dictionary<IItemObject, HashSet<Collider>> _items = new();

        public event Action<IItemObject> OnItemEnter;
        public event Action<IItemObject> OnItemExit;
        public IEnumerable<IItemObject> GetItems => _items.Keys;
        
        private void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<IItemObject>();
            if (item != null)
            {
                if (!_items.TryGetValue(item, out HashSet<Collider> colliders))
                {
                    colliders = new HashSet<Collider>();
                    _items.Add(item, colliders);
                    OnItemEnter?.Invoke(item);
                }
                colliders.Add(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var item = other.GetComponentInParent<IItemObject>();
            if (item != null && _items.TryGetValue(item, out HashSet<Collider> colliders))
            {
                colliders.Remove(other);
                if (colliders.Count == 0)
                {
                    _items.Remove(item);
                    OnItemExit?.Invoke(item);
                }
            }
        }
    }
}