using Core.Trading;

namespace Core.Configurations
{
    public interface IItemInstance
    {
        public ItemSign SourceItem { get; }
    }
    public interface IItemInstanceHandle : IItemInstance
    {
        public void SetSourceItem(ItemSign sign);
    }
}