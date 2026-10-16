using System;
using System.Collections.Generic;
using Core.Scatter;
using Core.TerrainGenerator.Surfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Stones
{
    /// <summary>
    /// Small stones of the terrain: which surface types have them and how they look, and how far and how densely they
    /// are drawn.
    /// </summary>
    [CreateAssetMenu(menuName = "Terrain/Stone Settings")]
    public class StoneSettings : ScriptableObject
    {
        [Title("Range")]
        [PropertyTooltip("Meters from the camera where the stones end")]
        [SerializeField, MinValue(10)] private float range = 150;
        [PropertyTooltip("Part of the range over which the stones shrink away")]
        [SerializeField, PropertyRange(0.02f, 0.5f)] private float fade = 0.15f;
        [PropertyTooltip("Meters within which the stones have their full density. Farther the density falls with the " +
                         "square of the distance")]
        [SerializeField, MinValue(1)] private float fullDensityDistance = 30;
        [PropertyTooltip("Stones closer than this many meters use the near shape of their StoneShape")]
        [SerializeField, MinValue(0)] private float detailedMeshDistance = 40;
        [PropertyTooltip("Size in meters of the tiles the placement is culled by")]
        [SerializeField, PropertyRange(2, 32)] private float tileSize = 8;

        [Title("Culling")]
        [PropertyTooltip("Tiles hidden behind hills are skipped: the terrain between the camera and the tile is checked")]
        [SerializeField] private bool terrainOcclusion = true;
        [PropertyTooltip("Heightmap samples along each line of sight")]
        [SerializeField, PropertyRange(4, 64)] private int occlusionSteps = 16;

        [Title("Shadows")]
        [PropertyTooltip("Stones closer than Detailed Mesh Distance cast shadows")]
        [SerializeField] private bool castShadows = true;
        [PropertyTooltip("Farther stones cast shadows too. Only those in view: stones outside it are not placed")]
        [SerializeField] private bool farShadows;

        [Title("Look")]
        [PropertyTooltip("Tiling textures of the stones")]
        [SerializeField] private StoneTextureSet textures;
        [PropertyTooltip("Texture pixels per meter on every stone, whatever its size and shape")]
        [SerializeField, MinValue(1)] private float texelDensity = 512;
        [PropertyTooltip("Each repetition of the textures is offset and rotated at random, so the tiling doesn't show")]
        [SerializeField] private bool stochasticTiling = true;
        [PropertyTooltip("How much the heights (mask blue) decide the blend between the random repetitions")]
        [SerializeField, PropertyRange(0, 1), ShowIf(nameof(stochasticTiling))] private float stochasticContrast = 0.6f;
        [PropertyTooltip("Largest random rotation of a repetition, in turns")]
        [SerializeField, PropertyRange(0, 1), ShowIf(nameof(stochasticTiling))] private float stochasticRotation = 1;
        [PropertyTooltip("Multiplies the smoothness of the mask (alpha)")]
        [SerializeField, PropertyRange(0, 2)] private float smoothness = 1;
        [PropertyTooltip("Strength of the normal maps")]
        [SerializeField, PropertyRange(0, 2)] private float normalStrength = 1;

        [Title("Occlusion")]
        [PropertyTooltip("Ambient occlusion per vertex: baked in the vertex colors of the shapes, and darker towards the " +
                         "ground. Costs next to nothing")]
        [SerializeField] private bool vertexOcclusion;
        [PropertyTooltip("How much the vertex colors of the shapes (their grayscale) darken them. Shapes without vertex " +
                         "colors are white: no change")]
        [SerializeField, PropertyRange(0, 1), ShowIf(nameof(vertexOcclusion))] private float vertexColorOcclusion = 1;
        [PropertyTooltip("Darkening at the bottom of the stones, where they meet the ground")]
        [SerializeField, PropertyRange(0, 1), ShowIf(nameof(vertexOcclusion))] private float groundOcclusion = 0.6f;
        [PropertyTooltip("Share of the stone height, from its pivot up, over which the bottom darkening fades out")]
        [SerializeField, PropertyRange(0.01f, 1), ShowIf(nameof(vertexOcclusion))] private float groundOcclusionHeight = 0.35f;
        [PropertyTooltip("How much the occlusion darkens the color too. The occlusion alone only darkens the indirect " +
                         "light, so it hardly shows in sunlight")]
        [SerializeField, PropertyRange(0, 1), ShowIf(nameof(vertexOcclusion))] private float occlusionOnColor = 0.5f;

        [Title("Stones")]
        [PropertyTooltip("Several layers may lie on one surface type: their stones mix, each layer at its own density")]
        [SerializeField] private List<StoneLayer> layers = new ();

        public float Range => range;
        public float Fade => fade;
        public float FullDensityDistance => fullDensityDistance;
        public float DetailedMeshDistance => detailedMeshDistance;
        public float TileSize => tileSize;
        public bool TerrainOcclusion => terrainOcclusion;
        public int OcclusionSteps => occlusionSteps;
        public bool CastShadows => castShadows;
        public bool FarShadows => farShadows;
        public StoneTextureSet Textures => textures;
        public float TexelDensity => texelDensity;
        public bool StochasticTiling => stochasticTiling;
        public float StochasticContrast => stochasticContrast;
        public float StochasticRotation => stochasticRotation;
        public float Smoothness => smoothness;
        public float NormalStrength => normalStrength;
        public bool VertexOcclusion => vertexOcclusion;
        public float VertexColorOcclusion => vertexColorOcclusion;
        public float GroundOcclusion => groundOcclusion;
        public float GroundOcclusionHeight => groundOcclusionHeight;
        public float OcclusionOnColor => occlusionOnColor;
        public IReadOnlyList<StoneLayer> Layers => layers;

        /// <summary>Raised when the settings are edited in the inspector.</summary>
        public event Action Changed;

        private void OnValidate() => Changed?.Invoke();
    }

    /// <summary>Stones lying on one surface type, mixed with the other layers of the same surface.</summary>
    [Serializable]
    public class StoneLayer : IScatterLayer
    {
        [PropertyTooltip("Surface type the stones lie on. Where surfaces blend, the stones thin out with their weight")]
        [SerializeField] private SurfaceType surface;
        [PropertyTooltip("Shapes of the stones; each stone takes one of them at random. Repeat a shape to make it more common")]
        [SerializeField] private List<StoneShape> shapes = new ();
        [PropertyTooltip("Stones per square meter at full density")]
        [SerializeField, MinValue(0)] private float density = 0.5f;
        [PropertyTooltip("Weight of the surface type in the alphamap below which the stones don't appear at all. Above " +
                         "it they still thin out with the weight")]
        [SerializeField, PropertyRange(0, 1)] private float minSurfaceWeight;
        [PropertyTooltip("Largest dimension of the stones, meters")]
        [SerializeField, MinMaxSlider(0.01f, 3, true)] private Vector2 size = new (0.05f, 0.3f);
        [PropertyTooltip("How much the stones follow the slope: 0 upright, 1 along the terrain normal. Each stone takes " +
                         "a value in this range")]
        [SerializeField, MinMaxSlider(0, 1, true)] private Vector2 slopeAlignment = new (0.6f, 1);
        [PropertyTooltip("Largest random rotation around each axis, degrees: y turns the stone around its up direction, " +
                         "x and z tilt it")]
        [SerializeField] private Vector3 maxRotation = new (15, 180, 15);
        [PropertyTooltip("How deep the stones sink into the ground, in stone sizes")]
        [SerializeField, MinMaxSlider(0, 1, true)] private Vector2 sink = new (0.1f, 0.3f);
        [PropertyTooltip("Ground around a stone kept clear of grass, in half stone sizes: 1 clears a disc as wide as the " +
                         "stone. 0: grass grows through. Works within the cover range of the ScatterSystem")]
        [SerializeField, PropertyRange(0, 2)] private float grassCover = 0.8f;
        [PropertyTooltip("Textures of StoneSettings.Textures the stones take one of at random, by index. Empty: all of them")]
        [SerializeField] private List<int> textures = new ();
        [PropertyTooltip("Multiplies the albedo")]
        [SerializeField] private Color tint = Color.white;
        [PropertyTooltip("Random brightness and hue change from stone to stone")]
        [SerializeField, PropertyRange(0, 1)] private float colorVariation = 0.15f;

        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Size of the patches the stones lie in, meters. 0: no patches, the density is even")]
        [SerializeField, MinValue(0)] private float noiseScale;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Share of the ground covered by the patches")]
        [SerializeField, PropertyRange(0, 1)] private float noiseCoverage = 0.5f;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Width of the edges of the patches, over which the stones thin out. 0: sharp edges")]
        [SerializeField, PropertyRange(0, 1)] private float noiseSoftness = 0.3f;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Layers with the same seed and scale share their patches, grass layers too")]
        [SerializeField] private int noiseSeed;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Lies outside the patches instead")]
        [SerializeField] private bool noiseInvert;

        public SurfaceType Surface => surface;
        public IReadOnlyList<StoneShape> Shapes => shapes;
        public float Density => density;
        public float MinSurfaceWeight => minSurfaceWeight;
        public Vector2 Size => size;
        public Vector2 SlopeAlignment => slopeAlignment;
        public Vector3 MaxRotation => maxRotation;
        public Vector2 Sink => sink;
        public float GrassCover => grassCover;
        public IReadOnlyList<int> Textures => textures;
        public Color Tint => tint;
        public float ColorVariation => colorVariation;
        public float NoiseScale => noiseScale;
        public float NoiseCoverage => noiseCoverage;
        public float NoiseSoftness => noiseSoftness;
        public int NoiseSeed => noiseSeed;
        public bool NoiseInvert => noiseInvert;
    }
}
