using System;
using System.Collections.Generic;
using Core.Character.Stuff;
using Core.Items;
using Core.Misc;
using Core.Trading;
using Core.Utilities;
using UnityEngine;
using Zenject;

namespace Runtime.Items
{
    [RequireComponent(typeof(Container))]
    public class ContainerContentView : MonoBehaviour, IInventoryStateListener, ISlotsGridListener
    {
        private class SlotLink
        {
            public string Path;
            public Transform Root;
            public int SiblingIndex;
        }
        
        [Inject] private IItemObjectFactory _itemObjectFactory;
        private Container _container;
        private bool _isInitialized;
        private Dictionary<string, SlotLink> _slotLinks;
        private void Start()
        {
            TryInit();
        }

        public void TryInit()
        {
            if (_isInitialized) return;
            _container = GetComponent<Container>();
            if (_container.Inventory is ISlotsGridReadonly slotsGrid)
            {
                slotsGrid.AddListener(this);
                _slotLinks = new Dictionary<string, SlotLink>();
                foreach (SlotCell slot in slotsGrid.EnumerateSlots())
                {
                    _slotLinks[slot.SlotId] = new SlotLink();
                    if (slot.TryGetProperty(Property.SiblingPropertyName, out Property siblingProperty))
                    {
                        _slotLinks[slot.SlotId].SiblingIndex = siblingProperty.values[0].intValue;
                    }
                    if (slot.TryGetProperty(Property.PathPropertyName, out Property pathProperty))
                    {
                        _slotLinks[slot.SlotId].Path = pathProperty.values[0].stringValue;
                        _slotLinks[slot.SlotId].Root = transform.FindDeepChild(_slotLinks[slot.SlotId].Path);
                    }
                    else
                    {
                        _slotLinks[slot.SlotId].Root = transform;
                    }
                    
                    if (slot.Item != null)
                    {
                        AddViewIfNeeded(slot.Item, _slotLinks[slot.SlotId].Root, _slotLinks[slot.SlotId].SiblingIndex);
                    }
                }
            }
            else
            {
                _container.AddListener(this);
                foreach (var itemInstance in _container.GetItems())
                {
                    AddViewIfNeeded(itemInstance, transform);
                }
            }
            
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_container != null)
            {
                if (_container.Inventory is ISlotsGridReadonly slotsGrid)
                {
                    slotsGrid.RemoveListener(this);
                    _slotLinks.Clear();
                }
                else
                {
                    _container.RemoveListener(this);
                }
            }

            _isInitialized = false;
        }

        private async void AddViewIfNeeded(ItemInstance item, Transform parent, int siblingIndex = 0)
        {
            var instance = await _itemObjectFactory.CreateSingle(item);
            instance.transform.SetParent(parent, false);
            instance.transform.SetSiblingIndex(siblingIndex);
        }

        public void ItemAdded(ItemInstance item)
        {
            AddViewIfNeeded(item, transform);
        }

        public void ItemMutated(ItemInstance item)
        {
            
        }

        public void ItemRemoved(ItemInstance item)
        {
            
        }

        public void SlotFilled(SlotCell slot)
        {
            AddViewIfNeeded(slot.Item, _slotLinks[slot.SlotId].Root, _slotLinks[slot.SlotId].SiblingIndex);
        }

        public void SlotReplaced(SlotCell slot)
        {
        }

        public void SlotEmptied(SlotCell slot)
        {
        }
    }
}