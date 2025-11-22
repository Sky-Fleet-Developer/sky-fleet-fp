using Core.Boot_strapper;
using Core.ContentSerializer;
using UnityEngine;
using Zenject;

namespace Core.World
{
    public class LocationChunksSetInstaller : MonoBehaviour, IMyInstaller
    {
        private LocationChunksSet _locationChunksSet;
private LoadingChunkRuntimeStrategy _loadingChunkRuntimeStrategy;
        [Inject]
        private void Inject(DiContainer container)
        {
            container.Inject(_locationChunksSet);
            container.Inject(_loadingChunkRuntimeStrategy);
        }

        public void InstallBindings(DiContainer container)
        {
            _loadingChunkRuntimeStrategy = new LoadingChunkRuntimeStrategy();
            _locationChunksSet = new LocationChunksSet(_loadingChunkRuntimeStrategy);
            container.BindInstance(_locationChunksSet);
        }
    }
}