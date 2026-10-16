using System;
using System.Collections.Generic;
using Core.Scatter;
using Core.TerrainGenerator.Surfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Rocks
{
    /// <summary>
    /// Large rocks of the terrain: which surface types and slopes have them, which prefabs, and how far around the
    /// camera they exist. Prefabs carry their own renderers, LODs and colliders.
    /// </summary>
    [CreateAssetMenu(menuName = "Terrain/Rock Settings")]
    public class RockSettings : ScriptableObject
    {
        [Title("Range")]
        [PropertyTooltip("Meters from the camera within which tiles get their rocks. Keep it within the loaded terrain")]
        [SerializeField, MinValue(16)] private float range = 400;
        [PropertyTooltip("Tiles farther than Range times this lose their rocks; between the two they keep them, so tiles " +
                         "don't flicker at the edge")]
        [SerializeField, PropertyRange(1, 2)] private float keepRange = 1.2f;
        [PropertyTooltip("Size in meters of the tiles the rocks are placed by")]
        [SerializeField, PropertyRange(16, 256)] private float tileSize = 64;
        [PropertyTooltip("Most rocks of one tile; more are dropped")]
        [SerializeField, PropertyRange(8, 1024)] private int maxPerTile = 128;
        [PropertyTooltip("Tiles placed by one GPU request")]
        [SerializeField, PropertyRange(1, 64)] private int tilesPerRequest = 16;
        [PropertyTooltip("Rocks spawned or moved per frame at most, to spread the cost of a new area")]
        [SerializeField, MinValue(1)] private int spawnsPerFrame = 48;

        [Title("Rocks")]
        [PropertyTooltip("Several layers may lie on one surface type: their rocks mix, each layer at its own density")]
        [SerializeField] private List<RockLayer> layers = new ();

        public float Range => range;
        public float KeepRange => keepRange;
        public float TileSize => tileSize;
        public int MaxPerTile => maxPerTile;
        public int TilesPerRequest => tilesPerRequest;
        public int SpawnsPerFrame => spawnsPerFrame;
        public IReadOnlyList<RockLayer> Layers => layers;

        /// <summary>Raised when the settings are edited in the inspector.</summary>
        public event Action Changed;

        private void OnValidate() => Changed?.Invoke();
    }

    /// <summary>Rocks lying on one surface type, mixed with the other layers of the same surface.</summary>
    [Serializable]
    public class RockLayer : IScatterLayer
    {
        [PropertyTooltip("Surface type the rocks lie on. Where surfaces blend, the rocks thin out with their weight")]
        [SerializeField] private SurfaceType surface;
        [PropertyTooltip("Prefabs of the rocks; each rock takes one of them at random. Repeat a prefab to make it more " +
                         "common. The pivot is where the rock meets the ground, y up")]
        [SerializeField, AssetsOnly] private List<GameObject> prefabs = new ();
        [PropertyTooltip("Rocks per square meter at full density: 0.01 is one rock per 100 square meters")]
        [SerializeField, MinValue(0)] private float density = 0.005f;
        [PropertyTooltip("Weight of the surface type in the alphamap below which the rocks don't appear at all. Above " +
                         "it they still thin out with the weight")]
        [SerializeField, PropertyRange(0, 1)] private float minSurfaceWeight;
        [PropertyTooltip("Steepness of the terrain where rocks lie, degrees from flat, min and max")]
        [SerializeField, MinMaxSlider(0, 90, true)] private Vector2 slope = new (0, 90);
        [PropertyTooltip("Scale of the prefab, min and max")]
        [SerializeField, MinMaxSlider(0.1f, 5, true)] private Vector2 scale = new (0.8f, 1.3f);
        [PropertyTooltip("How much the rocks follow the slope: 0 upright, 1 along the terrain normal. Each rock takes a " +
                         "value in this range")]
        [SerializeField, MinMaxSlider(0, 1, true)] private Vector2 slopeAlignment = new (0.3f, 0.8f);
        [PropertyTooltip("Largest random rotation around each axis, degrees: y turns the rock around its up direction, " +
                         "x and z tilt it")]
        [SerializeField] private Vector3 maxRotation = new (10, 180, 10);
        [PropertyTooltip("How deep the rocks sink into the ground, meters at scale 1, min and max")]
        [SerializeField, MinMaxSlider(0, 5, true)] private Vector2 sink = new (0.1f, 0.5f);
        [PropertyTooltip("Ground around a rock kept clear of stones and grass, in half widths of the rock (measured from " +
                         "the meshes of its prefab): 1 clears a disc as wide as the rock. 0: none. Works within the " +
                         "cover range of the ScatterSystem")]
        [SerializeField, PropertyRange(0, 2)] private float groundCover = 0.9f;

        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Size of the patches the rocks lie in, meters. 0: no patches, the density is even")]
        [SerializeField, MinValue(0)] private float noiseScale;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Share of the ground covered by the patches")]
        [SerializeField, PropertyRange(0, 1)] private float noiseCoverage = 0.5f;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Width of the edges of the patches, over which the rocks thin out. 0: sharp edges")]
        [SerializeField, PropertyRange(0, 1)] private float noiseSoftness = 0.3f;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Layers with the same seed and scale share their patches, grass and stone layers too")]
        [SerializeField] private int noiseSeed;
        [FoldoutGroup("Density noise")]
        [PropertyTooltip("Lies outside the patches instead")]
        [SerializeField] private bool noiseInvert;

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
        public float NoiseScale => noiseScale;
        public float NoiseCoverage => noiseCoverage;
        public float NoiseSoftness => noiseSoftness;
        public int NoiseSeed => noiseSeed;
        public bool NoiseInvert => noiseInvert;
    }
}
