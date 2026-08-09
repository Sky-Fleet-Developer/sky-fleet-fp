using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Sirenix.OdinInspector;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class HeightChannel : DeformationChannel<ComputeBuffer, HeightMapDeformerModule>
    {
        private HeightmapGpuWorker _gpuWorker;
        public Chunk chunk { get; private set; }
        public int Resolution { get; }

        public HeightChannel(HeightmapGpuWorker gpuWorker, Chunk chunk, int resolution, float chunkSize,
            Vector2Int coordinates, string path) : base(coordinates, chunkSize)
        {
            _gpuWorker = gpuWorker;
            this.chunk = chunk;
            Resolution = resolution;
            ReadTex(path, resolution);
        }
        
        public HeightmapGpuWorker GetGpuWorker() => _gpuWorker;

        private async void ReadTex(string path, int resolution)
        {
            ComputeBuffer verticesBuffer = new ComputeBuffer((resolution + 1) * (resolution + 1), sizeof(float));
            if (path != null)
            {
                var data = await RawReader.ReadAsync(path);
                verticesBuffer.SetData(data);
            }
            deformationLayersCache.Add(verticesBuffer);

            loading.SetResult(true);
        }

        protected override void ApplyToCache(HeightMapDeformerModule module)
        {
            module.WriteToChannel(this);
        }

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) =>
            new RectangleAffectSettings(chunk, Position, chunk.Resolution + 1, deformer);

        public override void SetChunk(Chunk chunk)
        {
            IsDirty = true;
            this.chunk = chunk;
        }

        protected override Task ApplyToTerrain()
        {
            if (deformationLayersCache[0] != null)
            {
                chunk.SetHeights(GetLastLayer());
            }

            return Task.CompletedTask;
        }

        public override Task PostApply()
        {
            return chunk.PostProcess();
        }

        protected override ComputeBuffer GetLayerCopy(ComputeBuffer source)
        {
            return _gpuWorker.CopyHeightBuffer(source);
        }
    }
}
