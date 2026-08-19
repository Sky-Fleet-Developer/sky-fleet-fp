using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable, CreateAssetMenu]
    public class MeshHeightmapChannelSettings : ChannelSettings
    {
        [Space] public FileFormatSeeker formatMap;


        public override DeformationChannel MakeDeformationChannel(TerrainProvider terrain, Vector2Int position, string directory)
        {
            string path = formatMap.SearchInFolder(position + terrain.Settings.ChunksCenter, directory);

            return new HeightChannel(terrain, terrain.GetChunk(position), Container.HeightmapResolution, Container.ChunkSize, position, path);
        }
    }
}