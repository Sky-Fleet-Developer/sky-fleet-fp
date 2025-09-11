using System;
using Core.Patterns.State;
using UnityEngine;

namespace Core.Character.Interface
{
    public abstract class FirstPersonInterfaceBase : MonoBehaviour, IFirstPersonInterface
    {
        protected FirstPersonInterfaceInstaller Master;

        public virtual void Init(FirstPersonInterfaceInstaller master)
        {
            Master = master;
        }

        public abstract bool IsMatch(IState state);

        private FirstPersonInterfaceState _state;
        public FirstPersonInterfaceState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    OnStateChanged?.Invoke(this, value);
                    _state = value;
                }
            }
        }

        public virtual void Show()
        {
            State = FirstPersonInterfaceState.Open;
        }

        public virtual void Hide()
        {
            State = FirstPersonInterfaceState.Close;
        }

        private void OnDisable()
        {
            if (_state == FirstPersonInterfaceState.Open)
            {
                State = FirstPersonInterfaceState.Close;
            }
        }
        
        public event Action<IFirstPersonInterface, FirstPersonInterfaceState> OnStateChanged;
    }
}