using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable]
    public class HeightmapSettings
    {
        public FileFormatSeeker formatMap;
        public float chunkSizeMeters = 300;

        public string GetPath(Vector2Int position, string directory)
        {
            return formatMap.SearchInFolder(position, directory);
        }
    }
}