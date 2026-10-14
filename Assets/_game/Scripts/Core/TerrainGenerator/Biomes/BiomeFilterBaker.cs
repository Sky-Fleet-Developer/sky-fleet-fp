using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Filters;
using Unity.Mathematics;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes
{
    /// <summary>
    /// Filters the heights and the alphamap of every biome with active filters before the biomes are blended,
    /// so blending (and the Max / Min layers in particular) compares the filtered terrain.
    /// <para>
    /// Each filtered biome has its own heights and alphamap (<see cref="GetOwnHeightTex"/>, <see cref="GetOwnAlphaTex"/>,
    /// written by <see cref="BiomeComposer"/>) with the layers of <see cref="TerrainData"/>. Its filters run on them
    /// in order, only on the layers where the biome has influence: filters see the other layers as unused
    /// (HeightmapFilterCommon.hlsl), while neighbour reads still find the biome's own data there.
    /// Results go to <see cref="FilteredHeights"/> and, for biomes with filters that write the alphamap,
    /// <see cref="FilteredAlpha"/>: one block of layers per biome.
    /// </para>
    /// </summary>
    public class BiomeFilterBaker : IDisposable
    {
        private class FilteredBiome
        {
            public IBiome Biome;
            public int Index;
            public int AlphaSlot;
            public RenderTexture OwnHeight;
            public RenderTexture OwnAlpha;
        }

        private readonly List<FilteredBiome> _filtered = new ();
        private readonly List<FilteredBiome> _previous = new ();
        private readonly BiomeLayerSites _layerSites = new ();
        private readonly List<IHeightmapFilter> _activeFilters = new ();
        // One per filtered biome, so the dispatches queued for one biome keep their own content
        private readonly List<ComputeBuffer> _slotCoordsBuffers = new ();
        private int3[] _slotCoords;
        private int _alphaSlots;
        private RenderTexture _filteredHeights;
        private RenderTexture _filteredAlpha;
        private RenderTexture _emptyHeights;
        private RenderTexture _emptyAlpha;

        /// <summary>Number of biomes with active filters, as of the last <see cref="UpdateFilteredBiomes"/>.</summary>
        public int Count => _filtered.Count;

        /// <summary>One block of TerrainData.LayerCount layers per filtered biome, in slot order. Never null.</summary>
        public RenderTexture FilteredHeights => _filtered.Count > 0 && _filteredHeights ? _filteredHeights : _emptyHeights;

        /// <summary>One block of TerrainData.LayerCount layers per alpha slot (<see cref="GetAlphaSlot"/>). Never null.</summary>
        public RenderTexture FilteredAlpha => _alphaSlots > 0 && _filteredAlpha ? _filteredAlpha : _emptyAlpha;

        /// <summary>
        /// Finds the biomes with active filters. Returns true when that set changed: their own data is then
        /// missing or outdated, so every chunk has to be composed again.
        /// </summary>
        public bool UpdateFilteredBiomes(IReadOnlyList<IBiome> biomes, TerrainData data)
        {
            _previous.Clear();
            _previous.AddRange(_filtered);
            _filtered.Clear();
            _alphaSlots = 0;
            for (int i = 0; i < biomes.Count; i++)
            {
                CollectActiveFilters(biomes[i]);
                if (_activeFilters.Count == 0)
                {
                    continue;
                }

                FilteredBiome entry = _previous.Find(previous => previous.Biome == biomes[i]) ?? new FilteredBiome { Biome = biomes[i] };
                entry.Index = i;
                entry.AlphaSlot = _activeFilters.Exists(filter => filter.WritesAlphamap) ? _alphaSlots++ : -1;
                EnsureTexture(ref entry.OwnHeight, data, data.RawHeightmapTex, 1, "Biome own heights");
                EnsureTexture(ref entry.OwnAlpha, data, data.RawAlphamapTex, 1, "Biome own alphamap");
                _filtered.Add(entry);
            }

            bool changed = _filtered.Count != _previous.Count;
            for (int i = 0; i < _filtered.Count && !changed; i++)
            {
                changed = _filtered[i] != _previous[i];
            }

            foreach (FilteredBiome previous in _previous)
            {
                if (!_filtered.Contains(previous))
                {
                    ReleaseTexture(ref previous.OwnHeight);
                    ReleaseTexture(ref previous.OwnAlpha);
                }
            }
            _previous.Clear();

            EnsureTexture(ref _emptyHeights, 1, 1, 1, data.RawHeightmapTex, "Biome filtered heights (empty)");
            EnsureTexture(ref _emptyAlpha, 1, 1, 1, data.RawAlphamapTex, "Biome filtered alphamap (empty)");
            if (_filtered.Count > 0)
            {
                EnsureTexture(ref _filteredHeights, data, data.RawHeightmapTex, _filtered.Count, "Biome filtered heights");
            }
            else
            {
                ReleaseTexture(ref _filteredHeights);
            }

            if (_alphaSlots > 0)
            {
                EnsureTexture(ref _filteredAlpha, data, data.RawAlphamapTex, _alphaSlots, "Biome filtered alphamap");
            }
            else
            {
                ReleaseTexture(ref _filteredAlpha);
            }
            return changed;
        }

        /// <summary>Slot of the biome in <see cref="FilteredHeights"/>, -1 when it has no active filters.</summary>
        public int GetSlot(int biomeIndex)
        {
            for (int i = 0; i < _filtered.Count; i++)
            {
                if (_filtered[i].Index == biomeIndex)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Slot of the biome in <see cref="FilteredAlpha"/>, -1 when none of its filters writes the alphamap.</summary>
        public int GetAlphaSlot(int biomeIndex)
        {
            int slot = GetSlot(biomeIndex);
            return slot >= 0 ? _filtered[slot].AlphaSlot : -1;
        }

        /// <summary>Own heights of the biome in a slot: written by the composer, input of the filters.</summary>
        public RenderTexture GetOwnHeightTex(int slot) => _filtered[slot].OwnHeight;

        /// <summary>Own alphamap of the biome in a slot: written by the composer, input of the filters.</summary>
        public RenderTexture GetOwnAlphaTex(int slot) => _filtered[slot].OwnAlpha;

        /// <summary>Biome index of a slot in the biome list.</summary>
        public int GetBiomeIndex(int slot) => _filtered[slot].Index;

        /// <summary>
        /// Runs the filters of every filtered biome on its own data into <see cref="FilteredHeights"/> and
        /// <see cref="FilteredAlpha"/>. Uses HeightmapTex, AlphamapTex and the scratch textures of TerrainData
        /// as intermediate targets, so the terrain must be baked after it.
        /// </summary>
        /// <param name="borderMargin">How far biome borders may be bent, see <see cref="Settings.BiomeEdgeNoise.Margin"/>.</param>
        public void Filter(TerrainData data, IReadOnlyList<IBiome> biomes, Matrix4x4 worldToTerrain,
            float chunkSizeMeters, float heightRangeMeters, bool sharedEdgePixels, float borderMargin)
        {
            if (_filtered.Count == 0 || !_layerSites.Build(data, biomes, worldToTerrain, chunkSizeMeters, borderMargin))
            {
                return;
            }

            for (int slot = 0; slot < _filtered.Count; slot++)
            {
                FilteredBiome biome = _filtered[slot];
                CollectActiveFilters(biome.Biome);
                if (_activeFilters.Count == 0 || !BuildSlotCoords(data, biome.Index))
                {
                    continue;
                }

                ComputeBuffer slotCoords = GetSlotCoordsBuffer(slot, data.LayerCount);
                slotCoords.SetData(_slotCoords);

                // Filters write only the layers of the biome, but read neighbour layers from their sources:
                // every intermediate texture must hold the biome's own data there
                int alphaWriters = _activeFilters.FindAll(filter => filter.WritesAlphamap).Count;
                if (_activeFilters.Count > 1)
                {
                    Graphics.CopyTexture(biome.OwnHeight, data.HeightmapTex);
                    Graphics.CopyTexture(biome.OwnHeight, data.GetScratchHeightmapTex());
                }
                if (alphaWriters > 0)
                {
                    Graphics.CopyTexture(biome.OwnAlpha, data.AlphamapTex);
                }
                if (alphaWriters > 1)
                {
                    Graphics.CopyTexture(biome.OwnAlpha, data.GetScratchAlphamapTex());
                }

                HeightmapBaker.RunChain(_activeFilters, data, slotCoords, biome.OwnHeight, biome.OwnAlpha,
                    chunkSizeMeters, heightRangeMeters, sharedEdgePixels);

                // Blending reads the filtered data only where the biome has influence
                for (int layer = 0; layer < data.LayerCount; layer++)
                {
                    if (_slotCoords[layer].z == 0)
                    {
                        continue;
                    }

                    Graphics.CopyTexture(data.HeightmapTex, layer, 0, _filteredHeights, slot * data.LayerCount + layer, 0);
                    if (biome.AlphaSlot >= 0)
                    {
                        Graphics.CopyTexture(data.AlphamapTex, layer, 0, _filteredAlpha, biome.AlphaSlot * data.LayerCount + layer, 0);
                    }
                }
            }
        }

        private void CollectActiveFilters(IBiome biome)
        {
            _activeFilters.Clear();
            IReadOnlyList<IHeightmapFilterSource> filters = biome.Filters;
            if (filters == null)
            {
                return;
            }

            foreach (IHeightmapFilterSource source in filters)
            {
                // Filters are read at bake time: a source may get a different filter assigned
                if (source?.Filter != null && source.Filter.IsActive)
                {
                    _activeFilters.Add(source.Filter);
                }
            }
        }

        /// <returns>False when the biome has no influence on any loaded layer.</returns>
        private bool BuildSlotCoords(TerrainData data, int biome)
        {
            if (_slotCoords == null || _slotCoords.Length != data.LayerCount)
            {
                _slotCoords = new int3[data.LayerCount];
            }

            bool any = false;
            for (int layer = 0; layer < data.LayerCount; layer++)
            {
                int3 slot = data.GetSlotCoords(layer);
                if (slot.z != 0 && !_layerSites.HasInfluence(layer, biome))
                {
                    slot.z = 0;
                }
                _slotCoords[layer] = slot;
                any |= slot.z != 0;
            }
            return any;
        }

        private ComputeBuffer GetSlotCoordsBuffer(int index, int layerCount)
        {
            while (_slotCoordsBuffers.Count <= index)
            {
                _slotCoordsBuffers.Add(null);
            }

            ComputeBuffer buffer = _slotCoordsBuffers[index];
            if (buffer == null || buffer.count != layerCount)
            {
                buffer?.Dispose();
                buffer = new ComputeBuffer(layerCount, sizeof(int) * 3);
                _slotCoordsBuffers[index] = buffer;
            }
            return buffer;
        }

        // An array like the format texture with blocks x TerrainData.LayerCount layers
        private static void EnsureTexture(ref RenderTexture texture, TerrainData data, RenderTexture format, int blocks,
            string name)
        {
            EnsureTexture(ref texture, data.Resolution, data.Resolution, blocks * data.LayerCount, format, name);
        }

        private static void EnsureTexture(ref RenderTexture texture, int width, int height, int layers, RenderTexture format,
            string name)
        {
            if (texture && texture.width == width && texture.height == height && texture.volumeDepth == layers
                && texture.graphicsFormat == format.graphicsFormat)
            {
                return;
            }

            ReleaseTexture(ref texture);
            RenderTextureDescriptor descriptor = format.descriptor;
            descriptor.width = width;
            descriptor.height = height;
            descriptor.volumeDepth = layers;
            texture = new RenderTexture(descriptor)
            {
                filterMode = format.filterMode,
                name = name,
            };
            texture.Create();
        }

        private static void ReleaseTexture(ref RenderTexture texture)
        {
            if (!texture)
            {
                return;
            }

            texture.Release();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            texture = null;
        }

        public void Dispose()
        {
            foreach (FilteredBiome biome in _filtered)
            {
                ReleaseTexture(ref biome.OwnHeight);
                ReleaseTexture(ref biome.OwnAlpha);
            }
            _filtered.Clear();
            ReleaseTexture(ref _filteredHeights);
            ReleaseTexture(ref _filteredAlpha);
            ReleaseTexture(ref _emptyHeights);
            ReleaseTexture(ref _emptyAlpha);
            _layerSites.Dispose();
            foreach (ComputeBuffer buffer in _slotCoordsBuffers)
            {
                buffer?.Dispose();
            }
            _slotCoordsBuffers.Clear();
        }
    }
}
