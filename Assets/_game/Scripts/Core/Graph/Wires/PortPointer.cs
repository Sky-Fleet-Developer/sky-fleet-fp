using System;

namespace Core.Graph.Wires
{
    public struct PortPointer : System.IEquatable<PortPointer>, System.IEquatable<string>, IPortUser
    {
        private readonly IGraphNode node;
        public readonly  Port Port;

        public string Id => _id;
        private string _id;
        private string _localId;
        private string _group;

        public PortPointer(IGraphNode node, Port port, string id, string group)
        {
            this.node = node;
            if (node == null)
            {
                throw new NullReferenceException();
            }
            Port = port;
            _group = group;
            _localId = id;
            _id = $"{node.NodeId}_{id}";
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

        public string GetGroup() => _group;
        public string GetName() => _localId;
        public bool IsNull() => node == null;
    }
}