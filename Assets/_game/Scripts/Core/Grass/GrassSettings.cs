using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Surfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Grass
{
    /// <summary>
    /// Grass of the terrain: which surface types grow it and how it looks, and how far and how densely it is drawn.
    /// </summary>
    [CreateAssetMenu(menuName = "Terrain/Grass Settings")]
    public class GrassSettings : ScriptableObject
    {
        [Title("Range")]
        [PropertyTooltip("Meters from the camera where the blades end. Past it the terrain takes the grass color")]
        [SerializeField, MinValue(10)] private float range = 500;
        [PropertyTooltip("Part of the range over which the blades fade out and the terrain color fades in")]
        [SerializeField, PropertyRange(0.02f, 0.5f)] private float fade = 0.15f;
        [PropertyTooltip("Meters within which the grass has its full density. Farther the density falls with the square of " +
                 "the distance and the blades get wider to keep covering the ground")]
        [SerializeField, MinValue(1)] private float fullDensityDistance = 15;
        [PropertyTooltip("Largest widening of far blades")]
        [SerializeField, PropertyRange(1, 8)] private float maxWidening = 4;
        [PropertyTooltip("Blades closer than this many meters use the detailed, curved mesh; farther ones a single triangle")]
        [SerializeField, MinValue(0)] private float detailedMeshDistance = 25;
        [PropertyTooltip("Segments of the detailed blade mesh")]
        [SerializeField, PropertyRange(2, 8)] private int detailedSegments = 4;
        [PropertyTooltip("Size in meters of the tiles the placement is culled by")]
        [SerializeField, PropertyRange(2, 32)] private float tileSize = 8;
        [PropertyTooltip("Largest number of detailed built-in blades. Blades past it are not drawn. The blade mesh holds a " +
                 "blade for every one of them. Layers with their own shape have their own limits (GrassShape)")]
        [SerializeField, MinValue(1024)] private int maxDetailedBlades = 100_000;
        [PropertyTooltip("Largest number of simple built-in blades. Blades past it are not drawn. The blade mesh holds a " +
                 "blade for every one of them")]
        [SerializeField, MinValue(1024)] private int maxBlades = 300_000;

        [Title("Culling")]
        [PropertyTooltip("Tiles hidden behind hills are skipped: the terrain between the camera and the tile is checked")]
        [SerializeField] private bool terrainOcclusion = true;
        [PropertyTooltip("Heightmap samples along each line of sight")]
        [SerializeField, PropertyRange(4, 64)] private int occlusionSteps = 16;

        [Title("Look")]
        [PropertyTooltip("Darkening at the roots, a stand-in for the shadows of the blades on each other")]
        [SerializeField, PropertyRange(0, 1)] private float rootDarkening = 0.5f;
        [SerializeField, PropertyRange(0, 1)] private float smoothness = 0.2f;
        [PropertyTooltip("How much the blades are lit like the ground under them rather than by their own facing")]
        [SerializeField, PropertyRange(0, 1)] private float terrainNormal = 0.7f;
        [PropertyTooltip("Blades closer than Detailed Mesh Distance cast shadows. Draws them once more for every shadow " +
                 "map; farther blades never do")]
        [SerializeField] private bool castShadows;

        [Title("Wind")]
        [PropertyTooltip("Direction the wind blows to, degrees clockwise from +z")]
        [SerializeField, PropertyRange(0, 360)] private float windDirection = 45;
        [PropertyTooltip("Lean of the blades in the wind, in blade heights")]
        [SerializeField, PropertyRange(0, 2)] private float windStrength = 0.3f;
        [PropertyTooltip("Meters between the waves running over the grass")]
        [SerializeField, MinValue(0.1f)] private float windWaveLength = 20;
        [PropertyTooltip("Speed of the waves, meters per second")]
        [SerializeField, MinValue(0)] private float windSpeed = 6;
        [PropertyTooltip("How much the strength changes from wave to wave")]
        [SerializeField, PropertyRange(0, 1)] private float windGustiness = 0.6f;
        [PropertyTooltip("Quick shaking of single blades")]
        [SerializeField, PropertyRange(0, 1)] private float windFlutter = 0.3f;

        [Title("Grass")]
        [PropertyTooltip("Several layers may grow on one surface type: their plants mix, each layer at its own density")]
        [SerializeField] private List<GrassLayer> layers = new ();

        public float Range => range;
        public float Fade => fade;
        public float FullDensityDistance => fullDensityDistance;
        public float MaxWidening => maxWidening;
        public float DetailedMeshDistance => detailedMeshDistance;
        public int DetailedSegments => detailedSegments;
        public float TileSize => tileSize;
        public int MaxDetailedBlades => maxDetailedBlades;
        public int MaxBlades => maxBlades;
        public bool TerrainOcclusion => terrainOcclusion;
        public int OcclusionSteps => occlusionSteps;
        public float RootDarkening => rootDarkening;
        public float Smoothness => smoothness;
        public float TerrainNormal => terrainNormal;
        public bool CastShadows => castShadows;
        public float WindDirection => windDirection;
        public float WindStrength => windStrength;
        public float WindWaveLength => windWaveLength;
        public float WindSpeed => windSpeed;
        public float WindGustiness => windGustiness;
        public float WindFlutter => windFlutter;
        public IReadOnlyList<GrassLayer> Layers => layers;

        /// <summary>Raised when the settings are edited in the inspector.</summary>
        public event Action Changed;

        private void OnValidate() => Changed?.Invoke();
    }

    /// <summary>Plants growing on one surface type, mixed with the other layers of the same surface.</summary>
    [Serializable]
    public class GrassLayer
    {
        [PropertyTooltip("Surface type the grass grows on. Where surfaces blend, the grass thins out with their weight")]
        [SerializeField] private SurfaceType surface;
        [PropertyTooltip("Shape of the plants. Empty: the built-in grass blade")]
        [SerializeField] private GrassShape shape;
        [PropertyTooltip("Blades per square meter at full density")]
        [SerializeField, MinValue(0)] private float density = 40;
        [PropertyTooltip("Height of the blades in meters")]
        [SerializeField, MinMaxSlider(0.02f, 3, true)] private Vector2 height = new (0.25f, 0.6f);
        [PropertyTooltip("Width of the blades at the root, meters. Shapes are scaled by it across and forward")]
        [SerializeField, PropertyRange(0.005f, 0.3f)] private float width = 0.04f;
        [PropertyTooltip("How far the tips lean over, in blade heights")]
        [SerializeField, PropertyRange(0, 1)] private float bend = 0.3f;
        [SerializeField] private Color rootColor = new (0.16f, 0.24f, 0.06f);
        [SerializeField] private Color tipColor = new (0.45f, 0.55f, 0.18f);
        [PropertyTooltip("Random brightness and hue change from blade to blade")]
        [SerializeField, PropertyRange(0, 1)] private float colorVariation = 0.15f;
        [PropertyTooltip("How much the terrain past the grass range takes the grass color")]
        [SerializeField, PropertyRange(0, 1)] private float farTint = 0.7f;
        [PropertyTooltip("Colors of the parts of the shape: element i is submesh i (material slot i of the model), up to " +
                         "4. Parts without an element, or not overridden, take the root and tip colors")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        private List<GrassPartColor> parts = new ();

        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Size of the patches the plants grow in, meters. 0: no patches, the density is even")]
        [SerializeField, MinValue(0)] private float noiseScale;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Share of the ground covered by the patches")]
        [SerializeField, PropertyRange(0, 1)] private float noiseCoverage = 0.5f;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Width of the edges of the patches, over which the plants thin out. 0: sharp edges")]
        [SerializeField, PropertyRange(0, 1)] private float noiseSoftness = 0.3f;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Layers with the same seed and scale share their patches")]
        [SerializeField] private int noiseSeed;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Grows outside the patches instead: with the same seed and scale as another layer, it fills " +
                         "the ground the other one leaves")]
        [SerializeField] private bool noiseInvert;

        public SurfaceType Surface => surface;
        public GrassShape Shape => shape;
        public float Density => density;
        public Vector2 Height => height;
        public float Width => width;
        public float Bend => bend;
        public Color RootColor => rootColor;
        public Color TipColor => tipColor;
        public float ColorVariation => colorVariation;
        public float FarTint => farTint;
        public IReadOnlyList<GrassPartColor> Parts => parts;
        public float NoiseScale => noiseScale;
        public float NoiseCoverage => noiseCoverage;
        public float NoiseSoftness => noiseSoftness;
        public int NoiseSeed => noiseSeed;
        public bool NoiseInvert => noiseInvert;
    }

    /// <summary>Colors of one part (submesh) of the shape of a grass layer: each plant takes one of them at random.</summary>
    [Serializable]
    public class GrassPartColor
    {
        /// <summary>Parts of a shape that can have their own color.</summary>
        public const int MaxParts = 4;

        /// <summary>Colors of one part, at most.</summary>
        public const int MaxColors = 4095;

        [PropertyTooltip("Own colors instead of the root and tip colors of the layer. The color variation of the layer " +
                         "and the vertex colors of the shape still apply")]
        [SerializeField] private bool overrideColor;
        [PropertyTooltip("Each plant takes one of them at random; repeat a color to make it more common")]
        [SerializeField, ShowIf(nameof(overrideColor))] private List<Color> colors = new () { Color.white };

        public bool OverrideColor => overrideColor;
        public IReadOnlyList<Color> Colors => colors;
    }
}
