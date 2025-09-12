using System.Collections.Generic;
using Core.Character.Interaction;
using Core.Items;
using Core.Trading;
using Runtime.Physic;
using UnityEngine;
using Zenject;

namespace Runtime.Items
{
    public class PickUpItemObject : InteractiveDynamicObject, IPickUpHandler, IItemObjectHandle
    {
        [SerializeField] private ItemObjectHandleImplementation itemObjectHandleImplementation;
        [Inject] private BankSystem _bankSystem;
        [Inject] private IItemDestructor _itemDestructor;
        
        private PickUpItemObject()
        {
            itemObjectHandleImplementation = new ItemObjectHandleImplementation(this);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            itemObjectHandleImplementation.Reset();
        } 
#endif
        public string Guid => itemObjectHandleImplementation.Guid;
        public List<string> Tags => itemObjectHandleImplementation.Tags;
        ItemInstance IItemObject.SourceItem => itemObjectHandleImplementation.SourceItem;
        void IItemObjectHandle.SetSourceItem(ItemInstance item)
        {
            itemObjectHandleImplementation.SetSourceItem(item);
            Rigidbody.mass = item.GetMass();
        }

        public void PickUpTo(IInventoryOwner inventoryOwner)
        {
            _bankSystem.TryPutItem(inventoryOwner, itemObjectHandleImplementation.SourceItem);
            _itemDestructor.Deconstruct(this);
        }
    }
}