using UnityEngine;

namespace Core.Ai
{
    public class DirectionToTarget : IDirectionData
    {
        public ITargetData Target { get; set; }
        public DirectionToTarget(ITargetData target) => Target = target;
        public Vector3 GetDirection(Vector3 origin) => Target.Position - origin;
    }
}