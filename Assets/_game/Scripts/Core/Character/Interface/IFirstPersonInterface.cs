using System;
using Core.Patterns.State;
using UnityEngine;

namespace Core.Character.Interface
{
    public enum FirstPersonInterfaceState
    {
        Open,
        Close
    }
    public interface IFirstPersonInterface
    {
        void Init(FirstPersonInterfaceInstaller master);
        bool IsMatch(IState state);
        void Show();
        void Hide();
        FirstPersonInterfaceState State { get; }
        event Action<IFirstPersonInterface, FirstPersonInterfaceState> OnStateChanged;
    }
}