using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    public class TreesChannelSettings : ChannelSettings
    {
        public FileFormatSeeker format;
        public GameObject[] prototypes;

        public override DeformationChannel MakeDeformationChannel(TerrainProvider terrain, Vector2Int position, string directory)
        {
            string path = format.SearchInFolder(position, directory);
            if (path == null) return null;
            return new TreesChannel(terrain.GetChunk(position), Container.ChunkSize, path, position, prototypes);
        }
    }
}