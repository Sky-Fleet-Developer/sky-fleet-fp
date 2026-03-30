using System;
using System.Collections.Generic;
using System.Linq;
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
        private Dictionary<VectorInt, LocationChunkData> _frozen;

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
        [Inject] private ILocationChunkLoadStrategy _loadStrategy;
        private Task _setRangeTask;
        private HashSet<(IWorldEntity entity, VectorInt target)> _notSorted = new ();
        public Task SetRangeTask => _setRangeTask;


        public LocationChunksSet()
        {
            _chunks = new ();
            _frozen = new ();
        }

        public VolumeInt GetRange() => _range;

        public void SetRange(VolumeInt range)
        {
            List<Task> tasks = SetRangeAndGetTasks(range);
            AwaitForSetRange(tasks).Forget();
        }
        public async UniTask SetRangeAsync(VolumeInt range)
        {
            List<Task> tasks = SetRangeAndGetTasks(range);

            await AwaitForSetRange(tasks);
        }

        private async UniTask AwaitForSetRange(List<Task> tasks)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            _setRangeTask = tcs.Task;
            foreach (var task in tasks)
            {
                await task;
            }
            tcs.SetResult(true);
            _setRangeTask = null;
        }

        private List<Task> SetRangeAndGetTasks(VolumeInt range)
        {
            var oldRange = _range;
            _range = range;
            
#if FLAT_SPACE
            VectorInt intersectionMin = new VectorInt(Mathf.Max(oldRange.xMin, range.xMin), Mathf.Max(oldRange.yMin, range.yMin));
            VectorInt intersectionMax = new VectorInt(Mathf.Min(oldRange.xMax, range.xMax), Mathf.Min(oldRange.yMax, range.yMax));
            VolumeInt intersection = new VolumeInt(intersectionMin, intersectionMax - intersectionMin);
            intersection.width = Mathf.Max(0, intersection.width);
            intersection.height = Mathf.Max(0, intersection.height);
#else
            VectorInt intersectionMin = new VectorInt(Mathf.Max(oldRange.xMin, range.xMin), Mathf.Max(oldRange.yMin, range.yMin), Mathf.Max(oldRange.zMin, range.zMin));
            VectorInt intersectionMax = new VectorInt(Mathf.Min(oldRange.xMax, range.xMax), Mathf.Min(oldRange.yMax, range.yMax), Mathf.Min(oldRange.zMax, range.zMax));
            VolumeInt intersection = new VolumeInt(intersectionMin, intersectionMax - intersectionMin);
            intersection.size = new VectorInt(Mathf.Max(0, intersection.size.x), Mathf.Max(0, intersection.size.y), Mathf.Max(0, intersection.size.z));
#endif

            List<Task> tasks = new List<Task>();
            {
                VectorInt i = VectorInt.zero;
                for (i.x = oldRange.xMin; i.x < oldRange.xMax; i.x++)
                {
                    for (i.y = oldRange.yMin; i.y < oldRange.yMax; i.y++)
                    {
#if !FLAT_SPACE
                        for (i.z = oldRange.zMin; i.z < oldRange.zMax; i.z++)
                        {
#endif
                            if (intersection.Contains(i) || !_chunks.ContainsKey(i) || range.Contains(i)) continue;
                            tasks.Add(SaveAndUnload(i));
                            //Debug.Log($"CELLS: unload chunk ({i})");

#if !FLAT_SPACE
                        }
#endif
                    }
                }
            }
            {
                VectorInt i = VectorInt.zero;
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
                            //Debug.Log($"CELLS: load chunk ({i})");
#if !FLAT_SPACE
                        }
#endif
                    }
                }
            }
            return tasks;
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
            var chunk = new LocationChunkData();
            _chunks[coord] = chunk;
            await _location.ReadChunk(chunk, coord);
            chunk.Lock();
            await _loadStrategy.Load(chunk, coord);
            chunk.Unlock();
            
            foreach ((IWorldEntity entity, VectorInt target) in _notSorted)
            {
                if(target == coord) chunk.AddEntity(entity);
            }
            _notSorted.RemoveWhere(x => x.target == coord);
        }

        private async Task Save(VectorInt coord)
        {
            await _location.WriteChunk(_chunks[coord], coord);
        }

        private async Task Unload(VectorInt coord)
        {
            var chunk = _chunks[coord];
            _chunks.Remove(coord);
            await _loadStrategy.Unload(chunk, coord);
        }

        private async Task SaveAndUnload(VectorInt coord)
        {
            var chunk = _chunks[coord];
            _frozen.Add(coord, chunk);
            try
            {
                _chunks.Remove(coord);
                await _location.WriteChunk(chunk, coord);
                await _loadStrategy.Unload(chunk, coord);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _frozen.Remove(coord);
            }
        }

        public void AddEntityToChunk(VectorInt cell, IWorldEntity entity)
        {
            Debug.Log($"CELLS: add ({entity}) to chunk ({cell})");
            if (!_chunks.ContainsKey(cell))
            {
                _notSorted.Add((entity, cell));
                return;
            }

            _chunks[cell].AddEntity(entity);
        }

        public void RemoveEntityFromChunk(VectorInt cell, IWorldEntity entity)
        {
            //Debug.Log($"CELLS: remove ({entity}) from chunk ({cell})");
            if (!_chunks.ContainsKey(cell))
            {
                if (_frozen.ContainsKey(cell))
                {
                    _frozen[cell].RemoveEntity(entity);
                    return;
                }
                //Debug.LogException(new Exception("Cell is not loaded"));
                return;
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
            var toUnload = _chunks.Keys.ToArray();
            
            for (var i = 0; i < toUnload.Length; i++)
            {
                tasks[i] = Unload(toUnload[i]);
            }

            Task.WaitAll(tasks);
        }
    }
}