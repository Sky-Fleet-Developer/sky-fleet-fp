using System.Collections.Generic;
using System.Threading.Tasks;
using Core.World;
using UnityEngine;

namespace WorldEditor
{
    public class LocationChunkEditorLoadStrategy : ILocationChunkLoadStrategy
    {
        public Task Load(LocationChunkData data, Vector2Int coord)
        {
            List<Task> tasks = new List<Task>();
            foreach (var worldEntity in data.GetEntities())
            {
                worldEntity.OnLodChanged(0);
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
                data.RemoveEntity(worldEntity);
                if (loadTask != null)
                {
                    tasks.Add(loadTask);
                }
            }
            
            return Task.WhenAll(tasks);
        }
    }
}