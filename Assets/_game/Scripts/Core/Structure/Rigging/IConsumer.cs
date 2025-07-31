using Core.Graph.Wires;

namespace Core.Structure.Rigging
{
    public interface IConsumer : IPowerUser
    {
        bool IsWork { get; }
        float Consumption { get; }
        PowerPort Power { get; }
    }
}