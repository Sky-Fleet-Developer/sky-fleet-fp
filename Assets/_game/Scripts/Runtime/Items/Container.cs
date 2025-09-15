using System.Collections.Generic;
using Core.Character.Interaction;
using Core.Configurations;
using Core.Items;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Items
{
    public class Container : MonoBehaviour, IContainerHandler
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
    }
}