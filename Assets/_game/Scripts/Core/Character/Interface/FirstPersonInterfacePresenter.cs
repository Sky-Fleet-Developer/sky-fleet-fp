using System;
using System.Collections.Generic;
using System.Linq;
using Core.UiStructure;
using Core.UIStructure;
using Core.Utilities;
using UnityEngine;
using Zenject;

namespace Core.Character.Interface
{
    public class FirstPersonInterfacePresenter : MonoBehaviour
    {
        [SerializeField] private Transform container;
        [SerializeField] private GameObject[] interfacesPrefabs;
        private List<IFirstPersonInterface> _childInterfaces;
        private List<IFirstPersonInterface> _prefabInterfaces = new ();
        private readonly List<IFirstPersonInterface> _currentStates = new();
        private readonly List<Component> _fromPool = new();
        private readonly List<IFirstPersonInterface> _services = new();
        private FirstPersonInterfaceInstaller _master;
        [Inject] private ServiceIssue _serviceIssue;
        [Inject] private DiContainer _diContainer;

        [Inject]
        private void Inject(DiContainer diContainer)
        {
            _diContainer = diContainer;
            if (_childInterfaces != null)
            {
                foreach (var firstPersonInterface in _childInterfaces)
                {
                    _diContainer.Inject(firstPersonInterface);
                }
            }
        }
        
        public void Init(FirstPersonInterfaceInstaller master)
        {
            _master = master;
            _childInterfaces = GetComponentsInChildren<IFirstPersonInterface>().ToList();
            foreach (var firstPersonInterface in _childInterfaces)
            {
                _diContainer?.Inject(firstPersonInterface);
                firstPersonInterface.Hide();
            }
            foreach (var prefab in interfacesPrefabs)
            {
                if (prefab.TryGetComponent(out IFirstPersonInterface firstPersonInterface))
                {
                    if (firstPersonInterface is Service service)
                    {
                        _serviceIssue.AddService(service);
                        _services.Add(firstPersonInterface);
                    }
                    else
                    {
                        _prefabInterfaces.Add(firstPersonInterface);
                    }
                }
            }
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
            
            foreach (var interfaceItem in _childInterfaces)
            {
                if (interfaceItem.IsMatch(state))
                {
                    _currentStates.Add(interfaceItem);
                    interfaceItem.Init(_master);
                    interfaceItem.Show();
                }
            }

            foreach (var interfaceItem in _prefabInterfaces)
            {
                if (interfaceItem.IsMatch(state))
                {
                    var instance = DynamicPool.Instance.Get((Component)interfaceItem);
                    _diContainer.Inject(instance);
                    var instanceAsInterface = (IFirstPersonInterface)instance;
                    _currentStates.Add(instanceAsInterface);
                    _fromPool.Add(instance);
                    instanceAsInterface.Init(_master);
                    instanceAsInterface.Show();
                }
            }

            foreach (var interfaceItem in _services)
            {
                if (interfaceItem.IsMatch(state))
                {
                    var instance = (IFirstPersonInterface)_serviceIssue.CreateService(interfaceItem.GetType(), typeof(FramedWindow), Window.LayoutType.None);
                    _diContainer.Inject(instance);
                    instance.Init(_master);
                    instance.Show();
                }
            }
        }
    }
}