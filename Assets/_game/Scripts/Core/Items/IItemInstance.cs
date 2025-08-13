using Core.Configurations;

namespace Core.Items
{
    public interface IItemInstance : ITablePrefab
    {
        ItemSign SourceItem { get; }
        string OwnerId { get; }
    }
    public interface IItemInstanceHandle : IItemInstance
    {
        void SetSourceItem(ItemSign sign);
        void SetOwnership(string ownerId);
    }
}