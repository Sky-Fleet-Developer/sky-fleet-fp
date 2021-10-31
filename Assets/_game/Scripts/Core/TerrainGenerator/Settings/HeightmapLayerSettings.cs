using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable, CreateAssetMenu]
    public class HeightmapLayerSettings : LayerSettings
    {
        [Space] public FileFormatSeeker formatMap;


        public override TerrainLayer MakeTerrainLayer(Vector2Int position, string directory)
        {
            string path = formatMap.SearchInFolder(position, directory);

            return new HeightLayer(TerrainProvider.GetTerrain(position).terrainData, Container.heightmapResolution, position, path);
        }
    }
}