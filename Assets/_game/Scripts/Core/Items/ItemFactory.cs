using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Core.Items
{
    public class ItemFactory : MonoInstaller, IFactory<ItemInstance, Task<List<GameObject>>>
    {
        [Inject(Optional = true)] private TablePrefabs _tablePrefabs;
        [Inject(Optional = true)] private ItemsTable _tableItems;
        
        public override void InstallBindings()
        {
            Container.Bind<IFactory<ItemInstance, Task<List<GameObject>>>>().To<ItemFactory>().FromInstance(this);
        }

        public async Task<List<GameObject>> Create(ItemInstance item)
        {
            var prefab = await _tablePrefabs.GetItem(_tableItems.GetItemPrefabGuid(item.Sign.Id)).LoadPrefab();
            List<GameObject> instances = new List<GameObject>((int)item.Amount);
            foreach (var makeInstance in item.DetachStacks(item.Sign.GetStackSize()))
            {
                var instance = ConstructItemPrivate(makeInstance, prefab);
                instances.Add(instance);
            }

            return instances;
        }

        private GameObject ConstructItemPrivate(ItemInstance item, GameObject source)
        {
            var instance = Instantiate(source);
            if (instance.TryGetComponent(out IItemObjectHandle itemObjectHandle))
            {
                itemObjectHandle.SetSourceItem(item);
            }

            return instance;
        }
    }
}