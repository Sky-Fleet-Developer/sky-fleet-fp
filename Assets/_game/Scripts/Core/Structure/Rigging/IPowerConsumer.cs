using Core.Graph.Wires;

namespace Core.Structure.Rigging
{
    public interface IPowerConsumer : IPowerUser
    {
        bool IsWork { get; }
        float Consumption { get; }
        PowerPort Power { get; }
        float PowerValue { set; }
    }
}