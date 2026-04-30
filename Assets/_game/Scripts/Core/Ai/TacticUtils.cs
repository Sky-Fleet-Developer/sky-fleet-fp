using Core.Ai;
using Core.World;
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
        
        public static float Dot(this Sensor from, ITargetData to, float predictionTime)
        {
            Vector3 dir = to.Position + to.Velocity * predictionTime - from.Position + from.Velocity * predictionTime;
            return Vector3.Dot(from.Rotation * Vector3.forward, dir);
        }

        public static float Distance(this Sensor from, ITargetData to, float predictionTime = 0)
        {
            if (predictionTime == 0)
            {
                return Vector3.Distance(from.Position, to.Position);
            }
            else
            {
                return Vector3.Distance(from.Position + from.Velocity * predictionTime, to.Position + to.Velocity * predictionTime);
            }
        }

        //public static float TimeToReach(this Sensor from, ITargetData to)
        //{
        //    Vector3 deltaPos = to.Position - from.Position;
        //    Vector3 relativeVelocity = to.Velocity + from.Velocity;
        //    float dot = Vector3.Dot(deltaPos.normalized, relativeVelocity.normalized);
        //    if (dot <= 0)
        //    {
        //        return Mathf.Infinity;
        //    }
        //    
        //    return deltaPos.magnitude / (relativeVelocity.magnitude * dot);
        //}

        public static SignatureDataWarp? GetClosestEnemy(this UnitEntity unit, TableRelations tableRelations)
        {
            SignatureDataWarp? closestEnemy = null;
            for (var i = 0; i < unit.Unit.Sensor.NeighbourSignatures.Count; i++)
            {
                var relation = tableRelations.GetRelation(unit.SignatureId,
                    unit.Unit.Sensor.NeighbourSignatures[i].Data.SignatureId);
                if (relation < RelationType.Neutral)
                {
                    closestEnemy = unit.Unit.Sensor.NeighbourSignatures[i];
                    break;
                }
            }

            return closestEnemy;
        }
    }
}