using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.World
{
    [CreateAssetMenu(menuName = "SF/Location", fileName = "Location")]
    public class Location : ScriptableObject
    {
        [SerializeField, FolderPath] private string dataPath;
        [SerializeField] private string dataFileFormat = "{x}_{y}.txt";
        private string _correctedFormat;
        private Dictionary<Vector2Int, LocationChunkData> _cache = new ();

        private void OnValidate()
        {
            _correctedFormat = null;
        }

        private string GetDataFilePath(int x, int y)
        {
            _correctedFormat ??= $"{Application.dataPath}/{dataPath}/{string.Format(dataFileFormat.Replace("{x}", "{0}").Replace("{y}", "{1}"), x, y)}";
            return _correctedFormat;
        }
        
        public void WriteChunk(LocationChunkData chunkData, int x, int y)
        {
            string path = GetDataFilePath(x, y);
            using FileStream stream = File.Open(path, FileMode.OpenOrCreate);
            chunkData.Serialize(stream);
        }

        public LocationChunkData ReadChunk(int x, int y)
        {
            if(_cache.TryGetValue(new Vector2Int(x, y), out LocationChunkData chunkData)) return chunkData;
            string path = GetDataFilePath(x, y);
            if(!File.Exists(path)) return null;
            chunkData = new LocationChunkData();
            using FileStream stream = File.Open(path, FileMode.Open);
            chunkData.Deserialize(stream);
            _cache.Add(new Vector2Int(x, y), chunkData);
            return chunkData;
        }
    }
}