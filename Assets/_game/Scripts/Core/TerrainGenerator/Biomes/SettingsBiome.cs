using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Biomes.Sources;
using Core.TerrainGenerator.Filters;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// The whole terrain as one biome made from the global settings: the files of TerrainGenerationSettings,
    /// with tile ChunksCenter at the terrain origin. Used when the scene has no biomes.
    /// </summary>
    public class SettingsBiome : IBiome
    {
        private readonly TerrainGenerationSettings _settings;
        private readonly Transform _terrain;
        private readonly RawTileBiomeSource _source = new ();

        public SettingsBiome(TerrainGenerationSettings settings, Transform terrain)
        {
            _settings = settings;
            _terrain = terrain;
            Refresh();
        }

        public float Radius => 0;
        public float BlendDistance => 0;
        public Color DebugColor => Color.white;
        public IBiomeSource Source => _source;
        // Global filters already cover the whole terrain
        public IReadOnlyList<IHeightmapFilterSource> Filters => Array.Empty<IHeightmapFilterSource>();

        /// <summary>Applies changed settings to the source.</summary>
        public void Refresh()
        {
            _source.Configure(_settings.TargetDirectory, _settings.heightmapSettings.formatMap,
                _settings.alphamapSettings.splatmapFormats.Count > 0 ? _settings.alphamapSettings.splatmapFormats[0] : null,
                _settings.heightmapSettings.chunkSizeMeters, _settings.ChunksCenter, _settings.heightmapSettings.sharedEdgePixels);
        }

        public void GetSites(List<BiomeSitePose> sites)
        {
            sites.Add(new BiomeSitePose(_terrain.position, _terrain.eulerAngles.y));
        }
    }
}
