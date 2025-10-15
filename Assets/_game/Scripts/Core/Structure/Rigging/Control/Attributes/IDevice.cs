using Core.Configurations;
using Core.Graph;

namespace Core.Structure.Rigging.Control.Attributes
{
    public interface IDevice : ITablePrefab
    {
        IGraph Graph { get; }
        IBlock Block { get; }
        void Init(IGraph graph, IBlock block);
        void UpdateDevice();
    }
}