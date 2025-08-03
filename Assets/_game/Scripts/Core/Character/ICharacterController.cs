using System.Collections;
using Core.Character.Interaction;
using Core.Structure.Rigging;

namespace Core.Character
{
    public interface ICharacterController
    {
        void EnterHandler(ICharacterHandler handler);
    }
}
