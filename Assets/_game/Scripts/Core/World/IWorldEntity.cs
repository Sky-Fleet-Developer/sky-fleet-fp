using UnityEngine;

namespace Core.World
{
    public interface IWorldEntity
    {
        Vector3 Position { get; }
        void OnLodChanged(int lod);
    }
}