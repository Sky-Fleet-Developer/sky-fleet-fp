using System;
using System.Collections.Generic;
using Core.Configurations;
using Core.Items;
using Core.Trading;
using UnityEngine.Assertions;
using Zenject;

namespace Core.Character.Stuff
{
    public class SlotCell : IItemInstancesSource, ICloneable, IDisposable
    {
        [Inject] private BankSystem _bankSystem;
        private ItemInstance _content;
        private string _slotId;
        private TagCombination[] _includeTags;
        private TagCombination[] _excludeTags;
        private List<IInventoryStateListener> _listeners = new();
        private IItemsContainerReadonly _attachedInventory;
        public string SlotId => _slotId;

        public SlotCell(string slotId, TagCombination[] includeTags, TagCombination[] excludeTags)
        {
            _slotId = slotId;
            _includeTags = includeTags;
            _excludeTags = excludeTags;
        }

        public bool HasItem => _content != null;
        public ItemInstance Item => _content;
        public bool IsContainer => _attachedInventory != null;
        public string ContainerKey => _attachedInventory.Key;

        public object Clone()
        {
            var result = new SlotCell(_slotId, _includeTags, _excludeTags);
            Assert.IsNull(result._content);
            return result;
        }

        public bool CanSetItem(ItemInstance content)
        {
            if(content == null) return true;
            if (!content.Sign.TryGetProperty(ItemSign.EquipableTag, out var equipableProperty))
            {
                return false;
            }
            if (equipableProperty.values[ItemProperty.Equipable_SlotType].stringValue != _slotId)
            {
                return false;
            }
            bool isMatch = false;
            foreach (var tag in _includeTags)
            {
                if (tag.IsItemMatch(content.Sign))
                {
                    isMatch = true;
                }
            }

            if (!isMatch)
            {
                return false;
            }
            
            foreach (var tag in _excludeTags)
            {
                if (tag.IsItemMatch(content.Sign))
                {
                    return false;
                }
            }
            return true;
        }
        
        public bool TrySetItem(ItemInstance content)
        {
            if (!CanSetItem(content)) return false;
            if(_content == null && content == null) return true;
            
            bool itemWasNull = _content == null;
            if (content == null)
            {
                foreach (var listener in _listeners)
                {
                    listener.ItemRemoved(_content);
                }
            }
            
            _content = content;

            if (_content == null)
            {
                _attachedInventory = null;
                return true;
            }
            
            if (_attachedInventory != null) // remove listeners from old inventory
            {
                foreach (var listener in _listeners)
                {
                    _attachedInventory.RemoveListener(listener);
                }
            }
            if (_content.IsContainer && _content.TryGetProperty(ItemSign.IdentifiableTag, out var identifiableProperty))
            {
                _attachedInventory = _bankSystem.GetOrCreateInventory(identifiableProperty
                    .values[ItemProperty.IdentifiableInstance_Identifier].stringValue);
                foreach (var listener in _listeners)
                {
                    _attachedInventory.AddListener(listener);
                }
            }
            else
            {
                _attachedInventory = null;
            }
            
            foreach (var listener in _listeners)
            {
                if (itemWasNull)
                {
                    listener.ItemAdded(_content);
                }
                else
                {
                    listener.ItemRemoved(_content);
                }
            }
            
            return true;
        }

        public IEnumerable<ItemInstance> GetItems()
        {
            if (!IsContainer) return Array.Empty<ItemInstance>();
            return _attachedInventory.GetItems();
        }

        /// <summary>
        /// Get the internal item from container inside of cell's item
        /// </summary>
        /// <returns>Returns item from container inside of cell's item</returns>
        /// <exception cref="Exception">Item is null or has no inventory => exception</exception>
        public bool TryPullItem(ItemInstance item, float amount, out ItemInstance result)
        {
            if (_attachedInventory == null)
            {
                throw new Exception("Has no attached inventory on cell");
            }
            
            return _bankSystem.TryPullItem(_attachedInventory.Key, item, amount, out result);
        }

        public void AddListener(IInventoryStateListener listener)
        {
            _listeners.Add(listener);
            _attachedInventory?.AddListener(listener);
        }

        public void RemoveListener(IInventoryStateListener listener)
        {
            _listeners.Remove(listener);
            _attachedInventory?.RemoveListener(listener);
        }

        /// <summary>
        /// Put item to container inside of cell's item
        /// </summary>
        /// <returns>False when cant put item inside cell's item container or when cell's item has no container</returns>
        public bool TryPutItem(ItemInstance item)
        {
            if(_attachedInventory == null)
            {
                return false;
            }
            return _bankSystem.TryPutItem(_attachedInventory.Key, item);
        }

        public void Dispose()
        {
            _bankSystem = null;
            _content = null;
            _attachedInventory = null;
            _excludeTags = null;
            _includeTags = null;
            _listeners.Clear();
            _listeners = null;
            _slotId = null;
        }
    }
}