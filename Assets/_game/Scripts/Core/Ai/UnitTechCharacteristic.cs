using System;
using UnityEngine;

namespace Core.Ai
{
    [Serializable]
    public struct UnitTechCharacteristic
    {
        public float minimalForwardSpeed;
        public float cruiseSpeed;
        public float maxAttackRange;
        public float minAttackRange;
        public float turn180Time;
        public float cruiseLiftAngle;
        public Vector3 blindZone;
    }
}