using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    public class TreesChannelSettings : ChannelSettings
    {
        public FileFormatSeeker format;
        public GameObject[] prototypes;

        public override DeformationChannel MakeDeformationChannel(TerrainProvider terrain, Vector2Int position, string directory)
        {
            string path = format.SearchInFolder(position + terrain.Settings.ChunksCenter, directory);
            if (path == null) return null;
            return new TreesChannel(terrain, terrain.GetChunk(position), Container.ChunkSize, path, position, prototypes);
        }
    }
}