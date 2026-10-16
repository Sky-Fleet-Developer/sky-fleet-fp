using UnityEngine;

namespace Core.TerrainGenerator
{
    public interface IPatch
    {
        float ChunkSize { get; }
        Material Material { get; }
        RectInt Rect { get; }
        bool Exist => this as UnityEngine.Object != null;
        void Enable();
        void Disable();
        void RefreshPosition();
        Transform transform { get;}
        void SetRect(RectInt rect);
        /// <summary>Vertical extent of the culling bounds, in meters relative to the terrain.</summary>
        void SetHeightRange(float min, float max);
    }
}