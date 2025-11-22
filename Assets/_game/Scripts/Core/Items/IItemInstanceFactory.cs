namespace Core.Items
{
    public interface IItemInstanceFactory
    {
        ItemInstance Create(ItemSign sign, float amount);
    }
}