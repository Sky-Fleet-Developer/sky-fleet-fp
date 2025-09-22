using Core.Character.Interface;
using Core.Patterns.State;
using Core.Trading;
using Core.UiStructure;
using Core.UIStructure.Utilities;
using UnityEngine;
using Zenject;

namespace Runtime.Trading.UI
{
    public class PlayerInventoryInterface : FirstPersonService, IMultipleSelectionListener<ItemInstanceView>
    {
        [SerializeField] private ItemInstancesListView itemInstancesListView;
        [SerializeField] private ItemSignDescriptionView itemSignDescriptionView;
        [Inject] private BankSystem _bankSystem;
        [Inject] private DragAndDropItemsMediator _dragAndDropItemsMediator;
        private IItemsContainerReadonly _inventory;
        private ItemInstanceView _selected;

        [Inject]
        private void Inject(DiContainer diContainer)
        {
            diContainer.Inject(itemInstancesListView);
        }

        public override bool IsMatch(IState state)
        {
            return true;
        }

        protected override void Awake()
        {
            base.Awake();
            itemInstancesListView.SelectionHandler.AddListener(this);
            itemInstancesListView.OnDropContentEvent += OnDropContent;
        }

        protected override void OnDestroy()
        {
            _inventory?.RemoveListener(itemInstancesListView);
            itemInstancesListView.SelectionHandler.RemoveListener(this);
            itemInstancesListView.OnDropContentEvent -= OnDropContent;
            base.OnDestroy();
        }

        public override void Init(FirstPersonInterfaceInstaller master)
        {
            base.Init(master);
            _inventory = _bankSystem.GetOrCreateInventory(Master.TargetState.Master);
            _inventory.AddListener(itemInstancesListView);
        }

        public override void Show()
        {
            base.Show();
            RefreshView();
            _dragAndDropItemsMediator.RegisterContainerView(itemInstancesListView, Master.TargetState.Master);
        }

        public override void Hide()
        {
            _inventory?.RemoveListener(itemInstancesListView);
            base.Hide();
        }

        private void RefreshView()
        {
            itemInstancesListView.SetItems(_inventory.GetItems());
        }

        public void OnSelected(ItemInstanceView target) { }

        public void OnDeselected(ItemInstanceView target)
        {
            if (_selected == target)
            {
                itemSignDescriptionView.Clear();
            }
        }
        public void OnFinalSelected(ItemInstanceView target)
        {
            _selected = target;
            itemSignDescriptionView.SetData(target.Data.Sign);
        }
        
        private void OnDropContent(DropEventData eventData)
        {
            foreach (IDraggable draggable in eventData.Content)
            {
                if (ReferenceEquals(draggable.MyContainer, this))
                {
                    return;
                }
            }
            eventData.Use();
            _dragAndDropItemsMediator.DragAndDropPreformed(eventData.Source, itemInstancesListView, eventData.Content);
        }
    }
}