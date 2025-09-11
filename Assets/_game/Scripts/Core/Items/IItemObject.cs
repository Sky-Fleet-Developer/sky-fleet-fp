using Core.Configurations;

namespace Core.Items
{
    public interface IItemObject : ITablePrefab
    {
        ItemInstance SourceItem { get; }
    }
    public interface IItemObjectHandle : IItemObject
    {
        void SetSourceItem(ItemInstance item);
    }
}