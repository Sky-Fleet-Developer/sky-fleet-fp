using System.Collections.Generic;
using Core.Scatter;
using UnityEngine;

namespace Core.PlacedPrefabs
{
    /// <summary>A scatter layer whose details are prefab instances: how they are picked, sized, turned and sunk.</summary>
    public interface IPlacedPrefabLayer : IScatterLayer
    {
        IReadOnlyList<GameObject> Prefabs { get; }

        /// <summary>Steepness of the terrain where instances lie, degrees from flat, min and max.</summary>
        Vector2 Slope { get; }

        /// <summary>Scale of the prefab, min and max.</summary>
        Vector2 Scale { get; }

        /// <summary>0 upright .. 1 along the terrain normal, min and max.</summary>
        Vector2 SlopeAlignment { get; }

        /// <summary>Largest random rotation around each axis, degrees.</summary>
        Vector3 MaxRotation { get; }

        /// <summary>How deep instances sink into the ground, meters at scale 1, min and max.</summary>
        Vector2 Sink { get; }

        /// <summary>The layer lies only in a range of ages: weights of its surface type (<see cref="AgeRange"/>).</summary>
        bool UseAge { get; }

        /// <summary>Surface weights, 0..1, in which the layer has its full density.</summary>
        Vector2 AgeRange { get; }

        /// <summary>Weights outside the range over which the density fades to nothing.</summary>
        float AgeFade { get; }

        /// <summary>Ground kept clear of stones and grass, in half widths of the instance. 0: none.</summary>
        float GroundCover { get; }
    }
}
