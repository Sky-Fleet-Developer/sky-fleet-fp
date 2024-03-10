namespace Core.Graph.Wires
{
    public struct PortPointer : System.IEquatable<PortPointer>, System.IEquatable<string>, IPortUser
    {
        private readonly IGraphNode node;
        public readonly  Port Port;

        public string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = node.NodeId + Port.Guid;
                }
                return _id;
            }
        }
        private string _id;
        private string _description;

        public PortPointer(IGraphNode node, Port port, string description)
        {
            this.node = node;
            Port = port;
            _description = description;
            _id = null;
        }

        public PortPointer(IGraphNode node, Port port)
        {
            this.node = node;
            Port = port;
            _description = string.Empty;
            _id = null;
        }

        public override string ToString()
        {
            return Id;
        }

        public bool Equals(PortPointer other)
        {
            return Id == other.Id;
        }

        public bool Equals(string other)
        {
            return Id.Equals(other);
        }

        public override bool Equals(object obj)
        {
            return obj is PortPointer other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        
        public bool CanConnect(Wire wire) => wire.CanConnect(this);
        public Port GetPort() => Port;

        public string GetPortDescription() => _description;
    }
}