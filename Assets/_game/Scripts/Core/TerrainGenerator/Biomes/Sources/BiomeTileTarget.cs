using System;
using Core.TerrainGenerator.Settings;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes.Sources
{
    /// <summary>
    /// A tile pool layer a source writes one tile into. Writes are ignored once the tile is no longer needed,
    /// because the layer may already hold another tile by then.
    /// </summary>
    public class BiomeTileTarget
    {
        private readonly MapGpuWorker _gpuWorker;
        private readonly Func<int, ComputeBuffer> _getUploadBuffer;

        /// <summary>Heights, Texture2DArray, normalized 0..1.</summary>
        public RenderTexture Height { get; }
        /// <summary>Alphamap, Texture2DArray RGBA: one surface weight per channel.</summary>
        public RenderTexture Alpha { get; }
        public int Layer { get; }
        public int Resolution { get; }
        /// <summary>False once the tile was released; a source should stop working on it.</summary>
        public bool IsValid { get; private set; } = true;

        public BiomeTileTarget(RenderTexture height, RenderTexture alpha, int layer, int resolution, MapGpuWorker gpuWorker,
            Func<int, ComputeBuffer> getUploadBuffer)
        {
            Height = height;
            Alpha = alpha;
            Layer = layer;
            Resolution = resolution;
            _gpuWorker = gpuWorker;
            _getUploadBuffer = getUploadBuffer;
        }

        public void Invalidate() => IsValid = false;

        /// <summary>Uploads square heights as read by RawReader, resampled to the pool resolution.</summary>
        /// <param name="sharedEdgePixels">Pixel layout of the source, <see cref="IBiomeSource.SharedEdgePixels"/>.</param>
        public void UploadHeights(float[,] heights, bool sharedEdgePixels)
        {
            if (!IsValid)
            {
                return;
            }

            ComputeBuffer buffer = _getUploadBuffer(heights.Length);
            buffer.SetData(heights);
            _gpuWorker.InsertHeightmapDataToBuffer(buffer, heights.GetLength(0), Height, Layer, Resolution, sharedEdgePixels);
        }

        /// <summary>
        /// Uploads a square alphamap as read from a PNG (pixels[x + y * width], y up), resampled to the pool resolution.
        /// It covers the tile exactly like the heights.
        /// </summary>
        /// <param name="sharedEdgePixels">Pixel layout of the source, <see cref="IBiomeSource.SharedEdgePixels"/>.</param>
        public void UploadAlpha(Color32[] alpha, bool sharedEdgePixels)
        {
            if (!IsValid)
            {
                return;
            }

            ComputeBuffer buffer = _getUploadBuffer(alpha.Length);
            buffer.SetData(alpha);
            _gpuWorker.InsertAlphamapDataToBuffer(buffer, (int)Mathf.Sqrt(alpha.Length), Alpha, Layer, Resolution, sharedEdgePixels);
        }
    }
}
