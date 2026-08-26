using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Utility
{
    public static class RectIntExtensions
    {
        public static IEnumerable<Vector2Int> EnumerateYFirst(this RectInt rect)
        {
            for (int x = rect.xMin; x < rect.xMax; x++) for (int y = rect.yMin; y < rect.yMax; y++)
            {
                yield return new Vector2Int(x, y);
            }
        }
    }
}