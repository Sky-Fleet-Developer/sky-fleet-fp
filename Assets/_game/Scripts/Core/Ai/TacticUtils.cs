using Core.Ai;
using UnityEngine;

namespace Core.Ai
{
    public static class ManeuverUtils
    {
        public static void SetFollowSpeed(this IUnitControl control, ITargetData target, Sensor sensor, float predictionTime, Vector3 followOffset, float chaseFactor, float minSpeed)
        {
            Vector3 v = target.Velocity;
            float vMag = Mathf.Max(v.magnitude, 0.001f);
            Vector3 targetPredicted = (target.Position + followOffset + v * predictionTime);
            Vector3 selfPredicted = sensor.Position + sensor.Velocity * predictionTime;
            Vector3 predictedDelta = targetPredicted - selfPredicted;
            Vector3 fwd = sensor.Rotation * Vector3.forward;
            float acceleration = Vector3.Dot(fwd, predictedDelta) * chaseFactor;
            
            control.SetSpeed(Mathf.Max(vMag + acceleration, minSpeed));
        }
    }
    
    public static class TacticUtils
    {
        public static float Dot(this Sensor from, ITargetData to)
        {
            Vector3 dir = to.Position - from.Position;
            return Vector3.Dot(from.Rotation * Vector3.forward, dir);
        }

        public static float Distance(this Sensor from, ITargetData to)
        {
            return Vector3.Distance(from.Position, to.Position);
        }
    }
}