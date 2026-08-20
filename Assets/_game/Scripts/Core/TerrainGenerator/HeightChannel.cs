using System;
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
        public IChunk chunk { get; private set; }
        public int Resolution { get; }
        private HeightmapData _data;
        private TerrainProvider _terrain;
        private Vector3Int _keyToReleaseAfterUse;
        private bool _hasReleaseKey;
        private float[,] _heightmapData;

        public HeightChannel(TerrainProvider terrain, IChunk chunk, int resolution,
            float chunkSize,
            Vector2Int coordinates, string path) : base(terrain, coordinates, chunkSize)
        {
            _terrain = terrain;
            _gpuWorker = terrain.Settings.GpuWorker;
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
                _hasReleaseKey = true;
                Task<ComputeBuffer> t1 = _data.GetLoadDataBuffer();
                Task<float[,]> t2 = RawReader.ReadAsync(path);
                ComputeBuffer buffer = await t1;
                _heightmapData = await t2;
                LoadHeightmapToTexture(buffer);
                _data.ReleaseLoadDataBuffer();

                //verticesBuffer.SetData(data);
            }
            //deformationLayersCache.Add(verticesBuffer);

            loading.SetResult(true);
        }

        private void LoadHeightmapToTexture(ComputeBuffer buffer)
        {
            buffer.SetData(_heightmapData);
            var mapBuffer = _data.GetMapBuffer(out Vector2Int mapMin, out int mapSize);
            //Debug.Log($"Send data from Heightmap {Coordinates} to texture");
            _gpuWorker.InsertDataToBuffer(buffer, _data.HeightmapTex, mapBuffer, Coordinates - mapMin, mapSize, Resolution);
        }

        protected override void ApplyToCache(HeightMapDeformerModule module)
        {
            if (!_hasReleaseKey)
            {
                return;
            }
            module.WriteToChannel(this);
        }

        public override RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer) =>
            new RectangleAffectSettings(chunk, Position, Terrain.Settings.HeightmapResolution + 1, deformer);

        public override void SetChunk(IChunk chunk)
        {
            this.chunk = chunk;
        }

        protected override Task ApplyToTerrain()
        {
            if (_hasReleaseKey)
            {
                //Debug.Log($"Set heights to mesh by {Coordinates}");
                chunk.SetHeights(_data.HeightmapTex, _data.GetMapBuffer(out var mapMin, out var mapSize), Coordinates - mapMin, mapSize);
            }
            return Task.CompletedTask;
        }

        //public override Task PostApply()
        //{
        //    if (!_hasReleaseKey)
        //    {
        //        return Task.CompletedTask;
        //    }
        //    return chunk.PostProcess();
        //}

        public override void OnChunkLoad()
        {
            LoadAgainAsync().Forget();
            base.OnChunkLoad();
        }

        private async UniTask LoadAgainAsync()
        {
            if (_heightmapData == null)
            {
                return;
            }
            //Debug.Log($"Load again {Coordinates}");
            if (!_data.SetChunkToMap(Coordinates, out _keyToReleaseAfterUse))
            {
                Debug.LogError("Has no free slot in heightmap! Increase TerrainGenerationSettings.maxLoadedChunks value!");
            }
            else
            {
                _hasReleaseKey = true;
            }

            var buffer = await _data.GetLoadDataBuffer();
            try
            {
                //Debug.Log($"Begin send heights to texture: {Coordinates}");
                LoadHeightmapToTexture(buffer);
                //Debug.Log($"End send heights to texture");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            _data.ReleaseLoadDataBuffer();
            IsDirty = true;
            //chunk.SetHeights(_data.Texture, _data.GetMapBuffer(out var mapMin, out var mapSize), Coordinates - mapMin, mapSize);
            //Debug.Log($"Set heights to mesh by {Coordinates}");
        }

        public override void OnChunkUnload()
        {
            if (_hasReleaseKey)
            {
                _data.ReleaseChunk(_keyToReleaseAfterUse);
                _hasReleaseKey = false;
            }

            base.OnChunkUnload();
        }
    }
}
