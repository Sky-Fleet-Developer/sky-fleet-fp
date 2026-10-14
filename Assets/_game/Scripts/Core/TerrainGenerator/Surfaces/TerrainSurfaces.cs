using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.TerrainGenerator.Biomes;
using Core.TerrainGenerator.Biomes.Sources;
using Core.TerrainGenerator.Settings;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Core.TerrainGenerator.Surfaces
{
    /// <summary>
    /// The surface types used by the terrain, compiled for the GPU: texture arrays with one layer per surface
    /// and a buffer of surface parameters (SampleTerrainSurface.hlsl). The id of a surface is its layer and the
    /// value stored in the alphamap (SurfaceAlphamap.hlsl).
    /// <para>
    /// Ids: the surfaces of <see cref="SurfaceSettings"/> first, in order, so channel i defaults to id i;
    /// then the other surfaces bound by biome sources, in biome order.
    /// </para>
    /// </summary>
    public class TerrainSurfaces : IDisposable
    {
        /// <summary>Ids are stored in 8 bits.</summary>
        public const int MaxSurfaces = 256;

        [StructLayout(LayoutKind.Sequential)]
        private struct SurfaceData
        {
            public const int Stride = sizeof(float) * 3;

            public float Metallic;
            public float Smoothness;
            public float TileSize;
        }

        private static readonly int AlbedoProperty = Shader.PropertyToID("terrain_surface_albedo");
        private static readonly int NormalProperty = Shader.PropertyToID("terrain_surface_normal");
        private static readonly int HeightProperty = Shader.PropertyToID("terrain_surface_height");
        private static readonly int SurfacesProperty = Shader.PropertyToID("terrain_surfaces");
        private static readonly int CountProperty = Shader.PropertyToID("terrain_surface_count");
        private static readonly int BlendDepthProperty = Shader.PropertyToID("terrain_surface_blend_depth");

        private readonly List<SurfaceType> _surfaces = new ();
        private readonly List<SurfaceType> _collected = new ();
        private readonly List<SurfaceData> _data = new ();
        private int _settingsCount;
        private bool _texturesDirty = true;
        private int _resolution;
        private RenderTexture _albedo;
        private RenderTexture _normal;
        private RenderTexture _height;
        private ComputeBuffer _buffer;
        private Texture2D _flatNormal;
        private Texture2D _middleHeight;

        public IReadOnlyList<SurfaceType> Surfaces => _surfaces;

        /// <summary>Raised when a surface type was edited: the textures are rebuilt on the next <see cref="Bind"/>.</summary>
        public event Action Changed;

        /// <summary>
        /// Collects the surfaces of the settings and of the biome sources. Returns true when the ids changed,
        /// so every alphamap written with the old ids is outdated.
        /// </summary>
        public bool Collect(SurfaceSettings settings, IReadOnlyList<IBiome> biomes)
        {
            _collected.Clear();
            if (settings != null)
            {
                _collected.AddRange(settings.Surfaces);
            }
            _settingsCount = _collected.Count;

            foreach (IBiome biome in biomes)
            {
                IBiomeSource source = biome.Source;
                if (source == null)
                {
                    continue;
                }

                for (int channel = 0; channel < 4; channel++)
                {
                    SurfaceChannel binding = source.GetSurfaceChannel(channel);
                    if (binding.Mode == SurfaceChannelMode.Surface && binding.Surface && !_collected.Contains(binding.Surface))
                    {
                        _collected.Add(binding.Surface);
                    }
                }
            }

            if (_collected.Count > MaxSurfaces)
            {
                Debug.LogError($"Terrain uses {_collected.Count} surface types, only {MaxSurfaces} are supported");
                _collected.RemoveRange(MaxSurfaces, _collected.Count - MaxSurfaces);
            }

            bool changed = _collected.Count != _surfaces.Count;
            for (int i = 0; i < _collected.Count && !changed; i++)
            {
                changed = _collected[i] != _surfaces[i];
            }

            if (changed)
            {
                foreach (SurfaceType surface in _surfaces)
                {
                    if (surface)
                    {
                        surface.Changed -= OnSurfaceChanged;
                    }
                }

                _surfaces.Clear();
                _surfaces.AddRange(_collected);
                foreach (SurfaceType surface in _surfaces)
                {
                    if (surface)
                    {
                        surface.Changed += OnSurfaceChanged;
                    }
                }
                _texturesDirty = true;
            }
            return changed;
        }

        /// <summary>Surface id of each channel of the source alphamap, -1 for channels that paint nothing.</summary>
        public int4 GetChannelIds(IBiomeSource source)
        {
            var ids = new int4(-1);
            if (source == null)
            {
                return ids;
            }

            for (int channel = 0; channel < 4; channel++)
            {
                SurfaceChannel binding = source.GetSurfaceChannel(channel);
                ids[channel] = binding.Mode switch
                {
                    SurfaceChannelMode.Default => channel < _settingsCount ? channel : -1,
                    SurfaceChannelMode.Surface => binding.Surface ? _surfaces.IndexOf(binding.Surface) : -1,
                    _ => -1,
                };
            }
            return ids;
        }

        /// <summary>Builds the textures when needed and binds everything to the terrain material.</summary>
        public void Bind(Material material, SurfaceSettings settings)
        {
            int resolution = Mathf.Max(16, settings?.TextureResolution ?? 1024);
            if (_texturesDirty || resolution != _resolution || !_albedo)
            {
                Build(resolution);
            }

            material.SetTexture(AlbedoProperty, _albedo);
            material.SetTexture(NormalProperty, _normal);
            material.SetTexture(HeightProperty, _height);
            material.SetBuffer(SurfacesProperty, _buffer);
            material.SetFloat(CountProperty, _surfaces.Count);
            material.SetFloat(BlendDepthProperty, settings?.BlendDepth ?? 0.2f);
        }

        private void OnSurfaceChanged()
        {
            _texturesDirty = true;
            Changed?.Invoke();
        }

        private void Build(int resolution)
        {
            _texturesDirty = false;
            _resolution = resolution;
            int layers = Mathf.Max(1, _surfaces.Count);
            EnsureArray(ref _albedo, resolution, layers, GraphicsFormat.R8G8B8A8_SRGB, "Terrain surface albedo");
            EnsureArray(ref _normal, resolution, layers, GraphicsFormat.R8G8B8A8_UNorm, "Terrain surface normal");
            EnsureArray(ref _height, resolution, layers, GraphicsFormat.R8_UNorm, "Terrain surface height");
            EnsureDefaults();

            _data.Clear();
            for (int i = 0; i < layers; i++)
            {
                SurfaceType surface = i < _surfaces.Count ? _surfaces[i] : null;
                // Blit resamples to the array size and converts color spaces
                Graphics.Blit(surface && surface.Albedo ? surface.Albedo : Texture2D.whiteTexture, _albedo, 0, i);
                Graphics.Blit(surface && surface.Normal ? surface.Normal : _flatNormal, _normal, 0, i);
                Graphics.Blit(surface && surface.Height ? surface.Height : _middleHeight, _height, 0, i);
                _data.Add(new SurfaceData
                {
                    Metallic = surface ? surface.Metallic : 0,
                    Smoothness = surface ? 1 - surface.Roughness : 0.3f,
                    TileSize = surface ? surface.TileSize : 4,
                });
            }

            _albedo.GenerateMips();
            _normal.GenerateMips();
            _height.GenerateMips();

            if (_buffer == null || _buffer.count != _data.Count)
            {
                _buffer?.Dispose();
                _buffer = new ComputeBuffer(_data.Count, SurfaceData.Stride);
            }
            _buffer.SetData(_data);
        }

        private void EnsureDefaults()
        {
            // Normal maps are packed: (1, 0.5, 0.5, 0.5) reads as flat in both the RG and the AG layouts
            if (!_flatNormal)
            {
                _flatNormal = CreatePixel(new Color(1f, 0.5f, 0.5f, 0.5f), "Terrain flat normal");
            }
            if (!_middleHeight)
            {
                _middleHeight = CreatePixel(new Color(0.5f, 0.5f, 0.5f, 1f), "Terrain middle height");
            }
        }

        private static Texture2D CreatePixel(Color color, string name)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
            {
                name = name,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return texture;
        }

        private static void EnsureArray(ref RenderTexture texture, int resolution, int layers, GraphicsFormat format, string name)
        {
            if (texture && texture.width == resolution && texture.volumeDepth == layers)
            {
                return;
            }

            Release(texture);
            var descriptor = new RenderTextureDescriptor(resolution, resolution, format, 0)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = layers,
                useMipMap = true,
                autoGenerateMips = false,
                msaaSamples = 1,
            };
            texture = new RenderTexture(descriptor)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
                name = name,
            };
            texture.Create();
        }

        private static void Release(UnityEngine.Object texture)
        {
            if (!texture)
            {
                return;
            }

            if (texture is RenderTexture renderTexture)
            {
                renderTexture.Release();
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        public void Dispose()
        {
            foreach (SurfaceType surface in _surfaces)
            {
                if (surface)
                {
                    surface.Changed -= OnSurfaceChanged;
                }
            }
            _surfaces.Clear();
            Release(_albedo);
            Release(_normal);
            Release(_height);
            Release(_flatNormal);
            Release(_middleHeight);
            _buffer?.Dispose();
            _buffer = null;
        }
    }
}
