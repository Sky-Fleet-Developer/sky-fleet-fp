using UnityEngine;

namespace Core.Ai
{
    public interface ITargetData
    {
        public Vector3 Position {get;}
        public Quaternion Rotation {get;}
        public Vector3 Velocity {get;}
    }

    public class ManualTargetData : ITargetData
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Velocity { get; set; }
    }
}