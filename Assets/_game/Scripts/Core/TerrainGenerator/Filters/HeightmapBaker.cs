using System.Collections.Generic;
using Core.TerrainGenerator.Surfaces;
using UnityEngine;

namespace Core.TerrainGenerator.Filters
{
    /// <summary>
    /// Produces TerrainData.HeightmapTex and TerrainData.AlphamapTex from the blended raw heightmap and alphamap
    /// by running the active global filters in order.
    /// Every bake processes all loaded chunks, so chunk edges are recomputed once their neighbours arrive.
    /// </summary>
    public class HeightmapBaker
    {
        private static readonly List<IHeightmapFilter> ActiveFilters = new ();
        private static readonly List<bool> WritesAlphamap = new ();

        public void Bake(TerrainData data, IReadOnlyList<IHeightmapFilter> filters, float chunkSizeMeters, float heightRangeMeters,
            bool sharedEdgePixels, TerrainSurfaces surfaces)
        {
            RunChain(filters, data, data.SlotCoordsBuffer, data.RawHeightmapTex, data.RawAlphamapTex, chunkSizeMeters,
                heightRangeMeters, sharedEdgePixels, surfaces);
        }

        /// <summary>
        /// Runs the active filters in order from the given heights and alphamap. The result ends in TerrainData.HeightmapTex
        /// and TerrainData.AlphamapTex; the scratch textures of TerrainData are used in between. Heights ping-pong
        /// through every filter, the alphamap only through the filters that write it.
        /// </summary>
        /// <param name="heights">Source heights; must not be HeightmapTex or the scratch heightmap.</param>
        /// <param name="alphamap">Source alphamap; must not be AlphamapTex or the scratch alphamap.</param>
        /// <param name="slotCoords">Per layer int3(chunk, isUsed): layers marked unused are skipped by the filters.</param>
        public static void RunChain(IReadOnlyList<IHeightmapFilter> filters, TerrainData data, ComputeBuffer slotCoords,
            RenderTexture heights, RenderTexture alphamap, float chunkSizeMeters, float heightRangeMeters, bool sharedEdgePixels,
            TerrainSurfaces surfaces)
        {
            // Read once: the flags decide the ping-pong order
            ActiveFilters.Clear();
            WritesAlphamap.Clear();
            int alphaWriters = 0;
            foreach (IHeightmapFilter filter in filters)
            {
                if (filter != null && filter.IsActive)
                {
                    ActiveFilters.Add(filter);
                    WritesAlphamap.Add(filter.WritesAlphamap);
                    alphaWriters += filter.WritesAlphamap ? 1 : 0;
                }
            }

            RenderTexture heightsFinal = data.HeightmapTex;
            RenderTexture alphaFinal = data.AlphamapTex;
            if (ActiveFilters.Count == 0)
            {
                Graphics.CopyTexture(heights, heightsFinal);
                Graphics.CopyTexture(alphamap, alphaFinal);
                return;
            }

            if (alphaWriters == 0)
            {
                Graphics.CopyTexture(alphamap, alphaFinal);
            }

            ComputeBuffer map = data.GetMapBuffer(out Vector2Int mapMin, out int mapSize);
            RenderTexture heightsScratch = ActiveFilters.Count > 1 ? data.GetScratchHeightmapTex() : null;
            RenderTexture alphaScratch = alphaWriters > 1 ? data.GetScratchAlphamapTex() : null;

            // Ping-pong so that the last writer of each map writes into its final texture
            RenderTexture heightsSource = heights;
            RenderTexture heightsDestination = ActiveFilters.Count % 2 == 1 ? heightsFinal : heightsScratch;
            RenderTexture alphaSource = alphamap;
            RenderTexture alphaDestination = alphaWriters % 2 == 1 ? alphaFinal : alphaScratch;
            for (int i = 0; i < ActiveFilters.Count; i++)
            {
                bool writesAlphamap = WritesAlphamap[i];
                var context = new HeightmapFilterContext(heightsSource, heightsDestination, alphaSource,
                    writesAlphamap ? alphaDestination : null, slotCoords, map, mapMin, mapSize, data.LayerCount,
                    data.Resolution, chunkSizeMeters, heightRangeMeters, sharedEdgePixels, surfaces);
                ActiveFilters[i].Apply(context);

                heightsSource = heightsDestination;
                heightsDestination = heightsDestination == heightsFinal ? heightsScratch : heightsFinal;
                if (writesAlphamap)
                {
                    alphaSource = alphaDestination;
                    alphaDestination = alphaDestination == alphaFinal ? alphaScratch : alphaFinal;
                }
            }
        }
    }
}
