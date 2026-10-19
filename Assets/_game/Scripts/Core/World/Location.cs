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
#endif

namespace Core.World
{
    [CreateAssetMenu(menuName = "SF/Location", fileName = "Location")]
    public class Location : ScriptableObject
    {
        [SerializeField, FolderPath(AbsolutePath = true)] private string dataPath;
        [SerializeField] private string dataFileFormat2d = "{x}_{y}.txt";
        [SerializeField] private string dataFileFormat3d = "{x}_{y}_{z}.txt";

        private string GetChunkPath(string directory, VectorInt coord)
        {
#if FLAT_SPACE
            string format = dataFileFormat2d.Replace("{x}", "{0}").Replace("{y}", "{1}");
            return Path.Combine(directory, string.Format(format, coord.x, coord.y));
#else
            string format = dataFileFormat3d.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}");
            return Path.Combine(directory, string.Format(format, coord.x, coord.y, coord.z));
#endif
        }

        private const string IndexFileName = "entity_index.bin";

        /// <summary>
        /// In play mode the index of the save slot wins over the authored one.
        /// </summary>
        /// <returns>Null if there is no index yet.</returns>
        public async Task<byte[]> ReadIndex(string saveDirectory = null)
        {
            string path = Path.Combine(dataPath, IndexFileName);
            if (Application.isPlaying && !string.IsNullOrEmpty(saveDirectory))
            {
                string savedPath = Path.Combine(saveDirectory, IndexFileName);
                if (File.Exists(savedPath))
                {
                    path = savedPath;
                }
            }

            return File.Exists(path) ? await File.ReadAllBytesAsync(path) : null;
        }

        public async Task WriteIndex(byte[] data, string saveDirectory = null)
        {
            bool toSave = Application.isPlaying;
            if (toSave && string.IsNullOrEmpty(saveDirectory)) return;
            string directory = toSave ? saveDirectory : dataPath;
            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(Path.Combine(directory, IndexFileName), data);
        }

        /// <summary>
        /// In play mode changes go to <paramref name="saveDirectory"/> and the authored folder stays untouched.
        /// An emptied chunk leaves a zero-length file there, so the authored entities do not come back.
        /// Outside play mode (world editor) the authored folder is written.
        /// </summary>
        public async Task WriteChunk(LocationChunkData chunkData, VectorInt coord, string saveDirectory = null)
        {
            bool toSave = Application.isPlaying;
            if (toSave && string.IsNullOrEmpty(saveDirectory))
            {
                return;
            }

            string authoredPath = GetChunkPath(dataPath, coord);
            string path = toSave ? GetChunkPath(saveDirectory, coord) : authoredPath;

            if (chunkData.IsEmpty)
            {
                if (toSave && File.Exists(authoredPath))
                {
                    Directory.CreateDirectory(saveDirectory);
                    File.WriteAllBytes(path, System.Array.Empty<byte>());
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            await using FileStream stream = File.Open(path, FileMode.Create);
            Serializers.GetSerializer(typeof(LocationChunkData)).Serialize(chunkData, stream);
        }

        /// <summary>
        /// In play mode a file in <paramref name="saveDirectory"/> wins over the authored one.
        /// </summary>
        public async Task ReadChunk(LocationChunkData destination, VectorInt coord, string saveDirectory = null)
        {
            string path = GetChunkPath(dataPath, coord);
            if (Application.isPlaying && !string.IsNullOrEmpty(saveDirectory))
            {
                string savedPath = GetChunkPath(saveDirectory, coord);
                if (File.Exists(savedPath))
                {
                    path = savedPath;
                }
            }

            if (!File.Exists(path) || new FileInfo(path).Length == 0) return;

            await using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read);
            var serializer = Serializers.GetSerializer(typeof(LocationChunkData));
            object chunkDataRef = destination;
            serializer?.Populate(stream, ref chunkDataRef);
        }
    }
}