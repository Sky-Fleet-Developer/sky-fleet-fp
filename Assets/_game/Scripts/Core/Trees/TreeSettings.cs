using System;
using System.Collections.Generic;
using Core.PlacedPrefabs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Trees
{
    /// <summary>
    /// Trees of the terrain: which surface types and slopes have them, which prefabs, and how far around the camera
    /// they exist. Prefabs carry their own renderers, LODs and colliders (a trunk collider is enough).
    /// </summary>
    [CreateAssetMenu(menuName = "Terrain/Tree Settings")]
    public class TreeSettings : PlacedPrefabSettings
    {
        [Title("Trees")]
        [PropertyTooltip("Several layers may lie on one surface type: their trees mix, each layer at its own density")]
        [SerializeField] private List<TreeLayer> layers = new ();

        [Title("Levels of detail")]
        [PropertyTooltip("Meters from the camera where a tree changes from the full model (LOD0) to LOD1")]
        [SerializeField, MinValue(1)] private float lod1Distance = 40;
        [PropertyTooltip("Meters from the camera where a tree changes from LOD1 to LOD2")]
        [SerializeField, MinValue(1)] private float lod2Distance = 120;
        [PropertyTooltip("Meters from the camera where the model of a tree is culled; farther trees are drawn as imposters. " +
                         "At most the Range")]
        [SerializeField, MinValue(1)] private float cullDistance = 300;

        [Title("Far ring")]
        [PropertyTooltip("Meters from the camera where the trees drawn as impostors end. Keep it within the loaded terrain")]
        [SerializeField, MinValue(1)] private float farRange = 1500;
        [PropertyTooltip("Meters before the cull distance where impostors start to fade in over the models")]
        [SerializeField, MinValue(0)] private float farFadeWidth = 30;
        [PropertyTooltip("Tiles of the far ring placed per frame")]
        [SerializeField, PropertyRange(1, 256)] private int farTilesPerFrame = 48;
        [PropertyTooltip("Most impostors of one tree prefab drawn at a time; more are dropped")]
        [SerializeField, PropertyRange(256, 65536)] private int farMaxVisible = 16384;

        public override IReadOnlyList<IPlacedPrefabLayer> Layers => layers;

        public float Lod1Distance => lod1Distance;
        public float Lod2Distance => Mathf.Max(lod2Distance, lod1Distance);
        public float CullDistance => Mathf.Max(cullDistance, Lod2Distance);
        public float FarRange => Mathf.Max(farRange, CullDistance);
        public float FarFadeWidth => farFadeWidth;
        public int FarTilesPerFrame => farTilesPerFrame;
        public int FarMaxVisible => farMaxVisible;
    }

    /// <summary>Trees growing on one surface type, mixed with the other layers of the same surface.</summary>
    [Serializable]
    public class TreeLayer : PlacedPrefabLayer
    {
        public TreeLayer()
        {
            // Upright trees on gentle slopes, free to turn around
            SetDefaults(0.02f, new Vector2(0, 35), new Vector2(0.8f, 1.3f), new Vector2(0, 0.1f), new Vector3(3, 180, 3),
                new Vector2(0, 0.2f), 0.6f);
        }
    }
}
