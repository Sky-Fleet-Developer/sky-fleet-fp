using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Core.TerrainGenerator.LOD
{
    /// <summary>
    /// Настройки LOD: дистанции, при которых узел данной глубины ещё должен дробиться дальше.
    /// LodDistances[0] — порог для корня (глубина 0), и т.д.
    /// Чем меньше индекс — тем больше threshold (близкие к камере зоны дробятся сильнее всего).
    /// </summary>
    [Serializable]
    public class QuadTreeLodConfig
    {
        /// <summary>Size in cells of the quadtree roots, the largest patches.</summary>
        public const int RootSizeInCells = 16;

        public float[] LodDistances;
        [PropertyTooltip("Vertices of a patch blend to the heights of its parent while approaching the distance where they merge, " +
                 "so LOD switches don't pop. Costs extra heightmap samples in the vertex shader")]
        public bool Geomorphing;
        [PropertyTooltip("Part of the merge distance over which the vertices blend")]
        [PropertyRange(0.05f, 0.5f)] public float MorphRange = 0.3f;

        public int MaxDepth => LodDistances.Length;

        /// <summary>
        /// View distance where a patch of the given size merges into its parent: a node of depth d splits while closer
        /// than LodDistances[d]. Infinity for the roots and sizes the tree never builds.
        /// </summary>
        public float MergeDistance(int patchSizeInCells)
        {
            int depth = Mathf.RoundToInt(Mathf.Log((float)RootSizeInCells / patchSizeInCells, 2));
            return depth >= 1 && depth <= LodDistances.Length ? LodDistances[depth - 1] : float.PositiveInfinity;
        }
    }
}