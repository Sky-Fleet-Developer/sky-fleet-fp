using System.Collections.Generic;
using Core.Items;
using Core.Trading;
using Core.UIStructure.Utilities;
using UnityEngine;
using Zenject;

namespace Core.Character.Interface
{
    public class DragAndDropItemsMediator : MonoBehaviour, IInstallerWithContainer
    {
        [Inject] private BankSystem _bankSystem;
        private Dictionary<IDragAndDropContainer, IInventoryOwner> _inventories = new ();

        public void RegisterContainerView(IDragAndDropContainer dragAndDropContainerView,
            IInventoryOwner inventoryOwner)
        {
            _inventories[dragAndDropContainerView] = inventoryOwner;
        }
        
        public void DeregisterContainerView(IDragAndDropContainer dragAndDropContainerView)
        {
            _inventories.Remove(dragAndDropContainerView);
        }

        public void DragAndDropPreformed(IDragAndDropContainer source, IDragAndDropContainer destination,
            IReadOnlyList<IDraggable> items)
        {
            var sourceInventory = _inventories[source];
            var destinationInventory = _inventories[destination];
            
            foreach (var draggable in items)
            {
                var item = (ItemInstance)draggable.Entity;
                if (_bankSystem.TryPullItem(sourceInventory.InventoryKey, item.Sign, item.Amount, out item))
                {
                    _bankSystem.TryPutItem(destinationInventory.InventoryKey, item);
                }
            }
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<DragAndDropItemsMediator>().FromInstance(this);
        }
    }
}