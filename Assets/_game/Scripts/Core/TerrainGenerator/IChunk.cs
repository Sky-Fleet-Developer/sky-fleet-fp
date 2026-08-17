using UnityEngine;

namespace Core.TerrainGenerator
{
    public interface IChunk
    {
        float ChunkSize { get; }
        Material Material { get; }
        bool IsChunkVisible { get; set; }
        Vector2Int Coord { get; }
        void SetHeights(RenderTexture dataTexture, ComputeBuffer getMapBuffer, Vector2Int mapMin, int mapSize);
        void Enable();
        void Disable();
        void RefreshPosition();
        void OnChunksRefreshed();
        Transform transform { get;}
    }
}