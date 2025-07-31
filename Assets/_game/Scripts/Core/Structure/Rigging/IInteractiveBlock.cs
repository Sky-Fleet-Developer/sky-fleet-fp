using Core.Character;

namespace Core.Structure.Rigging
{
    public interface IInteractiveBlock : IBlock, IInteractiveObject
    {
        void Interaction(ICharacterController character);
    }
}