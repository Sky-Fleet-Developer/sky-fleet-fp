using System.Collections.Generic;
using Core.Character.Interaction;
using Core.Items;
using Core.Trading;
using Runtime.Physic;
using UnityEngine;
using Zenject;

namespace Runtime.Items
{
    [RequireComponent(typeof(ItemObject))]
    public class PickUpItemObject : InteractiveDynamicObject, IPickUpHandler
    {
        [Inject] private BankSystem _bankSystem;
        private ItemObject _itemObject;
        protected override void Awake()
        {
            base.Awake();
            _itemObject = GetComponent<ItemObject>();
            _itemObject.OnItemInitialized.Subscribe(OnItemInit);
        }
        
        private void OnItemInit()
        {
            Rigidbody.mass = _itemObject.SourceItem.GetMass();
        }

        public void PickUpTo(IInventoryOwner inventoryOwner)
        {
            _bankSystem.TryPutItem(inventoryOwner.InventoryKey, _itemObject.SourceItem);
            _itemObject.Deconstruct();
        }
    }
}