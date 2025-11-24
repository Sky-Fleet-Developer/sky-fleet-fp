using System;
using System.Collections.Generic;
using Core.Items;
using Core.Trading;
using Core.UIStructure.Utilities;
using UnityEngine;
using Zenject;

namespace Core.Character.Interface
{
    public class DragAndDropItemsMediator : MonoBehaviour, IMyInstaller
    {
        [Inject] private BankSystem _bankSystem;
        private Dictionary<IDragAndDropContainer, DnDBindings> _inventories = new ();
        public struct DnDBindings
        {
            private BankSystem _bankSystem;
            private string _inventoryKey;
            private IPullPutItem _itemsContainer;
            private Func<ItemInstance, float, ItemInstance> _pullItem;
            private Func<ItemInstance, PutItemResult> _putItem;
            
            public DnDBindings(IPullPutItem container) : this()
            {
                _itemsContainer = container;
            }

            public DnDBindings(string inventoryKey, BankSystem bankSystem) : this()
            {
                _inventoryKey = inventoryKey;
                _bankSystem = bankSystem;
            }

            public DnDBindings(Func<ItemInstance, float, ItemInstance> pullItem, Func<ItemInstance, PutItemResult> putItem) : this()
            {
                _pullItem = pullItem;
                _putItem = putItem;
            }
            
            public bool TryPullItem(ItemInstance item, float amount, out ItemInstance result)
            {
                if (!string.IsNullOrEmpty(_inventoryKey))
                {
                    return _bankSystem.TryPullItem(_inventoryKey, item, amount, out result);
                }
                if (_itemsContainer != null)
                {
                    return _itemsContainer.TryPullItem(item, amount, out result);
                }
                result = _pullItem?.Invoke(item, amount);
                return result != null;
            }

            public PutItemResult TryPutItem(ItemInstance item)
            {
                if (!string.IsNullOrEmpty(_inventoryKey))
                {
                    return _bankSystem.TryPutItem(_inventoryKey, item);
                }
                if (_itemsContainer != null)
                {
                    return _itemsContainer.TryPutItem(item);
                }
                return _putItem?.Invoke(item)??PutItemResult.Fail;
            }
            
            
        }

        public void RegisterContainerView(IDragAndDropContainer dragAndDropContainerView, string inventoryKey)
        {
            _inventories[dragAndDropContainerView] = new DnDBindings(inventoryKey, _bankSystem);
        }

        public void RegisterContainerView(IDragAndDropContainer dragAndDropContainerView, IPullPutItem itemsContainer)
        {
            _inventories[dragAndDropContainerView] = new DnDBindings(itemsContainer);
        }
        
        /// <param name="dragAndDropContainerView">UI View the items container</param>
        /// <param name="pullItem">method to pull item from entire container</param>
        /// <param name="putItem">method to put item to entire container</param>
        public void RegisterContainerView(IDragAndDropContainer dragAndDropContainerView, Func<ItemInstance, float, ItemInstance> pullItem, Func<ItemInstance, PutItemResult> putItem)
        {
            _inventories[dragAndDropContainerView] = new DnDBindings(pullItem, putItem);
        }
        
        public void UnregisterContainerView(IDragAndDropContainer dragAndDropContainerView)
        {
            _inventories.Remove(dragAndDropContainerView);
        }

        public void DragAndDropPreformed(DropEventData eventData, IDragAndDropContainer destination)
        {
            DragAndDropPreformed(eventData.Source, destination, eventData.Content);
            if (eventData.Content.Count == 0)
            {
                eventData.Use();
            }
        }
        public void DragAndDropPreformed(IDragAndDropContainer source, IDragAndDropContainer destination, List<IDraggableItem> items)
        {
            var sourceInventory = _inventories[source];
            var destinationInventory = _inventories[destination];

            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = (ItemInstance)items[i];
                if (!sourceInventory.TryPullItem(item, item.Amount, out item)) continue;
                if (destinationInventory.TryPutItem(item) == PutItemResult.Fully)
                {
                    items.RemoveAt(i);
                    continue;
                }
                
                if (item.IsEmpty)
                {
                    items.RemoveAt(i);
                }
                else
                {
                    sourceInventory.TryPutItem(item);
                }
            }
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<DragAndDropItemsMediator>().FromInstance(this);
        }
    }
}