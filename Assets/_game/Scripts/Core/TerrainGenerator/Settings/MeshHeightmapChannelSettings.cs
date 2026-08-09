using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable, CreateAssetMenu]
    public class MeshHeightmapChannelSettings : ChannelSettings
    {
        [Space] public FileFormatSeeker formatMap;
        public ComputeShader gpuWorksShader;
        private HeightmapGpuWorker _gpuWorker;

        public HeightmapGpuWorker GpuWorker
        {
            get
            {
                _gpuWorker ??= new HeightmapGpuWorker(gpuWorksShader);
                return _gpuWorker;
            }
        }

        public override DeformationChannel MakeDeformationChannel(TerrainProvider terrain, Vector2Int position, string directory)
        {
            string path = formatMap.SearchInFolder(position + terrain.settings.ChunksCenter, directory);

            return new HeightChannel(GpuWorker, terrain.GetChunk(position), Container.HeightmapResolution, Container.ChunkSize, position, path);
        }
    }
}