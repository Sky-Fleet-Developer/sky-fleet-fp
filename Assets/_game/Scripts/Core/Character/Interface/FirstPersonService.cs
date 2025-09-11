using System;
using Core.Patterns.State;
using Core.UiStructure;

namespace Core.Character.Interface
{
    public abstract class FirstPersonService : Service, IFirstPersonInterface
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
            Window.Open();
            State = FirstPersonInterfaceState.Open;
        }

        public virtual void Hide()
        {
            Window.Close();
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