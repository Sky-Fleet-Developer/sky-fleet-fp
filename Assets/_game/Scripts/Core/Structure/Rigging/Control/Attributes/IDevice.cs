using Core.Configurations;
using Core.Graph;

namespace Core.Structure.Rigging.Control.Attributes
{
    public interface IDevice : ITablePrefab
    {
        IGraphHandler Graph { get; }
        IBlock Block { get; }
        void Init(IGraphHandler graph, IBlock block);
        void UpdateDevice();
    }
}