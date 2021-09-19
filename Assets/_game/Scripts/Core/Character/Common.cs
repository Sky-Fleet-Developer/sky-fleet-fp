using System.Collections;
using Core.Structure.Rigging;

namespace Core.Character
{
    public interface ICharacterController
    {
        IControl AttachedControl { get; }
        IEnumerator AttachToControl(IControl control);
        IEnumerator LeaveControl(CharacterDetachhData detachData);
    }
}
