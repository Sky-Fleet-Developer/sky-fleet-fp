using System.Collections.Generic;
using Core.Character;
using Core.Graph;
using Core.Structure.Rigging.Control;

namespace Core.Structure.Rigging
{
    public interface IControl : IInteractiveBlock, IUpdatableBlock, IGraphNode
    {
        bool IsUnderControl { get; }
        List<ControlAxis> Axes { get; }
        CharacterAttachData GetAttachData();
        void ReadInput();
        void LeaveControl(ICharacterController controller);
    }
}