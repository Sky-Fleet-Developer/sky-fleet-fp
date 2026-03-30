using UnityEngine;

namespace Core.Ai
{
    public class DirectionToTarget : IDirectionData
    {
        public ITargetData Target { get; set; }
        public Quaternion Correction { get; set; }
        public DirectionToTarget(ITargetData target) => Target = target;

        public Vector3 GetDirection(Vector3 origin)
        {
            return Correction * (Target.Position - origin);
        }

        public Vector3 GetPredictedDirection(Vector3 origin, Vector3 velocity, float time)
        {
            return Correction * (Target.Position + Target.Velocity * time - (origin + velocity * time));
        }
    }
}