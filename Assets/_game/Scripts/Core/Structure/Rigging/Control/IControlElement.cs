using Core.Graph;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control.Attributes;

namespace Core.Structure.Rigging.Control
{

    public interface IControlElement : IPortUser
    {
        void Tick();
        
        IDevice Device { get; set; }
        void Init(IGraphHandler graph, ICharacterInterface block);
    }
}