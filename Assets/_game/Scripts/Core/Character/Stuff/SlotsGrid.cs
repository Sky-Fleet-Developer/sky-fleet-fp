using System;
using System.Collections.Generic;
using System.Linq;
using Core.Items;
using Core.Trading;

namespace Core.Character.Stuff
{
    public class SlotsGrid : IItemsContainerMasterHandler, ISlotsGridSource, ICloneable
    {
        private string _gridId;
        private string _inventoryKey;
        private SlotCell[] _slots;
        private List<IInventoryStateListener> _listenersInventory = new ();
        private List<ISlotsGridListener> _listenersSlots = new ();

        public string Key => _inventoryKey;

        public SlotsGrid(string gridId, SlotCell[] slots)
        {
            _gridId = gridId;
            _slots = slots;
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
            _listenersInventory.Add(listener);
        }

        public void RemoveListener(IInventoryStateListener listener)
        {
            _listenersInventory.Remove(listener);
        }

        public void PutItem(ItemInstance item)
        {
            throw new NotImplementedException();
        }

        public bool TryPullItem(ItemSign sign, float amount, out ItemInstance result)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SlotCell> EnumerateSlots()
        {
            return _slots;
        }

        public ItemInstance PullItem(SlotCell slot, float amount)
        {
            throw new NotImplementedException();
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