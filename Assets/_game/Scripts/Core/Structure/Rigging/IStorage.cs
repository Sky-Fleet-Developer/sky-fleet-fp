using Core.Graph;

namespace Core.Structure.Rigging
{
    public interface IStorage : IGraphNode
    {
        float CurrentAmount { get; }
        float MaximalAmount { get; }
        float MaxInput { get; }
        float MaxOutput { get; }
        float AmountInPort { get; }
        StorageMode Mode { get; set; }
        void PushToPort(float amount);
    }
}