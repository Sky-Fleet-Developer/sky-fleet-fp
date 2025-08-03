using Core.Patterns.State;
using UnityEngine;

namespace Core.Character.Interface
{
    public interface IFirstPersonInterface
    {
        void Init(FirstPersonInterfaceInstaller master);
        bool IsMatch(IState state);
        void Redraw();
        void Show();
        void Hide();
    }
}