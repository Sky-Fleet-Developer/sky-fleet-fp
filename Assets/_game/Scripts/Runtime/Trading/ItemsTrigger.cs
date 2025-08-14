using System;
using System.Collections.Generic;
using Core.Items;
using UnityEngine;

namespace Runtime.Trading
{
    public class ItemsTrigger : MonoBehaviour
    {
        private Dictionary<IItemInstance, HashSet<Collider>> _items = new();

        public event Action<IItemInstance> OnItemEnter;
        public event Action<IItemInstance> OnItemExit;
        public IEnumerable<IItemInstance> GetItems => _items.Keys;
        
        private void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<IItemInstance>();
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
            var item = other.GetComponentInParent<IItemInstance>();
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