using Core.Graph;

namespace Runtime.Structure.Rigging
{
    public abstract class BlockWithNode : Block, IGraphNode
    {
        public IGraph Graph { get; private set; }
        public virtual void InitNode(IGraph graph)
        {
            Graph = graph;
        }

        public string NodeId => transform.name;
    }
}