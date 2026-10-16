using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// Min and max baked height of every loaded heightmap chunk. Measured on the GPU with atomics after each heightmap
    /// bake and read back asynchronously, so the ranges lag the bake by a few frames.
    /// </summary>
    public class ChunkHeightRanges : IDisposable
    {
        private const string ShaderPath = "TerrainGenerator/ChunkHeightRange";
        private const int ReduceGroupSize = 8;
        private const int ClearGroupSize = 64;
        private const float IntToHeight = 1f / 32767f;

        private static readonly int SourceProperty = Shader.PropertyToID("height_range_source");
        private static readonly int SlotCoordsProperty = Shader.PropertyToID("height_range_slot_coords");
        private static readonly int ResultProperty = Shader.PropertyToID("height_range_result");
        private static readonly int ResolutionProperty = Shader.PropertyToID("height_range_resolution");
        private static readonly int LayerCountProperty = Shader.PropertyToID("height_range_layer_count");

        private static ComputeShader _shader;
        private static int _clearKernel;
        private static int _reduceKernel;

        // Normalized heights, x = min, y = max
        private readonly Dictionary<Vector2Int, Vector2> _ranges = new ();
        private ComputeBuffer _resultBuffer;
        private int _requestedVersion;
        private int _appliedVersion;
        private bool _disposed;

        /// <summary>Raised on the main thread when the ranges of a newer bake have arrived.</summary>
        public event Action Changed;

        /// <summary>Starts measuring the baked heightmap of every used layer. Call right after the heightmap bake.</summary>
        public void Measure(TerrainData data)
        {
            if (_disposed || !SystemInfo.supportsAsyncGPUReadback || !GetShader())
            {
                return;
            }

            int layerCount = data.LayerCount;
            if (_resultBuffer == null || _resultBuffer.count != layerCount * 2)
            {
                _resultBuffer?.Dispose();
                _resultBuffer = new ComputeBuffer(layerCount * 2, sizeof(int));
            }

            // Slots can be reassigned before the readback arrives: results are keyed by the chunks of this moment
            var chunks = new int3[layerCount];
            for (int layer = 0; layer < layerCount; layer++)
            {
                chunks[layer] = data.GetSlotCoords(layer);
            }

            _shader.SetInt(LayerCountProperty, layerCount);
            _shader.SetBuffer(_clearKernel, ResultProperty, _resultBuffer);
            _shader.Dispatch(_clearKernel, Mathf.CeilToInt(layerCount / (float)ClearGroupSize), 1, 1);

            _shader.SetInt(ResolutionProperty, data.Resolution);
            _shader.SetTexture(_reduceKernel, SourceProperty, data.HeightmapTex);
            _shader.SetBuffer(_reduceKernel, SlotCoordsProperty, data.SlotCoordsBuffer);
            _shader.SetBuffer(_reduceKernel, ResultProperty, _resultBuffer);
            int groups = Mathf.CeilToInt(data.Resolution / (float)ReduceGroupSize);
            _shader.Dispatch(_reduceKernel, groups, groups, layerCount);

            int version = ++_requestedVersion;
            AsyncGPUReadback.Request(_resultBuffer, request => OnReadback(request, chunks, version));
        }

        private void OnReadback(AsyncGPUReadbackRequest request, int3[] chunks, int version)
        {
            // Bakes can overtake each other's readbacks: an older result must not replace a newer one
            if (_disposed || request.hasError || version <= _appliedVersion)
            {
                return;
            }

            _appliedVersion = version;
            NativeArray<int> result = request.GetData<int>();
            _ranges.Clear();
            for (int layer = 0; layer < chunks.Length; layer++)
            {
                int3 chunk = chunks[layer];
                int min = result[layer * 2];
                int max = result[layer * 2 + 1];
                if (chunk.z == 0 || min > max)
                {
                    continue;
                }

                _ranges[new Vector2Int(chunk.x, chunk.y)] = new Vector2(min * IntToHeight, max * IntToHeight);
            }

            Changed?.Invoke();
        }

        /// <summary>Normalized height range over all chunks of the rectangle.</summary>
        /// <returns>False when any of the chunks has not been measured yet.</returns>
        public bool TryGetRange(RectInt chunks, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            for (int x = chunks.xMin; x < chunks.xMax; x++)
            {
                for (int y = chunks.yMin; y < chunks.yMax; y++)
                {
                    if (!_ranges.TryGetValue(new Vector2Int(x, y), out Vector2 range))
                    {
                        return false;
                    }

                    min = Mathf.Min(min, range.x);
                    max = Mathf.Max(max, range.y);
                }
            }

            return min <= max;
        }

        private static ComputeShader GetShader()
        {
            if (!_shader)
            {
                _shader = Resources.Load<ComputeShader>(ShaderPath);
                if (!_shader)
                {
                    Debug.LogError($"Chunk height ranges: compute shader not found at Resources/{ShaderPath}");
                    return null;
                }
                _clearKernel = _shader.FindKernel("CSClear");
                _reduceKernel = _shader.FindKernel("CSReduce");
            }
            return _shader;
        }

        public void Dispose()
        {
            _disposed = true;
            _resultBuffer?.Dispose();
            _resultBuffer = null;
            _ranges.Clear();
        }
    }
}
