using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.ContentSerializer;
using Sirenix.OdinInspector;
using UnityEngine;
#if FLAT_SPACE
using VectorInt = UnityEngine.Vector2Int;
using VolumeInt = UnityEngine.RectInt;
#else
using VectorInt = UnityEngine.Vector3Int;
using VolumeInt = UnityEngine.BoundsInt;
#endif

namespace Core.World
{
    [CreateAssetMenu(menuName = "SF/Location", fileName = "Location")]
    public class Location : ScriptableObject
    {
        [SerializeField, FolderPath(AbsolutePath = true)] private string dataPath;
        [SerializeField] private string dataFileFormat2d = "{x}_{y}.txt";
        [SerializeField] private string dataFileFormat3d = "{x}_{y}_{z}.txt";
        private string _correctedFormat_3d;
        private string _correctedFormat_2d;

        private void OnValidate()
        {
            _correctedFormat_3d = null;
            _correctedFormat_2d = null;
        }

#if FLAT_SPACE
        private string GetDataFilePath()
        {
            _correctedFormat_2d ??= $"{dataPath}/{dataFileFormat2d.Replace("{x}", "{0}").Replace("{y}", "{1}")}";
            return _correctedFormat_2d;
        }
#else
        private string GetDataFilePath()
        {
            _correctedFormat_3d ??= $"{dataPath}/{dataFileFormat3d.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}")}";
            return _correctedFormat_3d;
        }
#endif
        
        public async Task WriteChunk(LocationChunkData chunkData, VectorInt coord)
        {
            if (Application.isPlaying)
            {
                return;
            }
#if FLAT_SPACE
            string path = string.Format(GetDataFilePath(), coord.x, coord.y);
#else
            string path = string.Format(GetDataFilePath(), coord.x, coord.y, coord.z);
#endif

            if (chunkData.IsEmpty)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return;
            }
            await using FileStream stream = File.Open(path, FileMode.OpenOrCreate);
            Serializers.GetSerializer(typeof(LocationChunkData)).Serialize(chunkData, stream);
        }

        public async Task ReadChunk(LocationChunkData destination, VectorInt coord)
        {
#if FLAT_SPACE
            string path = string.Format(GetDataFilePath(), coord.x, coord.y);
#else
            string path = string.Format(GetDataFilePath(), coord.x, coord.y, coord.z);
#endif
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