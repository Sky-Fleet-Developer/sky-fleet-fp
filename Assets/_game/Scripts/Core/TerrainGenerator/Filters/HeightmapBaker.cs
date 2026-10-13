using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Filters
{
    /// <summary>
    /// Produces TerrainData.HeightmapTex from TerrainData.RawHeightmapTex by running the active filters in order.
    /// Every bake processes all loaded chunks, so chunk edges are recomputed once their neighbours arrive.
    /// </summary>
    public class HeightmapBaker
    {
        private readonly List<IHeightmapFilter> _activeFilters = new();

        public void Bake(TerrainData data, IReadOnlyList<IHeightmapFilter> filters, float chunkSizeMeters, float heightRangeMeters,
            bool sharedEdgePixels)
        {
            _activeFilters.Clear();
            foreach (IHeightmapFilter filter in filters)
            {
                if (filter != null && filter.IsActive)
                {
                    _activeFilters.Add(filter);
                }
            }

            if (_activeFilters.Count == 0)
            {
                Graphics.CopyTexture(data.RawHeightmapTex, data.HeightmapTex);
                return;
            }

            ComputeBuffer map = data.GetMapBuffer(out Vector2Int mapMin, out int mapSize);
            RenderTexture final = data.HeightmapTex;
            RenderTexture scratch = _activeFilters.Count > 1 ? data.GetScratchHeightmapTex() : null;

            // Ping-pong so that the last filter writes into the final texture
            RenderTexture source = data.RawHeightmapTex;
            RenderTexture destination = _activeFilters.Count % 2 == 1 ? final : scratch;
            foreach (IHeightmapFilter filter in _activeFilters)
            {
                var context = new HeightmapFilterContext(source, destination, data.SlotCoordsBuffer, map, mapMin,
                    mapSize, data.LayerCount, data.Resolution, chunkSizeMeters, heightRangeMeters, sharedEdgePixels);
                filter.Apply(context);

                source = destination;
                destination = destination == final ? scratch : final;
            }
        }
    }
}
