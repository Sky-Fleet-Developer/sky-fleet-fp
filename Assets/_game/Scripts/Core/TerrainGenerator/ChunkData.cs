using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Core.TerrainGenerator.Settings;
using Core.TerrainGenerator.Utility;
using Core.World;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.TerrainGenerator
{
    [ShowInInspector]
    public class ChunkData : IDisposable
    {
        private MapGpuWorker _gpuWorker;
        private TerrainData _data;
        private Vector3Int _keyToReleaseAfterUse;
        private bool _hasReleaseKey;
        private Task _loading;

        public Vector2Int Coordinates { get; private set; }
        public TerrainProvider Terrain { get; }
        public Task LoadingTask => _loading;
        public bool IsDirty { get; protected set; }
        public bool IsChunkVisible { get; set; }

        public ChunkData(string heightmapPath, string alphamapPath, TerrainProvider terrain, Vector2Int coordinates)
        {
            Terrain = terrain;
            Coordinates = coordinates;
            IsDirty = true;
            _gpuWorker = terrain.Settings.GpuWorker;
            _data = terrain.GetHeightmapData();
            _loading = LoadTextures(heightmapPath, alphamapPath);
        }

        private async Task LoadTextures(string heightmapPath, string alphamapPath)
        {
            var heightmapLoading = ReadRaw16(heightmapPath);
            var alphamapData = ReadPng(alphamapPath);
            if (alphamapData == null)
            {
                return;
            }
            var heightmapData = await heightmapLoading;
            if (heightmapData == null)
            {
                return;
            }
            ComputeBuffer mapBuffer = _data.GetMapBuffer(out Vector2Int mapMin, out int mapSize);

            if (!_data.SetChunkToMap(Coordinates, out _keyToReleaseAfterUse))
            {
                Debug.LogError("Has no free slot in map! Increase TerrainGenerationSettings.maxLoadedChunks value!");
            }
            else
            {
                Debug.Log($"Set chunk {Coordinates} to map at key {_keyToReleaseAfterUse}");
            }
            _hasReleaseKey = true;
            Task<ComputeBuffer> getLoadBufferTask = _data.GetLoadDataBuffer();
            ComputeBuffer loadBuffer = await getLoadBufferTask;
            LoadHeightmapToTexture(heightmapData, loadBuffer, mapBuffer, mapMin, mapSize);
            LoadAlphamapToTexture(alphamapData, loadBuffer, mapBuffer, mapMin, mapSize);

            _data.ReleaseLoadDataBuffer();
        }
        
        private Color32[] ReadPng(string path)
        {
            if (path != null)
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                
                return LoadAtPath(path);
            }
            return null;
        }
        
        private async Task<float[,]> ReadRaw16(string path)
        {
            if (path != null)
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                
                Task<float[,]> t2 = RawReader.ReadAsync(path);
                return await t2;
            }
            return null;
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
        
        private void LoadHeightmapToTexture(float[,] data, ComputeBuffer buffer, ComputeBuffer mapBuffer, Vector2Int mapMin, int mapSize)
        {
            buffer.SetData(data);
            //Debug.Log($"Send data from Heightmap {Coordinates} to texture");
            _gpuWorker.InsertHeightmapDataToBuffer(buffer, (int)Mathf.Sqrt(data.Length), _data.HeightmapTex, mapBuffer, Coordinates - mapMin, mapSize, Terrain.Settings.HeightmapResolution);
        }
        
        private void LoadAlphamapToTexture(Color32[] data, ComputeBuffer buffer, ComputeBuffer mapBuffer, Vector2Int mapMin, int mapSize)
        {
            buffer.SetData(data);
            //Debug.Log($"Send data from Alphamap {Coordinates} to texture");
            _gpuWorker.InsertAlphamapDataToBuffer(buffer, (int)Mathf.Sqrt(data.Length), _data.AlphamapTex, mapBuffer, Coordinates - mapMin, mapSize, Terrain.Settings.AlphamapResolution);
        }
        
        //public abstract void RegisterDeformer(IDeformer deformer);
        //public abstract void ApplyDirtyToCache();
        //public abstract RectangleAffectSettings GetAffectSettingsForDeformer(IDeformer deformer);

        public void Dispose()
        {
            if (_hasReleaseKey)
            {
                _data.ReleaseChunk(_keyToReleaseAfterUse);
            }
        }
    }
}
