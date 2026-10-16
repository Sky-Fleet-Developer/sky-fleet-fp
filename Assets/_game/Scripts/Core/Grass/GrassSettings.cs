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
        [Tooltip("Meters from the camera where the blades end. Past it the terrain takes the grass color")]
        [SerializeField, Min(10)] private float range = 500;
        [Tooltip("Part of the range over which the blades fade out and the terrain color fades in")]
        [SerializeField, Range(0.02f, 0.5f)] private float fade = 0.15f;
        [Tooltip("Meters within which the grass has its full density. Farther the density falls with the square of " +
                 "the distance and the blades get wider to keep covering the ground")]
        [SerializeField, Min(1)] private float fullDensityDistance = 15;
        [Tooltip("Largest widening of far blades")]
        [SerializeField, Range(1, 8)] private float maxWidening = 4;
        [Tooltip("Blades closer than this many meters use the detailed, curved mesh; farther ones a single triangle")]
        [SerializeField, Min(0)] private float detailedMeshDistance = 25;
        [Tooltip("Segments of the detailed blade mesh")]
        [SerializeField, Range(2, 8)] private int detailedSegments = 4;
        [Tooltip("Size in meters of the tiles the placement is culled by")]
        [SerializeField, Range(2, 32)] private float tileSize = 8;
        [Tooltip("Largest number of detailed blades. Blades past it are not drawn. The blade mesh holds a blade for " +
                 "every one of them")]
        [SerializeField, Min(1024)] private int maxDetailedBlades = 100_000;
        [Tooltip("Largest number of simple blades. Blades past it are not drawn. The blade mesh holds a blade for " +
                 "every one of them")]
        [SerializeField, Min(1024)] private int maxBlades = 300_000;

        [Title("Culling")]
        [Tooltip("Tiles hidden behind hills are skipped: the terrain between the camera and the tile is checked")]
        [SerializeField] private bool terrainOcclusion = true;
        [Tooltip("Heightmap samples along each line of sight")]
        [SerializeField, Range(4, 64)] private int occlusionSteps = 16;

        [Title("Look")]
        [Tooltip("Darkening at the roots, a stand-in for the shadows of the blades on each other")]
        [SerializeField, Range(0, 1)] private float rootDarkening = 0.5f;
        [SerializeField, Range(0, 1)] private float smoothness = 0.2f;
        [Tooltip("How much the blades are lit like the ground under them rather than by their own facing")]
        [SerializeField, Range(0, 1)] private float terrainNormal = 0.7f;

        [Title("Wind")]
        [Tooltip("Direction the wind blows to, degrees clockwise from +z")]
        [SerializeField, Range(0, 360)] private float windDirection = 45;
        [Tooltip("Lean of the blades in the wind, in blade heights")]
        [SerializeField, Range(0, 2)] private float windStrength = 0.3f;
        [Tooltip("Meters between the waves running over the grass")]
        [SerializeField, Min(0.1f)] private float windWaveLength = 20;
        [Tooltip("Speed of the waves, meters per second")]
        [SerializeField, Min(0)] private float windSpeed = 6;
        [Tooltip("How much the strength changes from wave to wave")]
        [SerializeField, Range(0, 1)] private float windGustiness = 0.6f;
        [Tooltip("Quick shaking of single blades")]
        [SerializeField, Range(0, 1)] private float windFlutter = 0.3f;

        [Title("Grass")]
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

    /// <summary>Grass growing on one surface type.</summary>
    [Serializable]
    public class GrassLayer
    {
        [Tooltip("Surface type the grass grows on. Where surfaces blend, the grass thins out with their weight")]
        [SerializeField] private SurfaceType surface;
        [Tooltip("Blades per square meter at full density")]
        [SerializeField, Min(0)] private float density = 40;
        [Tooltip("Height of the blades in meters")]
        [SerializeField, MinMaxSlider(0.02f, 3, true)] private Vector2 height = new (0.25f, 0.6f);
        [Tooltip("Width of the blades at the root, meters")]
        [SerializeField, Range(0.005f, 0.3f)] private float width = 0.04f;
        [Tooltip("How far the tips lean over, in blade heights")]
        [SerializeField, Range(0, 1)] private float bend = 0.3f;
        [SerializeField, ColorUsage(false)] private Color rootColor = new (0.16f, 0.24f, 0.06f);
        [SerializeField, ColorUsage(false)] private Color tipColor = new (0.45f, 0.55f, 0.18f);
        [Tooltip("Random brightness and hue change from blade to blade")]
        [SerializeField, Range(0, 1)] private float colorVariation = 0.15f;
        [Tooltip("How much the terrain past the grass range takes the grass color")]
        [SerializeField, Range(0, 1)] private float farTint = 0.7f;

        public SurfaceType Surface => surface;
        public float Density => density;
        public Vector2 Height => height;
        public float Width => width;
        public float Bend => bend;
        public Color RootColor => rootColor;
        public Color TipColor => tipColor;
        public float ColorVariation => colorVariation;
        public float FarTint => farTint;
    }
}
