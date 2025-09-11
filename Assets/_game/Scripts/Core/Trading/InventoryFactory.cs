using UnityEngine;
using Zenject;

namespace Core.Trading
{
    public class InventoryFactory : MonoInstaller, IFactory<string, IInventoryMasterHandler>
    {
        public IInventoryMasterHandler Create(string key)
        {
            return new Inventory(key);
        }

        public override void InstallBindings()
        {
            Container.Bind<IFactory<string, IInventoryMasterHandler>>().To<InventoryFactory>().FromInstance(this);
        }
    }
}