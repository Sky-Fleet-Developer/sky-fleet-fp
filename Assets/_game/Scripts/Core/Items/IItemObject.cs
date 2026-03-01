using Core.Configurations;

namespace Core.Items
{
    public interface IItemObject : IRemotePrefab
    {
        ItemInstance SourceItem { get; }
    }
    public interface IItemObjectHandle : IItemObject
    {
        void SetSourceItem(ItemInstance item);
    }
}