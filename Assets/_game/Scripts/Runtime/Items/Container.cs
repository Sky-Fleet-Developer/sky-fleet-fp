using System.Collections.Generic;
using Core.Character;
using Core.Character.Interaction;
using Core.Configurations;
using Core.Game;
using Core.Items;
using Core.Structure.Rigging;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Items
{
    [RequireComponent(typeof(Container), typeof (DynamicWorldObject))]
    public class ContainerItemMass : MonoBehaviour, IInventoryStateListener, IMassModifier
    {
        private DynamicWorldObject _dynamicWorldObject;
        private Container _container;
        private float _mass;
        private IMassCombinator _massCombinator;
        public float Mass => _mass;

        private void Awake()
        {
            _dynamicWorldObject = GetComponent<DynamicWorldObject>();
            _container = GetComponent<Container>();
            _container.AddListener(this);
        }

        private void OnItemInit()
        {
            RefreshMass();
        }

        private void RefreshMass()
        {
            _mass = _container.GetMass();
            _massCombinator?.SetMassDirty(this);
        }

        public void ItemAdded(ItemInstance item)
        {
            OnMassDirty();
        }

        public void ItemMutated(ItemInstance item)
        {
            OnMassDirty();
        }

        public void ItemRemoved(ItemInstance item)
        {
            OnMassDirty();
        }

        private void OnMassDirty()
        {
            RefreshMass();
        }

        public void AddListener(IMassCombinator massCombinator)
        {
            _massCombinator = massCombinator;
        }

        public void RemoveListener(IMassCombinator massCombinator)
        {
            if (_massCombinator == massCombinator)
            {
                _massCombinator = null;
            }
        }
    }
    public class Container : MonoBehaviour, IContainerHandler, IInteractiveObject
    {
        [Inject] private BankSystem _bankSystem;
        private string _inventoryKey;
        private IItemsContainerReadonly _inventory;
        private float _maxVolume;
        private float _volumeEmployed;
        private ContainerInfo _containerInfo;

        public string InventoryKey => _inventoryKey;
        public float MaxVolume => _maxVolume;
        public float VolumeRemains => _maxVolume - _volumeEmployed;
        
        public void Init(string inventoryKey, ContainerInfo containerInfo, float maxVolume)
        {
            _inventoryKey = inventoryKey;
            _containerInfo = containerInfo;
            _maxVolume = maxVolume;
            _inventory = _bankSystem.GetOrCreateInventory(this);
        }
        
        public bool TryPutItem(ItemInstance item)
        {
            if (_containerInfo.IsItemMatch(item, _volumeEmployed))
            {
                if (_bankSystem.TryPutItem(this, item))
                {
                    _volumeEmployed += item.GetVolume();
                    return true;
                }
            }

            return false;
        }

        public bool TryPullItem(ItemSign sign, float amount, out ItemInstance result)
        {
            if (_bankSystem.TryPullItem(this, sign, amount, out result))
            {
                _volumeEmployed -= result.GetVolume();
                return true;
            }
            return false;
        }

        public IReadOnlyList<ItemInstance> GetItems()
        {
            return _inventory.GetItems();
        }

        public void AddListener(IInventoryStateListener listener)
        {
            _inventory.AddListener(listener);
        }

        public void RemoveListener(IInventoryStateListener listener)
        {
            _inventory.RemoveListener(listener);
        }

        public bool EnableInteraction => true;
        public Transform Root => transform;
        public bool RequestInteractive(ICharacterController character, out string data)
        {
            data = string.Empty;
            return true;
        }

        public float GetMass()
        {
            float mass = 0;
            foreach (var itemInstance in GetItems())
            {
                mass += itemInstance.GetMass();
            }
            return mass;
        }
    }
}