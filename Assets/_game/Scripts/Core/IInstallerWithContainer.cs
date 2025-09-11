using Zenject;

namespace Core
{
    public interface IInstallerWithContainer : IInstaller
    {
        public DiContainer DiContainer { get; set; }
    }
}