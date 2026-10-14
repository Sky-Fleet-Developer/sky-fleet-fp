using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Settings;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// Bakes the blended biome debug colors of every loaded chunk into <see cref="DebugTex"/>,
    /// a texture array with the same layers and pixel layout as the heightmap.
    /// </summary>
    public class BiomeDebugBaker : IDisposable
    {
        private const string KernelName = "CSBiomeDebug";
        private const int ThreadGroupSize = 8;

        private static readonly int DestinationProperty = Shader.PropertyToID("biome_debug_destination");
        private static readonly int SlotCoordsProperty = Shader.PropertyToID("biome_slot_coords");
        private static readonly int ColorsProperty = Shader.PropertyToID("biome_colors");
        private static readonly int ResolutionProperty = Shader.PropertyToID("biome_resolution");
        private static readonly int PixelsPerChunkProperty = Shader.PropertyToID("biome_pixels_per_chunk");
        private static readonly int PixelOffsetProperty = Shader.PropertyToID("biome_pixel_offset");
        private static readonly int ChunkSizeProperty = Shader.PropertyToID("biome_chunk_size");

        private readonly ComputeShader _shader;
        private readonly BiomeLayerSites _layerSites = new ();
        private readonly List<Vector4> _colors = new ();
        private RenderTexture _debugTex;
        private ComputeBuffer _colorsBuffer;

        public RenderTexture DebugTex => _debugTex;

        public BiomeDebugBaker(ComputeShader shader)
        {
            _shader = shader;
        }

        /// <returns>False when there is nothing to show: no shader or no biomes.</returns>
        public bool Bake(TerrainData data, IReadOnlyList<IBiome> biomes, Matrix4x4 worldToTerrain, float chunkSizeMeters,
            bool sharedEdgePixels, BiomeEdgeNoise edgeNoise)
        {
            if (!_shader || biomes == null || biomes.Count == 0)
            {
                return false;
            }

            if (!_layerSites.Build(data, biomes, worldToTerrain, chunkSizeMeters, edgeNoise.Margin))
            {
                return false;
            }

            EnsureTexture(data);

            _colors.Clear();
            foreach (IBiome biome in biomes)
            {
                Color color = biome.DebugColor.linear;
                _colors.Add(new Vector4(color.r, color.g, color.b, 1));
            }

            _layerSites.Upload();
            BufferUtility.Upload(ref _colorsBuffer, _colors, sizeof(float) * 4);

            int kernel = _shader.FindKernel(KernelName);
            _layerSites.Bind(_shader, kernel);
            edgeNoise.Bind(_shader);
            _shader.SetTexture(kernel, DestinationProperty, _debugTex);
            _shader.SetBuffer(kernel, SlotCoordsProperty, data.SlotCoordsBuffer);
            _shader.SetBuffer(kernel, ColorsProperty, _colorsBuffer);
            _shader.SetInt(ResolutionProperty, data.Resolution);
            _shader.SetFloat(PixelsPerChunkProperty, sharedEdgePixels ? data.Resolution - 1 : data.Resolution);
            _shader.SetFloat(PixelOffsetProperty, sharedEdgePixels ? 0f : 0.5f);
            _shader.SetFloat(ChunkSizeProperty, chunkSizeMeters);

            int groups = Mathf.CeilToInt(data.Resolution / (float)ThreadGroupSize);
            _shader.Dispatch(kernel, groups, groups, data.LayerCount);
            return true;
        }

        private void EnsureTexture(TerrainData data)
        {
            if (_debugTex && _debugTex.width == data.Resolution && _debugTex.volumeDepth == data.LayerCount)
            {
                return;
            }

            ReleaseTexture();
            _debugTex = new RenderTexture(data.Resolution, data.Resolution, 0, GraphicsFormat.R8G8B8A8_UNorm)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = data.LayerCount,
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "Biome debug colors",
            };
            _debugTex.Create();
        }

        private void ReleaseTexture()
        {
            if (!_debugTex)
            {
                return;
            }

            _debugTex.Release();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_debugTex);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_debugTex);
            }
            _debugTex = null;
        }

        public void Dispose()
        {
            ReleaseTexture();
            _layerSites.Dispose();
            _colorsBuffer?.Dispose();
            _colorsBuffer = null;
        }
    }
}
