using Core.Graph;

namespace Core.Structure.Rigging
{
    public abstract class BlockWithNode : Block, IGraphNode
    {
        public IGraph Graph { get; private set; }
        public void InitNode(IGraph graph)
        {
            Graph = graph;
        }

        public string NodeId => transform.name;
    }
}