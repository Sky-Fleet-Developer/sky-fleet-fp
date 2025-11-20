using System.Collections.Generic;
using Core.Character.Interface;
using Core.Character.Stuff;
using Core.Items;
using Core.Patterns.State;
using Core.Trading;
using Core.UiStructure;
using Core.UIStructure.Utilities;
using Core.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Runtime.Trading.UI
{
    public class PlayerSlotsInterface : FirstPersonService, ISlotsGridListener
    {
        //[SerializeField] private ItemInstancesListView itemInstancesListView;
        [SerializeField] private RectTransform slotsContainer;
        [SerializeField] private RectTransform slotContainersContainer;
        [Inject] private BankSystem _bankSystem;
        private ISlotsGridSource _inventory;
        private ItemInstanceView _selected;
        private Dictionary<string, SlotCellView> _slots = new();
        private SlotCellView _slotSource;
        private ThingsListView<ItemInstance> _slotContainerSource;
        
        [Inject]
        private void Inject(DiContainer diContainer)
        {
            //diContainer.Inject(itemInstancesListView);
        }

        public override bool IsMatch(IState state)
        {
            return true;
        }

        protected override void Awake()
        {
            base.Awake();
            _slotSource = slotsContainer.GetComponentInChildren<SlotCellView>();
            DynamicPool.Instance.Return(_slotSource);
            _slotContainerSource = slotContainersContainer.GetComponentInChildren<ThingsListView<ItemInstance>>();
            DynamicPool.Instance.Return(_slotContainerSource);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void Init(FirstPersonInterfaceInstaller master)
        {
            base.Init(master);
            _inventory = (ISlotsGridSource)_bankSystem.GetOrCreateInventory(((IInventoryOwner)Master.TargetState.Master).InventoryKey);
            _inventory.AddListener(this);
            foreach (var slot in _inventory.EnumerateSlots())
            {
                if (!_slots.TryGetValue(slot.SlotId, out var slotView))
                {
                    slotView = DynamicPool.Instance.Get(_slotSource, slotsContainer);
                    _slots[slot.SlotId] = slotView;
                }
                slotView.Init(slot.SlotId, _slotContainerSource, slotContainersContainer);
            }
        }

        public override void Show()
        {
            base.Show();
            RefreshView();
        }

        public override void Hide()
        {
            _inventory?.RemoveListener(this);
            base.Hide();
        }

        private void RefreshView()
        {
            foreach (var slot in _slots.Values)
            {
                if (slot.Cell != null)
                {
                    slot.Refresh();
                }
            }
        }
        
        public void SlotFilled(SlotCell slot)
        {
            RefreshSlotState(slot);
        }

        public void SlotReplaced(SlotCell slot)
        {
            RefreshSlotState(slot);
        }

        
        public void SlotEmptied(SlotCell slot)
        {
            RefreshSlotState(slot);
        }

        private void RefreshSlotState(SlotCell slot)
        {
            _slots[slot.SlotId].Set(slot);
        }
    }
}