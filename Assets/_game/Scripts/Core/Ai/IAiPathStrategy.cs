namespace Core.Ai
{
    public interface IAiPathStrategy : IAiStrategy
    {
        public void Link(int unitEntityId, int particleIndex);
    }
}