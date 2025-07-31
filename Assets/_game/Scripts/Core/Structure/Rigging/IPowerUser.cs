using Core.Graph;

namespace Core.Structure.Rigging
{
    public interface IPowerUser : IBlock, IGraphNode
    {
        void ConsumptionTick();
        void PowerTick();
    }
}