namespace Core.World
{
    public interface IWorldEntityDisposeListener
    {
        void OnEntityDisposed(IWorldEntity entity);
    }
}