using Zenject;

namespace Core
{
    public interface IInstallerWithContainer
    {
        void InstallBindings(DiContainer container);
    }
}