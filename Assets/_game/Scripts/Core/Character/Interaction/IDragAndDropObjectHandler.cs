using UnityEngine;

namespace Core.Character.Interaction
{
    public interface IDragAndDropObjectHandler : ICharacterHandler
    {
        Rigidbody Rigidbody { get; }
        bool MoveTransitional { get; }
        void StartPull();
        bool ProcessPull(float tension);
    }
}