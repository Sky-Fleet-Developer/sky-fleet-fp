using UnityEngine;

namespace Core.TerrainGenerator.LOD
{
    /// <summary>
    /// Пример конкретного модификатора: форсирует минимальную глубину для области ячеек.
    /// Удобно для "зон интереса" — вокруг важных объектов, построек и т.п.
    /// </summary>
    public readonly struct ForceMinDepthModifier : IRegionLodModifier
    {
        public Vector2Int CellMin { get; }
        public Vector2Int CellMax { get; }
        private readonly int _minDepth;

        public ForceMinDepthModifier(Vector2Int cellMin, Vector2Int cellMax, int minDepth)
        {
            CellMin = cellMin;
            CellMax = cellMax;
            _minDepth = minDepth;
        }

        public LodModifierResult Apply(Vector2Int nodeCellMin, Vector2Int nodeCellMax, float rawDistance, int depth)
        {
            return new LodModifierResult { AdjustedDistance = rawDistance, MinDepth = _minDepth };
        }
    }
}