using UnityEngine;

namespace Core.TerrainGenerator.LOD
{
    /// <summary>
    /// Абстрактный модификатор LOD для прямоугольной области, заданной в ячейках [Min, Max).
    /// Позволяет либо "приблизить" узел (уменьшить эффективную дистанцию до камеры,
    /// тем самым заставив его дробиться сильнее), либо жёстко задать минимальную глубину.
    /// Конкретную бизнес-логику (например, "зона строительства всегда максимальный LOD"
    /// или "плейсхолдер зоны интереса дизайнера") реализуйте через этот интерфейс.
    /// </summary>
    public interface IRegionLodModifier
    {
        Vector2Int CellMin { get; }
        Vector2Int CellMax { get; }

        /// <param name="nodeCellMin">границы проверяемого узла дерева</param>
        /// <param name="nodeCellMax"></param>
        /// <param name="rawDistance">дистанция от камеры до узла, посчитанная деревом</param>
        /// <param name="depth">текущая глубина узла</param>
        LodModifierResult Apply(Vector2Int nodeCellMin, Vector2Int nodeCellMax, float rawDistance, int depth);
    }
}