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

        public override IReadOnlyList<IPlacedPrefabLayer> Layers => layers;
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
