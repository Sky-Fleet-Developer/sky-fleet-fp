using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Configurations;
using Core.Items;
using Core.Misc;
using Core.Structure;
using Core.Trading;
using Core.Utilities;
using Core.Utilities.AsyncAwaitUtil.Source;
using Cysharp.Threading.Tasks;
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
            if (item.Amount > item.Sign.GetStackSize())
            {
                throw new System.Exception("Cant create ItemObject: amount over limit");
            }
            var prefab = await _tablePrefabs.GetItem(_tableItems.GetItemPrefabGuid(item.Sign.Id)).LoadPrefab();
            return ConstructItemPrivate(item, prefab);
        }

        public void SetupInstance(IItemObjectHandle itemObjectHandle, ItemInstance item)
        {
            itemObjectHandle.SetSourceItem(item);
            var go = itemObjectHandle.transform.gameObject;
            
            if (item.IsUnique && item.Sign.TryGetProperty(ItemSign.ContainerTag, out var containerProperty))
            {
                foreach (var monoBehaviour in go.GetComponents<MonoBehaviour>())
                {
                    _container.Inject(monoBehaviour);
                }
                if (!go.TryGetComponent(out Container containerComponent))
                {
                    containerComponent = go.AddComponent<Container>();
                    _container.Inject(containerComponent);
                }
                
                ContainerInfo container = _tableItems.GetContainer(item.Sign.Id);
                containerComponent.Init(
                    item.ContainerKey,
                    container,
                    containerProperty.values[Property.Container_Volume].floatValue);
            }
            
            if (itemObjectHandle is IDynamicStructure)
            {
                _container.Inject(go.AddComponent<ContainerItemMass>());
            }
            
            var view = go.AddComponent<SlotsContainerContentView>();
            _container.Inject(view);
            view.TryInit();
            
            if (itemObjectHandle is IStructure structure)
            {
                CycleService.RegisterStructure(structure);
                structure.Init();
            }
            
            if (itemObjectHandle is IBlock block)
            {
                foreach (var propertyInfo in block.GetBlockPlayerPropertiesCached())
                {
                    if (item.TryGetProperty(propertyInfo.Name, out var property))
                    {
                        block.ApplyProperty(propertyInfo, property.values[0].stringValue);
                    }
                }
            }

            if (item.TryGetProperty(Property.PositionPropertyName, out var positionProperty))
            {
                go.transform.localPosition = new Vector3(positionProperty.values[0].floatValue, positionProperty.values[1].floatValue, positionProperty.values[2].floatValue);
            }
            if (item.TryGetProperty(Property.RotationPropertyName, out var rotationProperty))
            {
                go.transform.localRotation = new Quaternion(rotationProperty.values[0].floatValue, rotationProperty.values[1].floatValue, rotationProperty.values[2].floatValue, rotationProperty.values[3].floatValue);
            }

            if (go.TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody.mass = item.GetMass();
            }
        }

        private IItemObject ConstructItemPrivate(ItemInstance item, GameObject source)
        {
            GameObject instance;
            if (Application.isPlaying)
            {
                instance = DynamicPool.Instance.Get(source.transform).gameObject;
            }
            else
            {
                instance = Instantiate(source);
            }

            if (!instance.TryGetComponent(out IItemObjectHandle itemObjectHandle))
            {
                return null;
            }
            
            SetupInstance(itemObjectHandle, item);
            
            return itemObjectHandle;
        }

        public void Deconstruct(IItemObject itemObject)
        {
            if (itemObject is IStructure structure)
            {
                CycleService.UnregisterStructure(structure);
            }
            if (Application.isPlaying)
            {
                DynamicPool.Instance.Return(itemObject.transform);
            }
            else
            {
                DestroyImmediate(itemObject.transform.gameObject);
            }
        }
    }
}