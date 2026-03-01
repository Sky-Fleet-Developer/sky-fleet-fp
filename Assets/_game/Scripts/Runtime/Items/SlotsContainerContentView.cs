using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Character.Stuff;
using Core.Items;
using Core.Misc;
using Core.Structure;
using Core.Trading;
using Core.Utilities;
using UnityEngine;
using Zenject;

namespace Runtime.Items
{
    [RequireComponent(typeof(Container))]
    public class SlotsContainerContentView : MonoBehaviour, ISlotsGridListener
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
        private Dictionary<string, SlotLink> _slotLinks = new();
        private Dictionary<string, (string, string)[]> _constantFieldsLinks = new();

        private void Start()
        {
            TryInit();
        }

        public void TryInit()
        {
            if (_isInitialized) return;
            _container = GetComponent<Container>();
            if (_container.Inventory is not ISlotsGridReadonly slotsGrid)
            {
                _isInitialized = true;
                return;
            }

            slotsGrid.AddListener(this);
            foreach (SlotCell slot in slotsGrid.EnumerateSlots())
            {
                var slotLink = new SlotLink();
                if (slot.TryGetProperty(Property.SiblingPropertyName, out Property siblingProperty))
                {
                    slotLink.SiblingIndex = siblingProperty.values[0].intValue;
                }

                if (slot.TryGetProperty(Property.PathPropertyName, out Property pathProperty))
                {
                    slotLink.Path = pathProperty.values[0].stringValue;
                    slotLink.Root = transform.FindDeepChild(slotLink.Path);
                }
                else
                {
                    slotLink.Root = transform;
                }

                _slotLinks[slot.SlotId] = slotLink;

                if (slot.TryGetProperty(Property.ConstantFieldsPropertyName, out Property constantFieldsProperty))
                {
                    var constantFieldsLinks = new (string, string)[constantFieldsProperty.values.Length];
                    for (var i = 0; i < constantFieldsProperty.values.Length; i++)
                    {
                        var split = constantFieldsProperty.values[i].stringValue.Split(':');
                        constantFieldsLinks[i] = (split[0], split[1]);
                    }

                    _constantFieldsLinks[slot.SlotId] = constantFieldsLinks;
                }

                if (slot.Item != null)
                {
                    AddView(slot, slotLink.Root, slotLink.SiblingIndex);
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
            }

            _isInitialized = false;
        }

        private async void AddView(SlotCell slot, Transform parent, int siblingIndex = 0)
        {
            IItemObject instance;
            var exist = parent.Find(slot.SlotId);
            if (exist)
            {
                var handle = exist.GetComponent<IItemObjectHandle>();
                _itemObjectFactory.SetupInstance(handle, slot.Item);
                instance = handle;
            }
            else
            {
                instance = await _itemObjectFactory.CreateSingle(slot.Item);
                instance.transform.SetParent(parent, false);
                instance.transform.SetSiblingIndex(siblingIndex);
                instance.transform.name = slot.SlotId;
            }

            if (instance is IBlock block)
            {
                FieldInfo[] fields = block.GetBlockConstantFieldsCached();
                if (_constantFieldsLinks.TryGetValue(slot.SlotId, out (string, string)[] value))
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (value[i].Item1 == fields[i].Name)
                        {
                            block.ApplyField(fields[i], value[i].Item2);
                        }
                    }
                }
            }
        }

        public void SlotFilled(SlotCell slot)
        {
            AddView(slot, _slotLinks[slot.SlotId].Root, _slotLinks[slot.SlotId].SiblingIndex);
        }

        public void SlotReplaced(SlotCell slot)
        {
        }

        public void SlotEmptied(SlotCell slot)
        {
        }
    }
}