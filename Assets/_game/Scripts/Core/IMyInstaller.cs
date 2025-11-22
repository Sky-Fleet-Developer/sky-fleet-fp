using Zenject;

namespace Core
{
    public interface IMyInstaller
    {
        void InstallBindings(DiContainer container);
    }
}