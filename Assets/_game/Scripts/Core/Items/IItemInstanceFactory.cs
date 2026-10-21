namespace Core.Items
{
    public interface IItemInstanceFactory
    {
        ItemInstance Create(ItemSign sign, float amount);
        ItemInstance CreateByDescription(ItemDescription description);
        /// <summary>The opposite of <see cref="CreateByDescription"/>: the current state of the item, including the contents of containers with their slots.</summary>
        ItemDescription Describe(ItemInstance instance);
        /// <summary>Forgets the item and everything inside it. Describe it first if the state is needed.</summary>
        void Release(ItemInstance instance);
    }
}