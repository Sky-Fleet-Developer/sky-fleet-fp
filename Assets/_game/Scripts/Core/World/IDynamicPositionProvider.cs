using UnityEngine;

namespace Core.World
{
    public interface IDynamicPositionProvider
    {
        /// <summary>
        /// Transform.position - WorldOffset.Offset
        /// </summary>
        Vector3 WorldPosition { get; }
        /// <summary>
        /// Transform.position
        /// </summary>
        Vector3 SpacePosition { get; }
        Vector3 StoredVelocity { get; }
        Vector3 GetPredictedWorldPosition(float time);
    }
}