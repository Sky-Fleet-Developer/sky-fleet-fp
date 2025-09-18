using System;
using Core.Character;
using Core.Character.Interaction;
using Core.Character.Interface;
using Core.Patterns.State;
using Runtime.Trading.UI;
using UnityEngine;
using Zenject;

namespace Runtime.Cargo.UI
{
    public class ContainerHandlerCharacterInterface : FirstPersonInterfaceBase
    {
        [SerializeField] private ItemInstancesListView itemInstancesListView;
        [SerializeField] private ItemSignDescriptionView itemSignDescriptionView;
        private IContainerHandler _containerHandler;
        private FirstPersonController.UIInteractionState _interactionState;

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

        private void OnDestroy()
        {
            _containerHandler.RemoveListener(itemInstancesListView);
        }

        public override void Show()
        {
            base.Show();
            itemInstancesListView.SetItems(_containerHandler.GetItems());
            _containerHandler.AddListener(itemInstancesListView);
        }

        public override void Hide()
        {
            base.Hide();
            _containerHandler.RemoveListener(itemInstancesListView);
        }
    }
}