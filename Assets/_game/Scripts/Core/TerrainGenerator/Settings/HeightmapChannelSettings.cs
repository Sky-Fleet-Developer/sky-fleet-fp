using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable, CreateAssetMenu]
    public class HeightmapChannelSettings : ChannelSettings
    {
        [Space] public FileFormatSeeker formatMap;


        public override DeformationChannel MakeDeformationChannel(Vector2Int position, string directory)
        {
            string path = formatMap.SearchInFolder(position, directory);

            return new HeightChannel(TerrainProvider.GetTerrain(position).terrainData, Container.heightmapResolution, position, path);
        }
    }
}