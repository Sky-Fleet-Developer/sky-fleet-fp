using System.Collections;
using Core.Structure.Rigging;

namespace Core.Character
{
    public interface ICharacterController
    {
        ICharacterInterface AttachedICharacterInterface { get; }
        void AttachToControl(ICharacterInterface iCharacterInterface);
        void LeaveControl();
    }
}
