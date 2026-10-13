using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable]
    public class HeightmapSettings
    {
        public FileFormatSeeker formatMap;
        public float chunkSizeMeters = 300;
        [Tooltip("Chunk files overlap by one pixel: the edge row of a chunk equals the edge row of its neighbour " +
                 "(e.g. 257 = 256 + 1). Off: every pixel is a separate cell of the chunk.")]
        public bool sharedEdgePixels = true;

        public string GetPath(Vector2Int position, string directory)
        {
            return formatMap.SearchInFolder(position, directory);
        }
    }
}