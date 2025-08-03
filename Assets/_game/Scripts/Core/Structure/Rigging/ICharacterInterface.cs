using System.Collections.Generic;
using Core.Character;
using Core.Character.Interaction;

namespace Core.Structure.Rigging
{
    public interface ICharacterInterface : IInteractiveBlock, ICharacterHandler
    {
        int GetAttachedControllersCount { get; }
        CharacterAttachData GetAttachData();
        CharacterDetachData GetDetachData();
        void ReadInput();
        void OnCharacterEnter(ICharacterController controller);
        void OnCharacterLeave(ICharacterController controller);
    }
}