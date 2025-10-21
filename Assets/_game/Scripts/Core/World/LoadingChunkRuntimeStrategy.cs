using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Core.World
{
    public class LoadingChunkRuntimeStrategy : ILocationChunkLoadStrategy
    {
        [Inject] private WorldGrid _worldGrid;
        [Inject] private WorldSpace _worldSpace;

        public async Task Load(LocationChunkData data, Vector2Int coord)
        {
            List<Task> tasks = new List<Task>();
            foreach (var worldEntity in data.GetEntities())
            {
                _worldSpace.AddEntity(worldEntity);
                var loadTask = worldEntity.GetAnyLoad();
                if (loadTask != null)
                {
                    tasks.Add(loadTask);
                }
            }

            foreach (var task in tasks)
            {
                await task;
            }
        }

        public async Task Unload(LocationChunkData data, Vector2Int coord)
        {
            List<Task> tasks = new List<Task>();
            foreach (var worldEntity in data.GetEntities())
            {
                _worldSpace.RemoveEntity(worldEntity);
                worldEntity.Dispose();
                var loadTask = worldEntity.GetAnyLoad();
                if (loadTask != null)
                {
                    tasks.Add(loadTask);
                }
            }

            foreach (var task in tasks)
            {
                await task;
            }
        }
    }
}