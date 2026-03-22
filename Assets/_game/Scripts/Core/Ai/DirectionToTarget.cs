using UnityEngine;

namespace Core.Ai
{
    public class DirectionToTarget : IDirectionData
    {
        public ITargetData Target { get; set; }
        public DirectionToTarget(ITargetData target) => Target = target;

        public Vector3 GetDirection(Vector3 origin)
        {
            return Target.Position - origin;
        }

        public Vector3 GetPredictedDirection(Vector3 origin, Vector3 velocity, float time)
        {
            return Target.Position + Target.Velocity * time - (origin + velocity * time);
        }
    }
}