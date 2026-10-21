using System;
using System.Collections.Generic;
using Core.Items;
using Core.Narrative;
using Core.Trading;
using QnE;
using UnityEngine;

namespace Runtime.Narrative
{
    /// <summary>
    /// Publishes how many units of an item an inventory holds, but only for the pairs an active stage asked for with
    /// <c>WatchInventory(inventoryKey, itemId)</c>. The watch ends when the stage exits.
    /// </summary>
    public class InventoryFacts
    {
        private readonly QnEService _service;
        private readonly BankSystem _bank;
        private readonly List<Watch> _watches = new();

        public InventoryFacts(QnEService service, BankSystem bank)
        {
            _service = service;
            _bank = bank;
        }

        public int WatchCount => _watches.Count;

        public void RegisterCommands()
        {
            _service.Commands.Register("WatchInventory", (context, args) =>
            {
                var watch = new Watch(this, args[0], args[1]);
                _watches.Add(watch);
                watch.Attach();
                return watch;
            });
        }

        /// <summary>
        /// An empty inventory can be dissolved by the bank and created again later. Re-attaches the watches whose
        /// inventory is no longer alive. Cheap enough to call about once a second.
        /// </summary>
        public void Sync()
        {
            for (var i = 0; i < _watches.Count; i++) _watches[i].Attach();
        }

        private sealed class Watch : IInventoryStateListener, IDisposable
        {
            private readonly InventoryFacts _owner;
            private readonly string _inventoryKey;
            private readonly string _itemId;
            private readonly string _factKey;
            private IItemsContainerReadonly _inventory;

            public Watch(InventoryFacts owner, string inventoryKey, string itemId)
            {
                _owner = owner;
                _inventoryKey = inventoryKey;
                _itemId = itemId;
                _factKey = NarrativeKeys.Inventory(inventoryKey, itemId);
            }

            public void Attach()
            {
                if (_inventory is { IsAlive: true }) return;
                _inventory = _owner._bank.GetOrCreateInventory(_inventoryKey);
                _inventory.AddListener(this);
                Publish();
            }

            public void ItemAdded(ItemInstance item) => OnItemChanged(item);
            public void ItemMutated(ItemInstance item) => OnItemChanged(item);
            public void ItemRemoved(ItemInstance item) => OnItemChanged(item);

            private void OnItemChanged(ItemInstance item)
            {
                if (item.Sign != null && item.Sign.Id == _itemId) Publish();
            }

            private void Publish()
            {
                float total = 0f;
                foreach (var item in _inventory.GetItems())
                {
                    if (item.Sign != null && item.Sign.Id == _itemId) total += item.Amount;
                }

                _owner._service.Blackboard.Set(_factKey, Mathf.FloorToInt(total + 0.0001f));
            }

            public void Dispose()
            {
                if (_inventory is { IsAlive: true }) _inventory.RemoveListener(this);
                _inventory = null;
                _owner._watches.Remove(this);
            }
        }
    }
}
