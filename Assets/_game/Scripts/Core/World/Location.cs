using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.World
{
    [CreateAssetMenu(menuName = "SF/Location", fileName = "Location")]
    public class Location : ScriptableObject
    {
        [SerializeField, FolderPath(AbsolutePath = true)] private string dataPath;
        [SerializeField] private string dataFileFormat = "{x}_{y}.txt";
        private string _correctedFormat;

        private void OnValidate()
        {
            _correctedFormat = null;
        }

        private string GetDataFilePath()
        {
            _correctedFormat ??= $"{dataPath}/{dataFileFormat.Replace("{x}", "{0}").Replace("{y}", "{1}")}";
            return _correctedFormat;
        }
        
        public async Task WriteChunk(LocationChunkData chunkData, int x, int y)
        {
            string path = string.Format(GetDataFilePath(), x, y);
            await using FileStream stream = File.Open(path, FileMode.OpenOrCreate);
            Serializers.GetSerializer(typeof(LocationChunkData)).Serialize(chunkData, stream);
        }

        public async Task ReadChunk(LocationChunkData destination, int x, int y)
        {
            string path = string.Format(GetDataFilePath(), x, y);
            if (File.Exists(path))
            {
                await using FileStream stream = File.Open(path, FileMode.Open);
                var serializer = Serializers.GetSerializer(typeof(LocationChunkData));
                object chunkDataRef = destination;
                serializer?.Populate(stream, ref chunkDataRef);
            }
        }
    }
}