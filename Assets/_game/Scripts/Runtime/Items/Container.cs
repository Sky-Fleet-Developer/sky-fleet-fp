using System;
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
    public class Container : MonoBehaviour, IContainerHandler, IInteractiveObject
    {
        [Inject] private BankSystem _bankSystem;
        [Inject] private IMassAndVolumeCalculator _massAndVolumeCalculator;
        private string _inventoryKey;
        private IItemsContainerReadonly _inventory;
        private float _maxVolume;
        private float _volumeEmployed;
        private ContainerInfo _containerInfo;

        public string InventoryKey => _inventoryKey;
        public float MaxVolume => _maxVolume;
        public float VolumeRemains => _maxVolume - _volumeEmployed;
        public IItemsContainerReadonly Inventory => _inventory;
        
        public void Init(string inventoryKey, ContainerInfo containerInfo, float maxVolume)
        {
            _inventoryKey = inventoryKey;
            _containerInfo = containerInfo;
            _maxVolume = maxVolume;
            _inventory = _bankSystem.GetOrCreateInventory(_inventoryKey);
        }
        
        public PutItemResult TryPutItem(ItemInstance item)
        {
            if (_containerInfo.IsItemMatch(item, _volumeEmployed))
            {
                var result = _bankSystem.TryPutItem(_inventoryKey, item);
                if (result != PutItemResult.Fail)
                {
                    _volumeEmployed = _massAndVolumeCalculator.GetVolume(_bankSystem.GetOrCreateInventory(_inventoryKey));
                }
                return result;
            }

            return PutItemResult.Fail;
        }

        public bool TryPullItem(ItemInstance item, float amount, out ItemInstance result)
        {
            if (_bankSystem.TryPullItem(_inventoryKey, item, amount, out result))
            {
                _volumeEmployed -= result.GetVolume();
                return true;
            }
            return false;
        }

        public IEnumerable<ItemInstance> GetItems()
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

        public void Interact(InteractEventData data)
        {
            if (data.used || data.KeyModifier != KeyModifier.Up || data.IsLongPress)
            {
                return;
            }
            data.Controller.EnterHandler(this);
            data.Use();
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