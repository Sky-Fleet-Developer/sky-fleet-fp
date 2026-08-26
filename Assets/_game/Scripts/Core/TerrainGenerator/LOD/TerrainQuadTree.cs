using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.LOD
{
    /// <summary>
    /// Строит quadtree вокруг позиции камеры и отдаёт список листовых узлов.
    /// Каждый лист = один Patch: SizeInCells определяет, сколько исходных ячеек
    /// он покрывает, и с каким масштабом должна встать сетка Patch.
    /// </summary>
    public class TerrainQuadTree
    {
        private readonly QuadTreeLodConfig _lodConfig;
        private readonly List<IRegionLodModifier> _modifiers;

        public TerrainQuadTree(QuadTreeLodConfig lodConfig, List<IRegionLodModifier> modifiers = null)
        {
            _lodConfig = lodConfig;
            _modifiers = modifiers ?? new List<IRegionLodModifier>();
        }

        /// <summary>
        /// Строит дерево для одного корневого узла.
        /// rootOriginCell/rootSizeInCells — см. GetRelevantRoots ниже для бесконечного мира.
        /// </summary>
        public void Build(
            Vector2Int rootOriginCell,
            int rootSizeInCells,
            Vector2 cameraWorldXZ,
            float cellSizeMeters, List<QuadTreeNode> leaves)
        {
            leaves.Clear();
            var root = new QuadTreeNode
            {
                CellMin = rootOriginCell,
                CellMax = rootOriginCell + new Vector2Int(rootSizeInCells, rootSizeInCells),
                Depth = 0
            };

            SubdivideRecursive(root, cameraWorldXZ, cellSizeMeters, leaves);
        }

        private void SubdivideRecursive(QuadTreeNode node, Vector2 cameraXZ, float cellSize, List<QuadTreeNode> leaves)
        {
            float distance = DistanceToNode(node, cameraXZ, cellSize);
            int forcedMinDepth = 0;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                var mod = _modifiers[i];
                if (!Overlaps(node.CellMin, node.CellMax, mod.CellMin, mod.CellMax))
                    continue;

                var result = mod.Apply(node.CellMin, node.CellMax, distance, node.Depth);
                distance = Mathf.Min(distance, result.AdjustedDistance);
                forcedMinDepth = Mathf.Max(forcedMinDepth, result.MinDepth);
            }

            bool canSplit = node.SizeInCells > 1 && node.Depth < _lodConfig.MaxDepth;
            bool wantsSplitByDistance = node.Depth < _lodConfig.LodDistances.Length
                                        && distance < _lodConfig.LodDistances[node.Depth];
            bool wantsSplitForced = node.Depth < forcedMinDepth;

            if (canSplit && (wantsSplitByDistance || wantsSplitForced))
            {
                foreach (var child in GetChildren(node))
                    SubdivideRecursive(child, cameraXZ, cellSize, leaves);
            }
            else
            {
                node.IsLeaf = true;
                leaves.Add(node);
            }
        }

        private static float DistanceToNode(QuadTreeNode node, Vector2 cameraXZ, float cellSize)
        {
            Vector2 worldMin = new Vector2(node.CellMin.x, node.CellMin.y) * cellSize;
            Vector2 worldMax = new Vector2(node.CellMax.x, node.CellMax.y) * cellSize;
            Vector2 closest = Vector2.Max(worldMin, Vector2.Min(cameraXZ, worldMax));
            return Vector2.Distance(cameraXZ, closest);
        }

        private static IEnumerable<QuadTreeNode> GetChildren(QuadTreeNode node)
        {
            var half = (node.CellMax - node.CellMin) / 2;
            Vector2Int[] mins =
            {
                node.CellMin,
                node.CellMin + new Vector2Int(half.x, 0),
                node.CellMin + new Vector2Int(0, half.y),
                node.CellMin + half
            };

            foreach (var min in mins)
            {
                yield return new QuadTreeNode
                {
                    CellMin = min,
                    CellMax = min + half,
                    Depth = node.Depth + 1
                };
            }
        }

        private static bool Overlaps(Vector2Int aMin, Vector2Int aMax, Vector2Int bMin, Vector2Int bMax)
        {
            return aMin.x < bMax.x && aMax.x > bMin.x &&
                   aMin.y < bMax.y && aMax.y > bMin.y;
        }

        /// <summary>
        /// Для бесконечного мира единого корня не хватит — верхний уровень тайлится
        /// сеткой корней размера rootSizeInCells. Возвращает координаты origin-ячеек
        /// тех корней, что вообще могут попасть в радиус действия LOD (LodDistances[0]).
        /// Вызывайте раз в N кадров / при пересечении границы корневой ячейки камерой.
        /// </summary>
        public IEnumerable<Vector2Int> GetRelevantRoots(Vector2 cameraWorldXZ, float cellSizeMeters,
            int rootSizeInCells)
        {
            float maxDistance = _lodConfig.LodDistances.Length > 0 ? _lodConfig.LodDistances[0] : 0f;
            float rootSizeMeters = rootSizeInCells * cellSizeMeters;

            int radiusInRoots = Mathf.CeilToInt(maxDistance / rootSizeMeters) + 1;

            Vector2Int cameraRoot = new Vector2Int(
                Mathf.FloorToInt(cameraWorldXZ.x / rootSizeMeters),
                Mathf.FloorToInt(cameraWorldXZ.y / rootSizeMeters));

            for (int dz = -radiusInRoots; dz <= radiusInRoots; dz++)
            for (int dx = -radiusInRoots; dx <= radiusInRoots; dx++)
            {
                var rootIndex = cameraRoot + new Vector2Int(dx, dz);
                yield return rootIndex * rootSizeInCells;
            }
        }
    }
}