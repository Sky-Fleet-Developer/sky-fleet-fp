using UnityEngine;

namespace Core.World
{
    public interface IWorldEntity
    {
        Vector3 Position { get; }
        void OnDistanceToPlayerChanged(int cellsDistance, float realDistanceSqr);
    }
}