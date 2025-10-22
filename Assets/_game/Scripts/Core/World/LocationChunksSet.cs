using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Core.World
{
    public interface ILocationChunkLoadStrategy
    {
        Task Load(LocationChunkData data, Vector2Int coord);
        Task Unload(LocationChunkData data, Vector2Int coord);
    }

    public class LocationChunksSet
    {
        private Dictionary<Vector2Int, LocationChunkData> _chunks;
        private RectInt _range = RectInt.zero;
        [Inject] private Location _location;
        [Inject] private DiContainer _diContainer;
        private ILocationChunkLoadStrategy _loadStrategy;


        public LocationChunksSet(ILocationChunkLoadStrategy loadStrategy)
        {
            _loadStrategy = loadStrategy;
            _chunks = new Dictionary<Vector2Int, LocationChunkData>();
        }

        public RectInt GetRange() => _range;
        
        public async Task SetRange(RectInt range)
        {
            var oldRange = _range;
            _range = range;

            Vector2Int intersectionMin = new Vector2Int(Mathf.Max(oldRange.xMin, range.xMin),
                Mathf.Max(_range.yMin, range.yMin));
            Vector2Int intersectionMax = new Vector2Int(Mathf.Min(oldRange.xMax, range.xMax),
                Mathf.Min(_range.yMax, range.yMax));
            RectInt intersection = new RectInt(intersectionMin, intersectionMax - intersectionMin);
            intersection.width = Mathf.Max(0, intersection.width);
            intersection.height = Mathf.Max(0, intersection.height);

            List<Task> tasks = new List<Task>();
            Vector2Int i = Vector2Int.zero;
            for (i.x = oldRange.xMin; i.x < oldRange.xMax; i.x++)
            {
                for (i.y = oldRange.yMin; i.y < oldRange.yMax; i.y++)
                {
                    if (intersection.Contains(i) || !_chunks.ContainsKey(i)) continue;
                    tasks.Add(SaveAndUnload(i));
                }
            }

            for (i.x = range.xMin; i.x < range.xMax; i.x++)
            {
                for (i.y = range.yMin; i.y < range.yMax; i.y++)
                {
                    if (intersection.Contains(i)) continue;
                    tasks.Add(Load(i));
                }
            }

            foreach (var task in tasks)
            {
                await task;
            }
        }

        public async Task Save()
        {
            List<Task> tasks = new List<Task>();
            Vector2Int i = Vector2Int.zero;
            for (i.x = _range.xMin; i.x < _range.xMax; i.x++)
            {
                for (i.y = _range.yMin; i.y < _range.yMax; i.y++)
                {
                    if (!_chunks.ContainsKey(i)) continue;
                    tasks.Add(Save(i));
                }
            }

            foreach (var task in tasks)
            {
                await task;
            }
        }

        private async Task Load(Vector2Int coord)
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
            await _location.ReadChunk(chunk, coord.x, coord.y);
            chunk.Lock();
            await _loadStrategy.Load(chunk, coord);
            chunk.Unlock();
        }

        private async Task Save(Vector2Int coord)
        {
            await _location.WriteChunk(_chunks[coord], coord.x, coord.y);
        }

        private async Task Unload(Vector2Int coord)
        {
            await _loadStrategy.Unload(_chunks[coord], coord);
        }

        private async Task SaveAndUnload(Vector2Int coord)
        {
            await _location.WriteChunk(_chunks[coord], coord.x, coord.y);
            await _loadStrategy.Unload(_chunks[coord], coord);
            _chunks.Remove(coord);
        }

        public void AddEntityToChunk(Vector2Int cell, IWorldEntity entity)
        {
            if (!_range.Contains(cell))
            {
                throw new Exception("Entity is not in loaded range");
            }

            _chunks[cell].AddEntity(entity);
        }

        public void RemoveEntityFromChunk(Vector2Int cell, IWorldEntity entity)
        {
            if (!_chunks.ContainsKey(cell))
            {
                throw new Exception("Cell is not loaded");
            }

            _chunks[cell].RemoveEntity(entity);
        }

        public IEnumerable<IWorldEntity> GetEntities(Vector2Int cell)
        {
            if (_chunks.TryGetValue(cell, out var chunk))
            {
                foreach (var worldEntity in chunk.GetEntities())
                {
                    yield return worldEntity;
                }
            }
        }

        public bool IsInRange(Vector2Int cell)
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