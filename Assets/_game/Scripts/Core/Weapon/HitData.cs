using UnityEngine;

namespace Core.Weapon
{
    public struct HitData
    {
        public Vector3 HitPoint;
        public Vector3 HitNormal;

        public HitData(Vector3 raycastHitPoint, Vector3 raycastHitNormal)
        {
            HitPoint = raycastHitPoint;
            HitNormal = raycastHitNormal;
        }
    }
}