using System.Collections.Generic;
using System.Threading.Tasks;
using Core.World;
using UnityEngine;
using Zenject;

#if FLAT_SPACE
using VectorInt = UnityEngine.Vector2Int;
using VolumeInt = UnityEngine.RectInt;
#else
using VectorInt = UnityEngine.Vector3Int;
using VolumeInt = UnityEngine.BoundsInt;
#endif

namespace WorldEditor
{
    public class LocationChunkEditorLoadStrategy : ILocationChunkLoadStrategy
    {
        [Inject] private WorldGrid _worldGrid;
        [Inject] private WorldSpace _worldSpace;

        public async Task Load(LocationChunkData data, VectorInt coord)
        {
            List<Task> tasks = new List<Task>();
            foreach (var worldEntity in data.GetEntities())
            {
                _worldSpace.AddEntity(worldEntity);
                worldEntity.OnLodChanged(0);
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

        public async Task Unload(LocationChunkData data, VectorInt coord)
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