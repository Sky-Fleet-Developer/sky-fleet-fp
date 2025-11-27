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
        private List<IInventoryStateListener> _inventoryListeners = new ();

        public string Key => _inventoryKey;
        public bool IsEmpty => !_slots.Any(x => x.HasItem);

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
                        foreach (var item in slotCell.GetItems()) yield return item;
                    }
                }
            }
        }

        public void Dispose()
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i].Dispose();
            }

            _slots = null;
            _gridId = null;
            _inventoryKey = null;
            _listenersSlots.Clear();
            _listenersSlots = null;
            _inventoryListeners.Clear();
            _inventoryListeners = null;
        }

        public void AddListener(IInventoryStateListener listener)
        {
            _inventoryListeners.Add(listener);
            foreach (var slotCell in _slots)
            {
                slotCell.AddListener(listener);
            }
        }

        public void RemoveListener(IInventoryStateListener listener)
        {
            _inventoryListeners.Remove(listener);
            foreach (var slotCell in _slots)
            {
                slotCell.RemoveListener(listener);
            }
        }

        public PutItemResult TryPutItem(ItemInstance item)
        {
            float amount = item.Amount;
            foreach (var slotCell in _slots)
            {
                if (slotCell.IsFilledFully) continue;
                
                var result = slotCell.TrySetItem(item);
                if (result != PutItemResult.Fail)
                {
                    foreach (var listener in _listenersSlots)
                    {
                        listener.SlotFilled(slotCell);
                    }
                    foreach (var listener in _inventoryListeners)
                    {
                        if (result == PutItemResult.Fully)
                        {
                            listener.ItemAdded(slotCell.Item);
                        }
                        else
                        {
                            listener.ItemMutated(slotCell.Item);
                        }
                    }
                }

                if (result == PutItemResult.Fully)
                {
                    return result;
                }
            }
            
            foreach (var slotCell in _slots)
            {
                if (slotCell.HasItem && slotCell.IsContainer)
                {
                    var result = slotCell.TryPutItem(item);
                    if (result == PutItemResult.Fully)
                    {
                        return result;
                    }
                }
            }
            return amount == item.Amount ? PutItemResult.Fail : PutItemResult.Partly;
        }
        
        public bool TryPullItem(ItemInstance item, float amount, out ItemInstance result)
        {
            foreach (var slotCell in _slots)
            {
                if (!slotCell.HasItem)
                {
                    continue;
                }

                if (slotCell.IsContainer)
                {
                    foreach (var i in slotCell.GetItems())
                    {
                        if (i.IsEqualsSignOrIdentity(item))
                        {
                            if (slotCell.TryPullItem(item, amount, out result))
                            {
                                return true; // Don't need to send info listeners, because it is already done in TryPullItem
                            }
                        }
                    }
                }
                
                if (slotCell.Item.IsEqualsSignOrIdentity(item))
                {
                    if (Mathf.Approximately(slotCell.Item.Amount, amount))
                    {
                        result = slotCell.Item;
                        slotCell.TrySetItem(null);
                        foreach (var listener in _listenersSlots)
                        {
                            listener.SlotEmptied(slotCell);
                        }

                        foreach (var listener in _inventoryListeners)
                        {
                            listener.ItemRemoved(result);
                        }
                        return true;
                    }
                    if (slotCell.Item.Amount > amount)
                    {
                        result = slotCell.Item.Split(amount);
                        foreach (var listener in _listenersSlots)
                        {
                            listener.SlotReplaced(slotCell);
                        }
                        foreach (var listener in _inventoryListeners)
                        {
                            listener.ItemMutated(result);
                        }
                        return true;
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