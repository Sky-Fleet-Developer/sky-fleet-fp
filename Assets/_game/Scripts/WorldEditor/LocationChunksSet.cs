using System.Collections.Generic;
using System.Threading.Tasks;
using Core.World;
using UnityEngine;

namespace WorldEditor
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
        private Location _location;
        private ILocationChunkLoadStrategy _loadStrategy;

        public LocationChunksSet(Location location, ILocationChunkLoadStrategy loadStrategy)
        {
            _location = location;
            _loadStrategy = loadStrategy;
            _chunks = new Dictionary<Vector2Int, LocationChunkData>();
        }

        public async Task SetRange(RectInt range)
        {
            Vector2Int intersectionMin = new Vector2Int(Mathf.Max(_range.xMin, range.xMin), 
                Mathf.Max(_range.yMin, range.yMin));
            Vector2Int intersectionMax = new Vector2Int(Mathf.Min(_range.xMax, range.xMax),
                Mathf.Min(_range.yMax, range.yMax));
            RectInt intersection = new RectInt(intersectionMin, intersectionMax - intersectionMin);
            
            List<Task> tasks = new List<Task>();
            Vector2Int i = Vector2Int.zero;
            for (i.x = _range.xMin; i.x < _range.xMax; i.x++)
            {
                for (i.y = _range.yMin; i.y < _range.yMax; i.y++)
                {
                    if(intersection.Contains(i)) continue;
                    tasks.Add(Load(i));
                }
            }
            for (i.x = range.xMin; i.x < range.xMax; i.x++)
            {
                for (i.y = range.yMin; i.y < range.yMax; i.y++)
                {
                    if(intersection.Contains(i)) continue;
                    tasks.Add(SaveAndUnload(i));
                }
            }
            await Task.WhenAll(tasks);
        }

        private async Task Load(Vector2Int coord)
        {
            var chunk = await _location.ReadChunk(coord.x, coord.y);
            _chunks.Add(new Vector2Int(coord.x, coord.y), chunk);
            await _loadStrategy.Load(chunk, coord);
        }
        
        private async Task SaveAndUnload(Vector2Int coord)
        {
            await _location.WriteChunk(_chunks[coord], coord.x, coord.y);
            await _loadStrategy.Unload(_chunks[coord], coord);
            _chunks.Remove(coord);
        }
    }
}