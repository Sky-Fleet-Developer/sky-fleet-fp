using Core.Graph;

namespace Core.Structure.Rigging
{
    public interface IFuelUser : IBlock, IGraphNode
    {
        void FuelTick();
    }
}