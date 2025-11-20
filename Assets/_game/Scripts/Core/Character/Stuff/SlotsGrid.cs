using System;
using System.Collections.Generic;
using System.Linq;
using Core.Items;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Core.Character.Stuff
{
    public class SlotsGrid : IItemsContainerMasterHandler, ISlotsGridSource, ICloneable
    {
        private string _gridId;
        private string _inventoryKey;
        private SlotCell[] _slots;
        private List<ISlotsGridListener> _listenersSlots = new ();

        public string Key => _inventoryKey;

        public SlotsGrid(string gridId, SlotCell[] slots)
        {
            _gridId = gridId;
            _slots = slots;
        }

        [Inject] private void Inject(DiContainer container)
        {
            foreach (var slotCell in _slots)
            {
                container.Inject(slotCell);
            }
        }
        
        public void SetAsInventory(string inventoryKey) => _inventoryKey = inventoryKey;

        public object Clone()
        {
            SlotCell[] slots = new SlotCell[_slots.Length];
            for (var i = 0; i < _slots.Length; i++)
            {
                slots[i] = _slots[i].Clone() as SlotCell;
            }
            var result = new SlotsGrid(_gridId, slots);
            if (result.GetItems().Any())
            {
                throw new ApplicationException("Clone has to be empty! Check cloning operation!");
            }
            return result;
        }

        
        public IEnumerable<ItemInstance> GetItems()
        {
            foreach (var slotCell in _slots)
            {
                if (slotCell.HasItem)
                {
                    yield return slotCell.Item;
                    if (slotCell.IsContainer)
                    {
                        foreach (var item in slotCell.EnumerateItems()) yield return item;
                    }
                }
            }
        }

        public ItemInstance PullItem(ItemInstance item, float amount)
        {
            var owner = item.GetOwnership();
            foreach (var slotCell in _slots)
            {
                if (slotCell.HasItem)
                {
                    if (slotCell.Item.Equals(item))
                    {
                        if (amount < slotCell.Item.Amount)
                        {
                            return slotCell.Item.Detach(amount);
                        }
                        else
                        {
                            slotCell.TrySetItem(null);
                            return item;
                        }
                    }
                    if (slotCell.IsContainer)
                    {
                        if (owner == slotCell.Item.Identifier)
                        {
                            return slotCell.PullItem(item, amount);
                        }
                    }
                }
            }
            return null;
        }

        public void AddListener(IInventoryStateListener listener)
        {
            foreach (var slotCell in _slots)
            {
                slotCell.AddListener(listener);
            }
        }

        public void RemoveListener(IInventoryStateListener listener)
        {
            foreach (var slotCell in _slots)
            {
                slotCell.RemoveListener(listener);
            }
        }

        public void PutItem(ItemInstance item)
        {
            foreach (var slotCell in _slots)
            {
                if (!slotCell.HasItem)
                {
                    if (slotCell.TrySetItem(item))
                    {
                        foreach (var listener in _listenersSlots)
                        {
                            listener.SlotFilled(slotCell);
                        }
                        return;
                    }
                }
            }
            
            foreach (var slotCell in _slots)
            {
                if (slotCell.HasItem && slotCell.IsContainer)
                {
                    if (slotCell.TryPutItem(item))
                    {

                        return;
                    }
                }
            }
        }
        
        public bool TryPullItem(ItemSign sign, float amount, out ItemInstance result)
        {
            foreach (var slotCell in _slots)
            {
                if (slotCell.HasItem && slotCell.Item.Sign.Equals(sign))
                {
                    if (Mathf.Approximately(slotCell.Item.Amount, amount))
                    {
                        result = slotCell.Item;
                        slotCell.TrySetItem(null);
                        result = slotCell.Item.Detach(amount);
                        foreach (var listener in _listenersSlots)
                        {
                            listener.SlotEmptied(slotCell);
                        }
                        return true;
                    }
                    if (slotCell.Item.Amount > amount)
                    {
                        result = slotCell.Item.Detach(amount);
                        foreach (var listener in _listenersSlots)
                        {
                            listener.SlotReplaced(slotCell);
                        }
                        return true;
                    }
                }
            }

            foreach (var slotCell in _slots)
            {
                if (!slotCell.HasItem || !slotCell.IsContainer) continue;
                
                foreach (var item in slotCell.EnumerateItems())
                {
                    if (item.Sign.Equals(sign))
                    {
                        var pulled = slotCell.PullItem(item, amount);
                        if (pulled != null)
                        {
                            result = pulled;
                            return true;
                        }
                    }
                }
            }

            result = null;
            return false;
        }

        public IEnumerable<SlotCell> EnumerateSlots()
        {
            return _slots;
        }

        public void AddListener(ISlotsGridListener listener)
        {
            _listenersSlots.Add(listener);
        }

        public void RemoveListener(ISlotsGridListener listener)
        {
            _listenersSlots.Remove(listener);
        }
    }
}