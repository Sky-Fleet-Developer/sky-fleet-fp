using UnityEngine;

namespace Core.Misc
{
    public static class BoundsIntExtension
    {
        public static bool ContainsInclusive(this BoundsInt bounds, Vector3Int pos)
        {
            return pos.x <= bounds.xMax && pos.x >= bounds.xMin && pos.y <= bounds.yMax && pos.y >= bounds.yMin && pos.z <= bounds.zMax && pos.z >= bounds.zMin;
        }
    }
}