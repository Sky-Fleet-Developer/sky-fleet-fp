using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Configurations;
using Core.Items;
using Core.Trading;
using Core.Utilities;
using UnityEngine;
using Zenject;

namespace Runtime.Items
{
    public class ItemObjectFactory : MonoBehaviour, IMyInstaller, IItemObjectFactory
    {
        [Inject(Optional = true)] private TablePrefabs _tablePrefabs;
        [Inject(Optional = true)] private ItemsTable _tableItems;
        private DiContainer _container;

        public void InstallBindings(DiContainer container)
        {
            _container = container;
            _container.Bind<IItemObjectFactory>().To<ItemObjectFactory>().FromInstance(this);
        }

        public async Task<List<IItemObject>> Create(ItemInstance item)
        {
            var prefab = await _tablePrefabs.GetItem(_tableItems.GetItemPrefabGuid(item.Sign.Id)).LoadPrefab();
            List<IItemObject> instances = new List<IItemObject>((int)item.Amount);
            foreach (var makeInstance in item.DetachStacks(item.Sign.GetStackSize()))
            {
                var instance = ConstructItemPrivate(makeInstance, prefab);
                instances.Add(instance);
            }

            return instances;
        }

        public async Task<IItemObject> CreateSingle(ItemInstance item)
        {
            var prefab = await _tablePrefabs.GetItem(_tableItems.GetItemPrefabGuid(item.Sign.Id)).LoadPrefab();
            return ConstructItemPrivate(item, prefab);
        }

        private IItemObject ConstructItemPrivate(ItemInstance item, GameObject source)
        {
            var instance = DynamicPool.Instance.Get(source.transform).gameObject;
            if (!instance.TryGetComponent(out IItemObjectHandle itemObjectHandle))
            {
                return null;
            }
            itemObjectHandle.SetSourceItem(item);
            
            if (item.Sign.TryGetProperty(ItemSign.ContainerTag, out var containerProperty) && item.TryGetProperty(ItemSign.IdentifiableTag, out var identifiableProperty))
            {
                foreach (var monoBehaviour in instance.GetComponents<MonoBehaviour>())
                {
                    _container.Inject(monoBehaviour);
                }
                if (!instance.TryGetComponent(out Container containerComponent))
                {
                    containerComponent = instance.AddComponent<Container>();
                    _container.Inject(containerComponent);
                }
                ContainerInfo container = _tableItems.GetContainer(item.Sign.Id);
                containerComponent.Init(
                    identifiableProperty.values[ItemProperty.IdentifiableInstance_Identifier].stringValue,
                    container,
                    containerProperty.values[ItemProperty.Container_Volume].floatValue);
                instance.AddComponent<ContainerItemMass>();
            }

            if (instance.TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody.mass = item.GetMass();
            }
            return itemObjectHandle;
        }

        public void Deconstruct(IItemObject itemObject)
        {
            DynamicPool.Instance.Return(itemObject.transform);
        }
    }
}