namespace Shared
{
    public interface INetworkBehaviour
    {
        public void Init();
        public void UpdateServer();
        public void Shutdown();
    }
}