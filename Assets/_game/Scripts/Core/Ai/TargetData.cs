using UnityEngine;

namespace Core.Ai
{
    public interface ITargetData
    {
        public Vector3 Position {get;}
        public Quaternion Rotation {get;}
        public Vector3 Velocity {get;}
    }
}