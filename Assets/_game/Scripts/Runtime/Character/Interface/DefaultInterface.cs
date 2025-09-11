using System;
using System.Collections.Generic;
using Core.Character;
using Core.Character.Interface;
using Core.Patterns.State;
using Core.UIStructure;
using Runtime.Trading.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Runtime.Character.Interface
{
    public class DefaultInterface : FirstPersonInterfaceBase
    {
        [SerializeField] private InputAction showInventoryBinding;
        [Inject] private ServiceIssue _serviceIssue;
        [Inject] private DiContainer _diContainer;

        private List<IFirstPersonInterface> _children = new ();
        private PlayerInventoryInterface _playerInventoryInterface;
        private void Awake()
        {
            showInventoryBinding.performed += ToggleInventory;
        }
         
        private void OnDestroy()
        {
            showInventoryBinding.performed -= ToggleInventory;
        }
        
        private void ToggleInventory(InputAction.CallbackContext context)
        {
            if (_playerInventoryInterface != null)
            {
                _playerInventoryInterface.Hide();
                return;
            }
            _playerInventoryInterface = (PlayerInventoryInterface)_serviceIssue.CreateService(typeof(PlayerInventoryInterface), typeof(FramedWindow), Window.LayoutType.None);
            _diContainer.Inject(_playerInventoryInterface);
            ShowInterface(_playerInventoryInterface);
        }

        private void ShowInterface(IFirstPersonInterface instance)
        {
            instance.Show();
            instance.OnStateChanged += InstanceOnOnStateChanged;
            _children.Add(instance);
        }

        private void InstanceOnOnStateChanged(IFirstPersonInterface instance, FirstPersonInterfaceState state)
        {
            if (state == FirstPersonInterfaceState.Close)
            {
                _children.Remove(instance);
                instance.OnStateChanged -= InstanceOnOnStateChanged;
                if (ReferenceEquals(instance, _playerInventoryInterface))
                {
                    _playerInventoryInterface = null;
                }
            }
        }

        public override bool IsMatch(IState state)
        {
            return true;
        }

        public override void Show()
        {
            base.Show();
            showInventoryBinding.Enable();
        }

        public override void Hide()
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                _children[i].Hide();
            }
            _children.Clear();
            base.Hide();
            showInventoryBinding.Disable();
        }
    }
}