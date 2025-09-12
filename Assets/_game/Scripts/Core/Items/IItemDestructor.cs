namespace Core.Items
{
    public interface IItemDestructor
    {
        void Deconstruct(IItemObjectHandle itemObject);
    }
}