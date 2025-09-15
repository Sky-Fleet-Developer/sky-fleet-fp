using UnityEngine;
using Zenject;

namespace Core.Trading
{
    public class InventoryFactory : MonoInstaller, IFactory<string, IItemsContainerMasterHandler>
    {
        public IItemsContainerMasterHandler Create(string key)
        {
            return new Inventory(key);
        }

        public override void InstallBindings()
        {
            Container.Bind<IFactory<string, IItemsContainerMasterHandler>>().To<InventoryFactory>().FromInstance(this);
        }
    }
}