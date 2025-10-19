using UnityEngine;
using Zenject;

namespace Core.World
{
    public class LocationInstaller : MonoBehaviour, IInstallerWithContainer
    {
        [SerializeField] private Location location;

        public void InstallBindings(DiContainer container)
        {
            container.BindInstance(location);
        }
    }
}