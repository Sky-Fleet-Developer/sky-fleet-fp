using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.PlacedPrefabs
{
    /// <summary>
    /// How far around the camera prefab instances exist and how they are placed in tiles. A kind derives from it and
    /// adds its layers (a list of its own layer type, so each has its own defaults and inspector).
    /// </summary>
    public abstract class PlacedPrefabSettings : ScriptableObject
    {
        [Title("Range")]
        [PropertyTooltip("Meters from the camera within which tiles get their instances. Keep it within the loaded terrain")]
        [SerializeField, MinValue(16)] private float range = 400;
        [PropertyTooltip("Tiles farther than Range times this lose their instances; between the two they keep them, so " +
                         "tiles don't flicker at the edge")]
        [SerializeField, PropertyRange(1, 2)] private float keepRange = 1.2f;
        [PropertyTooltip("Size in meters of the tiles the instances are placed by")]
        [SerializeField, PropertyRange(16, 256)] private float tileSize = 64;
        [PropertyTooltip("Most instances of one tile; more are dropped")]
        [SerializeField, PropertyRange(8, 1024)] private int maxPerTile = 128;
        [PropertyTooltip("Tiles placed by one GPU request")]
        [SerializeField, PropertyRange(1, 64)] private int tilesPerRequest = 16;
        [PropertyTooltip("Instances spawned or moved per frame at most, to spread the cost of a new area")]
        [SerializeField, MinValue(1)] private int spawnsPerFrame = 48;

        public float Range => range;
        public float KeepRange => keepRange;
        public float TileSize => tileSize;
        public int MaxPerTile => maxPerTile;
        public int TilesPerRequest => tilesPerRequest;
        public int SpawnsPerFrame => spawnsPerFrame;

        public abstract IReadOnlyList<IPlacedPrefabLayer> Layers { get; }

        /// <summary>Raised when the settings are edited in the inspector.</summary>
        public event Action Changed;

        private void OnValidate() => Changed?.Invoke();
    }
}
