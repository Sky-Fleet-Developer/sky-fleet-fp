using System;
using Core.TerrainGenerator.Surfaces;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.TerrainGenerator.Biomes.Sources
{
    /// <summary>
    /// Made on the GPU, no files: fractal Perlin noise shaped by a curve. Where it goes is set per destination: the
    /// heights of the biome (<see cref="writeHeight"/>) and the weight of each of the four surface channels
    /// (<see cref="NoiseSurfaceWeight"/>): off, constant, the shaped noise, or its inverse. The noise is a function of
    /// the position, so the tiles continue each other and the biome has no end. Heights are normalized (1 = the height
    /// range of the terrain): they are only as high as <see cref="amplitude"/> says. A biome filter can still turn the
    /// heights into surface weights (e.g. SurfaceFromHeightFilter).
    /// </summary>
    [Serializable]
    public class NoiseBiomeSource : IBiomeSource
    {
        /// <summary>Weight a surface channel gets from the noise. Values match NOISE_WEIGHT_* in NoiseBiomeTile.compute.</summary>
        public enum NoiseSurfaceWeight
        {
            Off = 0,
            /// <summary>1 everywhere.</summary>
            Constant = 1,
            /// <summary>The noise after the curve, 0..1.</summary>
            Noise = 2,
            /// <summary>1 minus the noise after the curve.</summary>
            InverseNoise = 3,
        }

        [Serializable]
        public struct SurfaceDestination
        {
            [PropertyTooltip("Surface type painted by this channel")]
            [HorizontalGroup, HideLabel] public SurfaceChannel surface;
            [PropertyTooltip("Weight of the surface")]
            [HorizontalGroup, HideLabel] public NoiseSurfaceWeight weight;
        }

        private const string ShaderPath = "TerrainGenerator/NoiseBiomeTile";
        private const string KernelName = "CSNoiseTile";
        private const int CurveSamples = 256;

        private static readonly int HeightProperty = Shader.PropertyToID("tile_height");
        private static readonly int AlphaProperty = Shader.PropertyToID("tile_alpha");
        private static readonly int LayerProperty = Shader.PropertyToID("tile_layer");
        private static readonly int ResolutionProperty = Shader.PropertyToID("tile_resolution");
        private static readonly int CoordProperty = Shader.PropertyToID("tile_coord");
        private static readonly int PixelsPerTileProperty = Shader.PropertyToID("tile_pixels_per_tile");
        private static readonly int PixelOffsetProperty = Shader.PropertyToID("tile_pixel_offset");
        private static readonly int TileSizeProperty = Shader.PropertyToID("tile_size_meters");
        private static readonly int WriteHeightProperty = Shader.PropertyToID("tile_write_height");
        private static readonly int ChannelWeightsProperty = Shader.PropertyToID("tile_channel_weights");
        private static readonly int SeedProperty = Shader.PropertyToID("noise_seed");
        private static readonly int WavelengthProperty = Shader.PropertyToID("noise_wavelength_meters");
        private static readonly int OctavesProperty = Shader.PropertyToID("noise_octaves");
        private static readonly int PersistenceProperty = Shader.PropertyToID("noise_persistence");
        private static readonly int LacunarityProperty = Shader.PropertyToID("noise_lacunarity");
        private static readonly int BaseProperty = Shader.PropertyToID("noise_base");
        private static readonly int AmplitudeProperty = Shader.PropertyToID("noise_amplitude");
        private static readonly int CurveProperty = Shader.PropertyToID("noise_curve");

        private static ComputeShader _shader;
        private static int _kernel = -1;
        private static readonly Vector4[] CurveBuffer = new Vector4[CurveSamples / 4];

        [SerializeField] private int seed;
        [PropertyTooltip("Wavelength of the largest octave, in meters")]
        [SerializeField, MinValue(1)] private float wavelengthMeters = 500;
        [SerializeField, PropertyRange(1, 8)] private int octaves = 4;
        [PropertyTooltip("How much weaker each next octave is")]
        [SerializeField, PropertyRange(0, 1)] private float persistence = 0.5f;
        [PropertyTooltip("How much finer each next octave is")]
        [SerializeField, PropertyRange(1, 4)] private float lacunarity = 2;
        [PropertyTooltip("Shapes the noise: the horizontal axis is the noise value 0..1, the vertical one the height 0..1 " +
                 "between the base height and base height + amplitude")]
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        [PropertyTooltip("Normalized height of the noise value 0 (1 = the height range of the terrain)")]
        [SerializeField, PropertyRange(0, 1)] private float baseHeight;
        [PropertyTooltip("Normalized height the curve adds at 1 (1 = the height range of the terrain). A small value " +
                 "makes the noise a faint relief, enough for a filter to read it")]
        [SerializeField, PropertyRange(0, 1)] private float amplitude = 0.02f;
        [PropertyTooltip("Size of a tile in meters. The noise does not depend on it, only the memory of one tile does")]
        [SerializeField, MinValue(1)] private float tileSizeMeters = 1024;
        [PropertyTooltip("Write the noise as the heights of the biome. Off: the biome is flat (height 0)")]
        [SerializeField] private bool writeHeight = true;
        [PropertyTooltip("Surfaces painted by the four alphamap channels, each with its weight from the noise. " +
                 "Weights are normalized between the channels where they are all above 0")]
        [SerializeField, ListDrawerSettings(IsReadOnly = true, ShowFoldout = false)]
        private SurfaceDestination[] surfaces = DefaultSurfaces();
        // Before the destinations: the surface of the whole biome. Still used by channel 0 while that is on Default
        [SerializeField, HideInInspector] private SurfaceChannel groundSurface;

        public float TileSizeMeters => tileSizeMeters;
        public Vector2 Origin => Vector2.zero;
        public bool SharedEdgePixels => true;

        /// <summary>Equal settings give equal content, so biomes with the same noise share the tiles.</summary>
        public string ContentKey => $"noise|{seed}|{wavelengthMeters}|{octaves}|{persistence}|{lacunarity}|{baseHeight}|{amplitude}|" +
                                    $"{tileSizeMeters}|{GetCurveHash()}|{writeHeight}|{GetWeight(0)}{GetWeight(1)}{GetWeight(2)}{GetWeight(3)}";

        public SurfaceChannel GetSurfaceChannel(int channel)
        {
            if (channel < 0 || channel > 3 || GetWeight(channel) == NoiseSurfaceWeight.Off)
            {
                return new SurfaceChannel(SurfaceChannelMode.Ignore);
            }

            SurfaceChannel binding = surfaces != null && channel < surfaces.Length ? surfaces[channel].surface : default;
            return channel == 0 && binding.Mode == SurfaceChannelMode.Default ? groundSurface : binding;
        }

        private NoiseSurfaceWeight GetWeight(int channel)
        {
            return surfaces != null && channel < surfaces.Length ? surfaces[channel].weight : NoiseSurfaceWeight.Off;
        }

        // The ground surface covers everything, like before the destinations existed
        private static SurfaceDestination[] DefaultSurfaces()
        {
            var result = new SurfaceDestination[4];
            result[0].weight = NoiseSurfaceWeight.Constant;
            return result;
        }

        public UniTask<bool> LoadTile(Vector2Int tile, BiomeTileTarget target)
        {
            ComputeShader shader = GetShader();
            if (!shader || !target.IsValid)
            {
                return UniTask.FromResult(false);
            }

            FillCurve();
            shader.SetTexture(_kernel, HeightProperty, target.Height);
            shader.SetTexture(_kernel, AlphaProperty, target.Alpha);
            shader.SetInt(LayerProperty, target.Layer);
            shader.SetInt(ResolutionProperty, target.Resolution);
            shader.SetInts(CoordProperty, tile.x, tile.y);
            shader.SetFloat(PixelsPerTileProperty, target.Resolution - 1);
            shader.SetFloat(PixelOffsetProperty, 0);
            shader.SetFloat(TileSizeProperty, tileSizeMeters);
            shader.SetInt(WriteHeightProperty, writeHeight ? 1 : 0);
            shader.SetInts(ChannelWeightsProperty, (int)GetWeight(0), (int)GetWeight(1), (int)GetWeight(2), (int)GetWeight(3));
            shader.SetInt(SeedProperty, seed);
            shader.SetFloat(WavelengthProperty, Mathf.Max(1, wavelengthMeters));
            shader.SetInt(OctavesProperty, Mathf.Clamp(octaves, 1, 8));
            shader.SetFloat(PersistenceProperty, persistence);
            shader.SetFloat(LacunarityProperty, lacunarity);
            shader.SetFloat(BaseProperty, baseHeight);
            shader.SetFloat(AmplitudeProperty, amplitude);
            shader.SetVectorArray(CurveProperty, CurveBuffer);

            int groups = Mathf.CeilToInt(target.Resolution / 8f);
            shader.Dispatch(_kernel, groups, groups, 1);
            return UniTask.FromResult(true);
        }

        private void FillCurve()
        {
            AnimationCurve source = curve != null && curve.length > 0 ? curve : AnimationCurve.Linear(0, 0, 1, 1);
            for (int i = 0; i < CurveSamples; i += 4)
            {
                var packed = new Vector4();
                for (int k = 0; k < 4; k++)
                {
                    packed[k] = Mathf.Clamp01(source.Evaluate((i + k) / (CurveSamples - 1f)));
                }
                CurveBuffer[i / 4] = packed;
            }
        }

        private int GetCurveHash()
        {
            if (curve == null)
            {
                return 0;
            }

            var hash = new HashCode();
            hash.Add((int)curve.preWrapMode);
            hash.Add((int)curve.postWrapMode);
            foreach (Keyframe key in curve.keys)
            {
                hash.Add(key.time);
                hash.Add(key.value);
                hash.Add(key.inTangent);
                hash.Add(key.outTangent);
            }
            return hash.ToHashCode();
        }

        private static ComputeShader GetShader()
        {
            if (!_shader)
            {
                _shader = Resources.Load<ComputeShader>(ShaderPath);
                if (!_shader)
                {
                    Debug.LogError($"Noise biome source: compute shader not found at Resources/{ShaderPath}");
                    return null;
                }
                _kernel = _shader.FindKernel(KernelName);
            }
            return _shader;
        }
    }
}
