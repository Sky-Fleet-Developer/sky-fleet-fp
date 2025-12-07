using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

#if FLAT_SPACE
using VectorInt = UnityEngine.Vector2Int;
using VolumeInt = UnityEngine.RectInt;
#else
using VectorInt = UnityEngine.Vector3Int;
using VolumeInt = UnityEngine.BoundsInt;
#endif

namespace Core.World
{
    public interface ILocationChunkLoadStrategy
    {
        Task Load(LocationChunkData data, VectorInt coord);
        Task Unload(LocationChunkData data, VectorInt coord);
    }

    public class LocationChunksSet
    {
        private Dictionary<VectorInt, LocationChunkData> _chunks;
        
#if FLAT_SPACE
        private VolumeInt _range = VolumeInt.zero;
#else
        private VolumeInt _range = new VolumeInt(Vector3Int.zero, Vector3Int.zero);
#endif
        
#if FLAT_SPACE
        private static readonly VolumeInt ZeroVolume = UnityEngine.RectInt.zero;
#else
        private static readonly VolumeInt ZeroVolume = new UnityEngine.BoundsInt(0, 0, 0, 0, 0, 0);
#endif
        
        [Inject] private Location _location;
        [Inject] private DiContainer _diContainer;
        private ILocationChunkLoadStrategy _loadStrategy;
        private Task _setRangeTask;
        public Task SetRangeTask => _setRangeTask;


        public LocationChunksSet(ILocationChunkLoadStrategy loadStrategy)
        {
            _loadStrategy = loadStrategy;
            _chunks = new Dictionary<VectorInt, LocationChunkData>();
        }

        public VolumeInt GetRange() => _range;
        
        public async UniTask SetRange(VolumeInt range)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            _setRangeTask = tcs.Task;
            var oldRange = _range;
            _range = range;

#if FLAT_SPACE
            VectorInt intersectionMin = new VectorInt(Mathf.Max(oldRange.xMin, range.xMin), Mathf.Max(_range.yMin, range.yMin));
            VectorInt intersectionMax = new VectorInt(Mathf.Min(oldRange.xMax, range.xMax), Mathf.Min(_range.yMax, range.yMax));
            VolumeInt intersection = new VolumeInt(intersectionMin, intersectionMax - intersectionMin);
            intersection.width = Mathf.Max(0, intersection.width);
            intersection.height = Mathf.Max(0, intersection.height);
#else
            VectorInt intersectionMin = new VectorInt(Mathf.Max(oldRange.xMin, range.xMin), Mathf.Max(_range.yMin, range.yMin));
            VectorInt intersectionMax = new VectorInt(Mathf.Min(oldRange.xMax, range.xMax), Mathf.Min(_range.yMax, range.yMax));
            VolumeInt intersection = new VolumeInt(intersectionMin, intersectionMax - intersectionMin);
            intersection.size = new VectorInt(Mathf.Max(0, intersection.size.x), Mathf.Max(0, intersection.size.y), Mathf.Max(0, intersection.size.z));
#endif
            


            List<Task> tasks = new List<Task>();
            VectorInt i = VectorInt.zero;
            for (i.x = oldRange.xMin; i.x < oldRange.xMax; i.x++)
            {
                for (i.y = oldRange.yMin; i.y < oldRange.yMax; i.y++)
                {
#if !FLAT_SPACE
                for (i.z = oldRange.zMin; i.z < oldRange.zMax; i.z++)
                {
#endif
                    if (intersection.Contains(i) || !_chunks.ContainsKey(i)) continue;
                    tasks.Add(SaveAndUnload(i));
#if !FLAT_SPACE
                }
#endif
                }
            }

            for (i.x = range.xMin; i.x < range.xMax; i.x++)
            {
                for (i.y = range.yMin; i.y < range.yMax; i.y++)
                {
#if !FLAT_SPACE
                for (i.z = range.zMin; i.z < range.zMax; i.z++)
                {
#endif
                    if (intersection.Contains(i)) continue;
                    tasks.Add(Load(i));
#if !FLAT_SPACE
                }
#endif
                }
            }

            foreach (var task in tasks)
            {
                await task;
            }
            tcs.SetResult(true);
            _setRangeTask = null;
        }

        public async Task Save()
        {
            List<Task> tasks = new List<Task>();
            VectorInt i = VectorInt.zero;
            for (i.x = _range.xMin; i.x < _range.xMax; i.x++)
            {
                for (i.y = _range.yMin; i.y < _range.yMax; i.y++)
                {
#if !FLAT_SPACE
                for (i.z = _range.zMin; i.z < _range.zMax; i.z++)
                {
#endif
                    if (!_chunks.ContainsKey(i)) continue;
                    tasks.Add(Save(i));
#if !FLAT_SPACE
                }
#endif
                }
            }

            foreach (var task in tasks)
            {
                await task;
            }
        }

        private async Task Load(VectorInt coord)
        {
            /*var chunk = await _location.ReadChunk(coord.x, coord.y);
            foreach (var entity in chunk.GetEntities())
            {
                _diContainer.Inject(entity);
            }
             = chunk;
            Debug.Log($"Load chunk: {coord}");*/
            var chunk = new LocationChunkData();
            _chunks[coord] = chunk;
            await _location.ReadChunk(chunk, coord);
            chunk.Lock();
            await _loadStrategy.Load(chunk, coord);
            chunk.Unlock();
        }

        private async Task Save(VectorInt coord)
        {
            await _location.WriteChunk(_chunks[coord], coord);
        }

        private async Task Unload(VectorInt coord)
        {
            await _loadStrategy.Unload(_chunks[coord], coord);
        }

        private async Task SaveAndUnload(VectorInt coord)
        {
            await _location.WriteChunk(_chunks[coord], coord);
            await _loadStrategy.Unload(_chunks[coord], coord);
            _chunks.Remove(coord);
        }

        public void AddEntityToChunk(VectorInt cell, IWorldEntity entity)
        {
            if (!_range.Contains(cell))
            {
                throw new Exception("Entity is not in loaded range");
            }

            _chunks[cell].AddEntity(entity);
        }

        public void RemoveEntityFromChunk(VectorInt cell, IWorldEntity entity)
        {
            if (!_chunks.ContainsKey(cell))
            {
                throw new Exception("Cell is not loaded");
            }

            _chunks[cell].RemoveEntity(entity);
        }

        public IEnumerable<IWorldEntity> GetEntities(VectorInt cell)
        {
            if (_chunks.TryGetValue(cell, out var chunk))
            {
                foreach (var worldEntity in chunk.GetEntities())
                {
                    yield return worldEntity;
                }
            }
        }

        public bool IsInRange(VectorInt cell)
        {
            return _range.Contains(cell);
        }

        public void Unload()
        {
            Task[] tasks = new Task[_chunks.Count];
            int i = 0;
            foreach (var locationChunkData in _chunks)
            {
                tasks[i++] = Unload(locationChunkData.Key);
            }

            Task.WaitAll(tasks);
        }
    }
}