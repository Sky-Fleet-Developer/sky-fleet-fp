using Core;
using Core.Items;
using UnityEngine;
using Zenject;

namespace Runtime.Items
{
    public class ItemInstanceFactoryInstaller : MonoBehaviour, IMyInstaller
    {
        private ItemInstanceFactory _itemInstanceFactory;

        [Inject]
        private void Inject(DiContainer container)
        {
            container.Inject(_itemInstanceFactory);
        }
        public void InstallBindings(DiContainer container)
        {
            _itemInstanceFactory = new ItemInstanceFactory();
            container.Bind<IItemInstanceFactory>().To<ItemInstanceFactory>().FromInstance(_itemInstanceFactory);
        }
    }
}