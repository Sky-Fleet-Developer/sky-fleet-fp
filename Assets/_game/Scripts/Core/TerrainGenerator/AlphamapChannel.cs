using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Core.TerrainGenerator.Settings;
using UnityEngine;

using Core.TerrainGenerator.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Color = UnityEngine.Color;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class AlphamapChannel : DeformationChannel<ColorMapModifier>
    {
        private MapGpuWorker _gpuWorker;
        public IChunk chunk { get; private set; }
        public int Resolution { get; }
        private TerrainData _data;
        private TerrainProvider _terrain;
        private Vector3Int _keyToReleaseAfterUse;
        private bool _hasReleaseKey;
        private Color32[] _alphamapData;

        public AlphamapChannel(TerrainProvider terrain, IChunk chunk, int resolution,
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
                ComputeBuffer buffer = await t1;
                _alphamapData = LoadAtPath(path);
                LoadToTexture(buffer);
                _data.ReleaseLoadDataBuffer();

                //verticesBuffer.SetData(data);
            }
            //deformationLayersCache.Add(verticesBuffer);

            loading.SetResult(true);
        }
        
        private Color32[] LoadAtPath(string path) // TODO: get texture in edit-mode
        {
            using (Bitmap bitmap = new Bitmap(path))
            {
                int resolution = bitmap.Width;
                Color32[] result = new Color32[resolution * resolution];
                for (int u = 0; u < resolution; u++)
                {
                    for (int v = 0; v < resolution; v++)
                    {
                        var nativeColor = bitmap.GetPixel(u, resolution - v - 1);
                        result[u + v * resolution] = new Color32(nativeColor.R, nativeColor.G, nativeColor.B, nativeColor.A);
                    }
                }
                return result;
            }
        }

        private void LoadToTexture(ComputeBuffer buffer)
        {
            buffer.SetData(_alphamapData);
            var mapBuffer = _data.GetMapBuffer(out Vector2Int mapMin, out int mapSize);
            //Debug.Log($"Send data from Heightmap {Coordinates} to texture");
            _gpuWorker.InsertAlphamapDataToBuffer(buffer, _data.AlphamapTex, mapBuffer, Coordinates - mapMin, mapSize, Resolution);
        }

        protected override void ApplyToCache(ColorMapModifier module)
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
            if (_alphamapData == null)
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
                LoadToTexture(buffer);
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
