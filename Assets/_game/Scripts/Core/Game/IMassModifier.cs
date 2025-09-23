namespace Core.Game
{
    public interface IMassModifier
    {
        float Mass { get; }
        void AddListener(IMassCombinator massCombinator);
        void RemoveListener(IMassCombinator massCombinator);
    }
}