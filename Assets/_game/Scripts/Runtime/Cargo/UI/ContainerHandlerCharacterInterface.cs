using System;
using Core.Character;
using Core.Character.Interaction;
using Core.Character.Interface;
using Core.Items;
using Core.Patterns.State;
using Core.UIStructure.Utilities;
using Runtime.Trading.UI;
using UnityEngine;
using Zenject;

namespace Runtime.Cargo.UI
{
    public class ContainerHandlerCharacterInterface : FirstPersonService
    {
        [SerializeField] private ItemInstancesListView itemInstancesListView;
        [SerializeField] private ItemSignDescriptionView itemSignDescriptionView;
        [Inject] private DragAndDropItemsMediator _dragAndDropItemsMediator;
        private IContainerHandler _containerHandler;
        private FirstPersonController.UIInteractionState _interactionState;

        protected override void Awake()
        {
            base.Awake();
            itemInstancesListView.OnDropContentEvent += OnDropContent;
        }

        [Inject]
        private void Inject(DiContainer diContainer)
        {
            diContainer.Inject(itemInstancesListView);
        }
        
        public override bool IsMatch(IState state)
        {
            return state is FirstPersonController.UIInteractionState { Handler: IContainerHandler };
        }
        
        public override void Init(FirstPersonInterfaceInstaller master)
        {
            base.Init(master);
            _interactionState = (FirstPersonController.UIInteractionState)Master.TargetState;
            _containerHandler = (IContainerHandler)_interactionState.Handler;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _containerHandler.RemoveListener(itemInstancesListView);
            itemInstancesListView.OnDropContentEvent -= OnDropContent;
        }

        public override void Show()
        {
            base.Show();
            itemInstancesListView.SetItems(_containerHandler.GetItems());
            _containerHandler.AddListener(itemInstancesListView);
            _dragAndDropItemsMediator.RegisterContainerView(itemInstancesListView, _containerHandler);
        }

        public override void Hide()
        {
            base.Hide();
            _containerHandler.RemoveListener(itemInstancesListView);
            _dragAndDropItemsMediator.DeregisterContainerView(itemInstancesListView);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            _interactionState.LeaveState();
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