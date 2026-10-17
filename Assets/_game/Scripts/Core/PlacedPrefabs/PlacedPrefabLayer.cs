using System;
using System.Collections.Generic;
using Core.TerrainGenerator.Surfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.PlacedPrefabs
{
    /// <summary>
    /// Instances of prefabs lying on one surface type, mixed with the other layers of the same surface. A kind derives
    /// from it to get its own list in its settings and, if needed, its own defaults.
    /// </summary>
    [Serializable]
    public abstract class PlacedPrefabLayer : IPlacedPrefabLayer
    {
        [PropertyTooltip("Surface type the instances lie on. Where surfaces blend, they thin out with its weight")]
        [SerializeField] private SurfaceType surface;
        [PropertyTooltip("Prefabs of the instances; each takes one of them at random. Repeat a prefab to make it more " +
                         "common. The pivot is where the instance meets the ground, y up")]
        [SerializeField, AssetsOnly] private List<GameObject> prefabs = new ();
        [PropertyTooltip("Instances per square meter at full density: 0.01 is one per 100 square meters")]
        [SerializeField, MinValue(0)] private float density = 0.005f;
        [PropertyTooltip("Weight of the surface type in the alphamap below which the instances don't appear at all. " +
                         "Above it they still thin out with the weight")]
        [SerializeField, PropertyRange(0, 1)] private float minSurfaceWeight;
        [PropertyTooltip("Steepness of the terrain where instances lie, degrees from flat, min and max")]
        [SerializeField, MinMaxSlider(0, 90, true)] private Vector2 slope = new (0, 90);
        [PropertyTooltip("Scale of the prefab, min and max")]
        [SerializeField, MinMaxSlider(0.1f, 5, true)] private Vector2 scale = new (0.8f, 1.3f);
        [PropertyTooltip("How much the instances follow the slope: 0 upright, 1 along the terrain normal. Each takes a " +
                         "value in this range")]
        [SerializeField, MinMaxSlider(0, 1, true)] private Vector2 slopeAlignment = new (0.3f, 0.8f);
        [PropertyTooltip("Largest random rotation around each axis, degrees: y turns the instance around its up " +
                         "direction, x and z tilt it")]
        [SerializeField] private Vector3 maxRotation = new (10, 180, 10);
        [PropertyTooltip("How deep the instances sink into the ground, meters at scale 1, min and max")]
        [SerializeField, MinMaxSlider(0, 5, true)] private Vector2 sink = new (0.1f, 0.5f);
        [PropertyTooltip("Ground around an instance kept clear of stones and grass, in half widths of the instance " +
                         "(measured from the meshes of its prefab): 1 clears a disc as wide as the instance. 0: none. " +
                         "Works within the cover range of the ScatterSystem")]
        [SerializeField, PropertyRange(0, 2)] private float groundCover = 0.9f;

        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Size of the patches the instances lie in, meters. 0: no patches, the density is even")]
        [SerializeField, MinValue(0)] private float noiseScale;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Share of the ground covered by the patches")]
        [SerializeField, PropertyRange(0, 1)] private float noiseCoverage = 0.5f;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Width of the edges of the patches, over which the instances thin out. 0: sharp edges")]
        [SerializeField, PropertyRange(0, 1)] private float noiseSoftness = 0.3f;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Layers with the same seed and scale share their patches, grass and stone layers too")]
        [SerializeField] private int noiseSeed;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Lies outside the patches instead")]
        [SerializeField] private bool noiseInvert;

        [FoldoutGroup("Age")]
        [PropertyTooltip("The weight of the surface type in the alphamap is the age of the grove: the denser the surface, " +
                         "the older. The layer lies only in a range of ages. Layers of one surface with ranges after " +
                         "each other make a gradient, for example from young to dead trees")]
        [SerializeField] private bool useAge;
        [FoldoutGroup("Age")]
        [PropertyTooltip("Surface weights, 0..1, in which the layer has its full density")]
        [SerializeField, ShowIf(nameof(useAge)), MinMaxSlider(0, 1, true)] private Vector2 ageRange = new (0, 1);
        [FoldoutGroup("Age")]
        [PropertyTooltip("Weights outside the range over which the density fades to nothing. Overlapping fades of two " +
                         "layers blend them")]
        [SerializeField, ShowIf(nameof(useAge)), PropertyRange(0, 0.5f)] private float ageFade = 0.1f;

        /// <summary>For the constructor of a kind: the values a new layer of it starts with.</summary>
        protected void SetDefaults(float newDensity, Vector2 newSlope, Vector2 newScale, Vector2 newSlopeAlignment,
            Vector3 newMaxRotation, Vector2 newSink, float newGroundCover)
        {
            density = newDensity;
            slope = newSlope;
            scale = newScale;
            slopeAlignment = newSlopeAlignment;
            maxRotation = newMaxRotation;
            sink = newSink;
            groundCover = newGroundCover;
        }

        public SurfaceType Surface => surface;
        public IReadOnlyList<GameObject> Prefabs => prefabs;
        public float Density => density;
        public float MinSurfaceWeight => minSurfaceWeight;
        public Vector2 Slope => slope;
        public Vector2 Scale => scale;
        public Vector2 SlopeAlignment => slopeAlignment;
        public Vector3 MaxRotation => maxRotation;
        public Vector2 Sink => sink;
        public float GroundCover => groundCover;
        public bool UseAge => useAge;
        public Vector2 AgeRange => ageRange;
        public float AgeFade => ageFade;
        public float NoiseScale => noiseScale;
        public float NoiseCoverage => noiseCoverage;
        public float NoiseSoftness => noiseSoftness;
        public int NoiseSeed => noiseSeed;
        public bool NoiseInvert => noiseInvert;
    }
}
