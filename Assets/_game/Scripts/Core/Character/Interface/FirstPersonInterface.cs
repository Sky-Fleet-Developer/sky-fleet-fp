using Core.Patterns.State;
using UnityEngine;

namespace Core.Character.Interface
{
    public abstract class FirstPersonInterface : MonoBehaviour
    {
        public FirstPersonInterfaceInstaller Master { get; private set; }

        public void Init(FirstPersonInterfaceInstaller master)
        {
            Master = master;
        }

        public abstract bool IsMatch(IState state);
        public abstract void Refresh();

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}