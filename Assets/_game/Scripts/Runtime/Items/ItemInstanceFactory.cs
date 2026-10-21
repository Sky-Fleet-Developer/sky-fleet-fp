using System.Collections.Generic;
using System.Linq;
using Core.Character.Stuff;
using Core.Configurations;
using Core.Items;
using Core.Misc;
using Core.Trading;
using Zenject;

namespace Runtime.Items
{
    public class ItemInstanceFactory : IItemInstanceFactory
    {
        [Inject] private BankSystem _bankSystem;
        [Inject] private ItemsTable _itemsTable;
        
        public ItemInstance Create(ItemSign sign, float amount)
        {
            var instance = new ItemInstance(sign, amount, _bankSystem.BindInventoryToContainerSettings, _bankSystem.UnbindInventoryToContainerSettings);
            return instance;
        }

        /*public ItemInstance CreateUniq(ItemSign sign, float amount, string guid)
        {
            if (!sign.HasTag(ItemSign.IdentifiableTag))
            {
                throw new Exception($"Error when creating item instance: Trying setup uniqId to item without Identifiable tag. Wanted id: {guid}, item sign: {sign.Id}");
            }
            
            return new ItemInstance(sign, amount, guid, _bankSystem.BindInventoryToContainerSettings, _bankSystem.UnbindInventoryToContainerSettings);
        }*/

        public ItemInstance CreateByDescription(ItemDescription description)
        {
            if (!_itemsTable.TryGetItem(description.signId, out var sign))
            {
                sign = _itemsTable.GetItem(ItemSign.Unknown);
            }
            ItemInstance instance = new ItemInstance(sign, description, _bankSystem.BindInventoryToContainerSettings, _bankSystem.UnbindInventoryToContainerSettings);
            if(instance.IsContainer && description.nestedItems is { Count: > 0 })
            {
                var inventoryWarp = _bankSystem.GetPullPutWarp(instance.ContainerKey);
                if (inventoryWarp is ISlotsGridSource slotsGridSource)
                {
                    TryAddNestedInSlots(description, slotsGridSource);
                }
                else
                {
                    foreach (var nestedItem in description.nestedItems)
                    {
                        inventoryWarp.TryPutItem(CreateByDescription(nestedItem));
                    }
                }
            }
            return instance;
        }

        public ItemDescription Describe(ItemInstance instance)
        {
            var description = new ItemDescription
            {
                signId = instance.Sign.Id,
                amount = instance.Amount,
                properties = instance.Properties.Select(x => (Property)x.Clone()).ToList(),
                nestedItems = new List<ItemDescription>()
            };
            if (!instance.IsContainer) return description;

            var inventory = _bankSystem.GetOrCreateInventory(instance.ContainerKey);
            if (inventory is ISlotsGridReadonly grid)
            {
                foreach (var slot in grid.EnumerateSlots())
                {
                    if (!slot.HasItem) continue;
                    var nested = Describe(slot.Item);
                    nested.gridSlot = slot.SlotId;
                    description.nestedItems.Add(nested);
                }
            }
            else
            {
                foreach (var item in inventory.GetItems())
                {
                    description.nestedItems.Add(Describe(item));
                }
            }

            return description;
        }

        public void Release(ItemInstance instance)
        {
            if (instance.IsContainer)
            {
                var inventory = _bankSystem.TakeInventory(instance.ContainerKey);
                if (inventory != null)
                {
                    foreach (var item in inventory.GetItems().ToList())
                    {
                        Release(item);
                    }

                    inventory.Dispose();
                }
            }

            instance.Dispose();
        }

        private void TryAddNestedInSlots(ItemDescription description, ISlotsGridSource slotsGridSource)
        {
            if (description.nestedItems is null) return;
            foreach (var nestedItem in description.nestedItems)
            {
                var nestedInstance = CreateByDescription(nestedItem);
                bool isPlaced = false;
                if (!string.IsNullOrEmpty(nestedItem.gridSlot))
                {
                    foreach (var enumerateSlot in slotsGridSource.EnumerateSlots())
                    {
                        if (enumerateSlot.SlotId.Equals(nestedItem.gridSlot))
                        {
                            enumerateSlot.TrySetItem(nestedInstance);
                            isPlaced = true;
                            break;
                        }
                    }
                }

                if (!isPlaced)
                {
                    slotsGridSource.TryPutItem(nestedInstance);
                }
            }
        }
    }
}