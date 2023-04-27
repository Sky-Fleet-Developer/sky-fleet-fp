using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;

namespace Core.Structure.Rigging.Control
{

    public interface IControlElement : IPortUser, IInteractiveDevice
    {
        void Tick();
        
        IDevice Device { get; set; }
        void Init(IStructure structure, IControl block);
    }
}