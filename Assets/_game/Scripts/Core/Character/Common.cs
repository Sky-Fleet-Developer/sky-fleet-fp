using System.Collections;
using Core.Structure.Rigging;

namespace Core.Character
{
    public interface ICharacterController
    {
        ICharacterInterface AttachedICharacterInterface { get; }
        IEnumerator AttachToControl(ICharacterInterface iCharacterInterface);
        IEnumerator LeaveControl(CharacterDetachData detachData);
    }
}
