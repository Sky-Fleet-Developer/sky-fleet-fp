using Core.Graph;

namespace Core.Structure.Rigging
{
    public abstract class BlockWithNode : Block, IGraphNode
    {
        public IGraphHandler Graph { get; private set; }
        public void InitNode(IGraphHandler graph)
        {
            Graph = graph;
        }

        public string NodeId => transform.name;
    }
}