using Core.Configurations;

namespace Core.Items
{
    public interface IItemObject : ITablePrefab
    {
        ItemInstance SourceItem { get; }
        string OwnerId { get; }
    }
    public interface IItemObjectHandle : IItemObject
    {
        void SetSourceItem(ItemInstance item);
        void SetOwnership(string ownerId);
    }
}