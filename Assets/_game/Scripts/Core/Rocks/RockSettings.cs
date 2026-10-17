using System;
using System.Collections.Generic;
using Core.PlacedPrefabs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Rocks
{
    /// <summary>
    /// Large rocks of the terrain: which surface types and slopes have them, which prefabs, and how far around the
    /// camera they exist. Prefabs carry their own renderers, LODs and colliders.
    /// </summary>
    [CreateAssetMenu(menuName = "Terrain/Rock Settings")]
    public class RockSettings : PlacedPrefabSettings
    {
        [Title("Rocks")]
        [PropertyTooltip("Several layers may lie on one surface type: their rocks mix, each layer at its own density")]
        [SerializeField] private List<RockLayer> layers = new ();

        public override IReadOnlyList<IPlacedPrefabLayer> Layers => layers;
    }

    /// <summary>Rocks lying on one surface type, mixed with the other layers of the same surface.</summary>
    [Serializable]
    public class RockLayer : PlacedPrefabLayer
    {
    }
}
