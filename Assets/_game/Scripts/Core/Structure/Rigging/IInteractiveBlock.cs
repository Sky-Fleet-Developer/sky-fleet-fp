using Core.Character;

namespace Core.Structure.Rigging
{
    public interface IInteractiveBlock : IBlock, IInteractiveObject
    {
        //IEnumerable<IInteractiveDevice> GetInteractiveDevices();
        void Interaction(ICharacterController character);
    }
}