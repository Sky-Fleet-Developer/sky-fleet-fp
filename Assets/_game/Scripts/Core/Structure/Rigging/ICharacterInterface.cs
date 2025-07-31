using System.Collections.Generic;
using Core.Character;
using Core.Character.Interaction;

namespace Core.Structure.Rigging
{
    public interface ICharacterInterface : IInteractiveBlock
    {
        int GetAttachedControllersCount { get; }
        CharacterAttachData GetAttachData();
        void ReadInput();
        void LeaveControl(ICharacterController controller);
        IEnumerable<ICharacterHandler> GetHandlers();
    }
}