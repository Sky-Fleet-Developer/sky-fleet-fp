using System.IO;
using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class HeightChannel : DeformationChannel<HeightMapDeformerModule>
    {
        private HeightmapGpuWorker _gpuWorker;
        public Chunk chunk { get; private set; }
        public int Resolution { get; }
        private HeightmapData _data;
        private TerrainProvider _terrain;
        private Vector2Int _keyToReleaseAfterUse;
        private bool _isLoaded;
        private float[,] _heightmapData;

        public HeightChannel(TerrainProvider terrain, HeightmapGpuWorker gpuWorker, Chunk chunk, int resolution,
            float chunkSize,
            Vector2Int coordinates, string path) : base(terrain, coordinates, chunkSize)
        {
            _terrain = terrain;
            _gpuWorker = gpuWorker;
            _data = terrain.GetHeightmapData();
            this.chunk = chunk;
            Resolution = resolution;
            ReadTex(path);
        }
        
        public HeightmapGpuWorker GetGpuWorker() => _gpuWorker;

        private async void ReadTex(string path)
        {
            //ComputeBuffer verticesBuffer = new ComputeBuffer((resolution + 1) * (resolution + 1), sizeof(float));
            if (path != null)
            {
                //Debug.Log($"Load chunk {path}");
                
                if (!File.Exists(path))
                {
                    return;
                }
                
                if (!_data.SetChunkToMap(Coordinates, out _keyToReleaseAfterUse))
                {
                    Debug.LogError("Has no free slot in heightmap! Increase TerrainGenerationSettings.maxLoadedChunks value!");
                }
                
                Task<ComputeBuffer> t1 = _data.GetLoadDataBuffer();
                Task<float[,]> t2 = RawReader.ReadAsync(path);
                ComputeBuffer buffer = await t1;
                _heightmapData = await t2;
                ApplyHeightmap(buffer);
                //verticesBuffer.SetData(data);
            }
            //deformationLayersCache.Add(verticesBuffer);

            loading.SetResult(true);
        }

        private void ApplyHeightmap(ComputeBuffer buffer)
        {
            buffer.SetData(_heightmapData);
            _gpuWorker.InsertDataToBuffer(buffer, _data.Texture, _data.GetMapBuffer(out Vector2Int mapMin, out int mapSize), Coordinates - mapMin, mapSize, Resolution);
            _data.ReleaseLoadDataBuffer();
            _isLoaded = true;
        }

        protected override void ApplyToCache(HeightMapDeformerModule module)
        {
            if (!_isLoaded)
            {
                return;
            }
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
            if (_isLoaded)
            {
                chunk.SetHeights(_data.Texture, _data.GetMapBuffer(out var mapMin, out var mapSize), Coordinates - mapMin, mapSize);
            }
            return Task.CompletedTask;
        }

        public override Task PostApply()
        {
            if (!_isLoaded)
            {
                return Task.CompletedTask;
            }
            return chunk.PostProcess();
        }

        public override void OnChunkLoad()
        {
            LoadAgainAsync().Forget();
            base.OnChunkLoad();
        }

        private async UniTask LoadAgainAsync()
        {
            Debug.Log($"Load again {Coordinates}");
            ApplyHeightmap(await _data.GetLoadDataBuffer());
            chunk.SetHeights(_data.Texture, _data.GetMapBuffer(out var mapMin, out var mapSize), Coordinates - mapMin, mapSize);
        }

        public override void OnChunkUnload()
        {
            if (_isLoaded)
            {
                _data.ReleaseChunk(_keyToReleaseAfterUse);
            }

            base.OnChunkUnload();
        }
    }
}
