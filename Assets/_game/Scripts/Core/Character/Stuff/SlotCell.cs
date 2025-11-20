using System;
using System.Collections.Generic;
using Core.Configurations;
using Core.Items;
using Core.Trading;
using UnityEngine.Assertions;
using Zenject;

namespace Core.Character.Stuff
{
    public class SlotCell : IItemInstancesSource, ICloneable
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

        public object Clone()
        {
            var result = new SlotCell(_slotId, _includeTags, _excludeTags);
            Assert.IsNull(result._content);
            return result;
        }

        public bool TrySetItem(ItemInstance content)
        {
            if (content == null)
            {
                _content = null;
                return true;
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

            _content = content;
            if (_content.Sign.TryGetProperty(ItemSign.ContainerTag, out var containerProperty) &&
                _content.TryGetProperty(ItemSign.IdentifiableTag, out var identifiableProperty))
            {
                _attachedInventory = _bankSystem.GetOrCreateInventory(identifiableProperty
                    .values[ItemProperty.IdentifiableInstance_Identifier].stringValue);
            }
            else
            {
                _attachedInventory = null;
            }
            return true;
        }

        public IEnumerable<ItemInstance> EnumerateItems()
        {
            if (IsContainer) return Array.Empty<ItemInstance>();
            return _attachedInventory.EnumerateItems();
        }

        public ItemInstance PullItem(ItemInstance item, float amount)
        {
            if (_attachedInventory == null)
            {
                throw new Exception("Has no attached inventory on cell");
            }
            
            return _attachedInventory.PullItem(item, amount);
        }

        public void AddListener(IInventoryStateListener listener)
        {
            _listeners.Add(listener);
        }

        public void RemoveListener(IInventoryStateListener listener)
        {
            _listeners.Remove(listener);
        }
    }
}