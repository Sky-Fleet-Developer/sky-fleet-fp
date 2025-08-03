using Core.Character;
using UnityEngine;

namespace Core.Structure.Rigging
{
    public interface IInteractiveObject
    {
        bool EnableInteraction { get; }
        Transform Root { get; }
        bool RequestInteractive(ICharacterController character, out string data);
    }
}