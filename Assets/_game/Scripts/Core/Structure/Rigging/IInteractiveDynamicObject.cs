using UnityEngine;

namespace Core.Structure.Rigging
{
    public interface IInteractiveDynamicObject : IInteractiveObject
    {
        Rigidbody Rigidbody { get; }
        bool MoveTransitional { get; }
        void StartPull();
        bool ProcessPull(float tension);
    }
}