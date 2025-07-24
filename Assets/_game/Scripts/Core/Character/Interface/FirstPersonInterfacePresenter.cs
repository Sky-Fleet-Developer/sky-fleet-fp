using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Character.Interface
{
    public class FirstPersonInterfacePresenter : MonoBehaviour
    {
        [SerializeField] private Transform container;
        private FirstPersonInterface[] _interfaces;
        private readonly List<FirstPersonInterface> _currentStates = new();
        private FirstPersonInterfaceInstaller _master;

        private void Awake()
        {
            _interfaces = GetComponentsInChildren<FirstPersonInterface>();
        }

        private void Start()
        {
            foreach (var interfaceItem in _interfaces)
            {
                interfaceItem.Hide();
            }
        }

        public void Init(FirstPersonInterfaceInstaller master)
        {
            _master = master;
        }
        public void UpdateState(FirstPersonController.InteractionState state)
        {
            for (var i = 0; i < _currentStates.Count; i++)
            {
                if (!_currentStates[i].IsMatch(state))
                {
                    _currentStates[i].Hide();
                    _currentStates.RemoveAt(i);
                }
            }
            
            foreach (var interfaceItem in _interfaces)
            {
                if (interfaceItem.IsMatch(state))
                {
                    _currentStates.Add(interfaceItem);
                    interfaceItem.Init(_master);
                    interfaceItem.Show();
                }
            }
        }

        public void RunCurrent()
        {
            for (var i = 0; i < _currentStates.Count; i++)
            {
                _currentStates[i].Refresh();   
            }
        }
    }
}