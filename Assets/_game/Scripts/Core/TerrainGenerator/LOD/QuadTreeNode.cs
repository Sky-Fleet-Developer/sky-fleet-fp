using UnityEngine;

namespace Core.TerrainGenerator.LOD
{
    /// <summary>
    /// Узел quadtree в координатах ячеек (не в мировых координатах!).
    /// Ячейка = world.xz / chunkSizeMeters, как у вас и описано.
    /// CellMin включительно, CellMax исключительно. Размер узла всегда степень двойки.
    /// </summary>
    public struct QuadTreeNode
    {
        public Vector2Int CellMin;
        public Vector2Int CellMax;
        public int Depth;
        public bool IsLeaf;

        public int SizeInCells => CellMax.x - CellMin.x;
        public Vector2Int Center => CellMin + (CellMax - CellMin) / 2;
    }
}