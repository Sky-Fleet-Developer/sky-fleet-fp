using UnityEngine;

namespace Core.World
{
    public interface IDynamicPositionProvider
    {
        Vector3 WorldPosition { get; }
        Vector3 SpacePosition { get; }
        Vector3 StoredVelocity { get; }
        Vector3 GetPredictedWorldPosition(float time);
    }
}