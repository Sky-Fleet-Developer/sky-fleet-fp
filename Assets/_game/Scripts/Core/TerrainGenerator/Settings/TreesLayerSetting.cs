using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    public class TreesLayerSetting : LayerSettings
    {
        public FileFormatSeeker format;
        public GameObject[] prototypes;

        public override TerrainLayer MakeTerrainLayer(Vector2Int position, string directory)
        {
            string path = format.SearchInFolder(position, directory);
            if (path == null) return null;
            return new TreesLayer(TerrainProvider.GetTerrain(position).terrainData, path, position, prototypes);
        }
    }
}