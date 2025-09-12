using Core.Character.Interface;
using Core.Patterns.State;
using Core.Trading;
using Core.UiStructure;
using Core.UIStructure.Utilities;
using UnityEngine;
using Zenject;

namespace Runtime.Trading.UI
{
    public class PlayerInventoryInterface : FirstPersonService, ISelectionListener<ItemInstanceView>
    {
        [SerializeField] private ItemInstancesListView itemInstancesListView;
        [SerializeField] private ItemSignDescriptionView itemSignDescriptionView;
        [Inject] private BankSystem _bankSystem;
        private IInventoryReadonly _inventory;

        public override bool IsMatch(IState state)
        {
            return true;
        }

        protected override void Awake()
        {
            base.Awake();
            itemInstancesListView.SelectionHandler.AddListener(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            itemInstancesListView.SelectionHandler.RemoveListener(this);
        }

        public override void Init(FirstPersonInterfaceInstaller master)
        {
            base.Init(master);
            _inventory = _bankSystem.GetOrCreateInventory(Master.TargetState.Master);
        }

        public override void Show()
        {
            base.Show();
            RefreshView();
        }

        private void RefreshView()
        {
            itemInstancesListView.SetItems(_inventory.GetItems());
        }

        public void OnSelectionChanged(ItemInstanceView prev, ItemInstanceView next)
        {
            itemSignDescriptionView.SetData(next.Data.Sign);
        }
    }
}