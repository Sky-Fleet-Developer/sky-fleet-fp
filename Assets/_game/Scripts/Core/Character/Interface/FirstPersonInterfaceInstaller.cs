using System;
using Core.Patterns.State;
using UnityEngine;

namespace Core.Character.Interface
{
    public class FirstPersonInterfaceInstaller : MonoBehaviour
    {
        [SerializeField] private FirstPersonController target;
        [SerializeField] private FirstPersonInterfacePresenter presenterPrefab;
        private FirstPersonInterfacePresenter _interfacePresenter;
        private FirstPersonController.InteractionState _currentTargetState;
        public FirstPersonController.InteractionState TargetState => _currentTargetState;
        private void Start()
        {
            target.StateChanged += OnStateChanged;
            _interfacePresenter = Instantiate(presenterPrefab);
            _interfacePresenter.Init(this);
            UpdateState((FirstPersonController.InteractionState)target.CurrentState);
        }

        private void OnStateChanged()
        {
            UpdateState((FirstPersonController.InteractionState)target.CurrentState);
        }

        private void UpdateState(FirstPersonController.InteractionState state)
        {
            _currentTargetState = state;
            _interfacePresenter.UpdateState(state);
        }

        private void Update()
        {
            _interfacePresenter.RunCurrent();
        }
    }
}