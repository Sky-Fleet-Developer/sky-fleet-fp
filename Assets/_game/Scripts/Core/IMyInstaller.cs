using Zenject;

namespace Core
{
    public interface IMyInstaller
    {
        void InstallBindings(DiContainer container);
    }

    public interface IBindMe
    {
    }
}