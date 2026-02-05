using System.Collections.Generic;
using Core.Character.Stuff;
using Core.Configurations;
using UnityEngine;
using Zenject;

namespace Core.Trading
{
    public interface IInventoryFactory
    {

        public IItemsContainerMasterHandler CreateInventory(string key);
    }
    
    public class InventoryFactory : MonoBehaviour, IMyInstaller, IInventoryFactory
    {
        [Inject] private StuffSlotsTable _stuffSlotsTable;
        [Inject] private BankSystem _bankSystem;
        
        public IItemsContainerMasterHandler CreateInventory(string key)
        {
            if (_stuffSlotsTable.TryCreateGrid(key, out var grid))
            {
                return grid;
            }

            var inventory = new Inventory(key, _bankSystem);
            
            return inventory;
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<IInventoryFactory>().To<InventoryFactory>().FromInstance(this);
        }
    }
}