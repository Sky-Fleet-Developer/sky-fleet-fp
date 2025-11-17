using System.Collections.Generic;
using Core.Character.Stuff;
using UnityEngine;
using Zenject;

namespace Core.Trading
{
    public interface IInventoryFactory
    {
        public static readonly List<(string, string)> SlotsShortPostfix = new()
        {
            ( "_c-slots", "character_slots")
        };
        public IItemsContainerMasterHandler CreateInventory(string key);
    }
    
    public class InventoryFactory : MonoInstaller, IInventoryFactory
    {
        [Inject] StuffSlotsTable _stuffSlotsTable;
        public IItemsContainerMasterHandler CreateInventory(string key)
        {
            foreach (var valueTuple in IInventoryFactory.SlotsShortPostfix)
            {
                if (key.EndsWith(valueTuple.Item1))
                {
                    return _stuffSlotsTable.CreateGrid(key, valueTuple.Item2);
                }
            }
            return new Inventory(key);
        }

        public override void InstallBindings()
        {
            Container.Bind<IInventoryFactory>().To<InventoryFactory>().FromInstance(this);
        }
    }
}