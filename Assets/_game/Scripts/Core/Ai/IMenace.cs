using Core.World;
using UnityEngine;

namespace Core.Ai
{
    /// <summary>
    /// Implement this interface to be able to participate in the menace system.
    /// </summary>
    public interface IMenace
    {
        public UnitEntity MyUnit { get; }
        public float MenaceDistanceSqr { get; }
        public Ray AimingRay { get; }
        public float MenaceFactorValue { get; }
    }
}