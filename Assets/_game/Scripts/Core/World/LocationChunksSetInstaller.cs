using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using UnityEngine;
using Zenject;

namespace Core.World
{
    public class LoadingChunkRuntimeStrategy : ILocationChunkLoadStrategy
    {
        [Inject] private WorldGrid _worldGrid;
        public Task Load(LocationChunkData data, Vector2Int coord)
        {
            List<Task> tasks = new List<Task>();
            foreach (var worldEntity in data.GetEntities())
            {
                var loadTask = worldEntity.GetAnyLoad();
                if (loadTask != null)
                {
                    tasks.Add(loadTask);
                }
            }
            
            return Task.WhenAll(tasks);
        }

        public Task Unload(LocationChunkData data, Vector2Int coord)
        {
            List<Task> tasks = new List<Task>();
            foreach (var worldEntity in data.GetEntities())
            {
                worldEntity.Dispose();
                var loadTask = worldEntity.GetAnyLoad();
                if (loadTask != null)
                {
                    tasks.Add(loadTask);
                }
            }
            
            return Task.WhenAll(tasks);
        }
    }
    public class LocationChunksSetInstaller : MonoBehaviour, IInstallerWithContainer
    {
        private LocationChunksSet _locationChunksSet;

        [Inject]
        private void Inject(DiContainer container)
        {
            container.Inject(_locationChunksSet);
        }
        public void InstallBindings(DiContainer container)
        {
            _locationChunksSet = new LocationChunksSet(new LoadingChunkRuntimeStrategy());
            container.BindInstance(_locationChunksSet);
        }
    }
}