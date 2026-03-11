using Core.Ai;
using UnityEngine;

namespace Runtime.Ai.Maneuvers
{
    public class Follow : IManeuver
    {
        private ITargetData _target;
        private float _predictionTime = 2f;
        private float _chaseFactor = 0.1f;
        private Vector3 _followOffset;

        public Follow(ITargetData target, Vector3 followOffset)
        {
            _target = target;
            _followOffset = followOffset;
        }

        public void Tick(IUnitControl control, Sensor sensor)
        {
            Vector3 v = _target.Velocity;
            float vMag = Mathf.Max(v.magnitude, 0.001f);
            //Vector3 vDir = v / vMag;
            Vector3 targetPredicted = (_target.Position + v * _predictionTime);
            Vector3 selfPredicted = sensor.Position + sensor.Velocity * _predictionTime;
            Vector3 predictedDelta = targetPredicted - selfPredicted;
            Vector3 fwd = sensor.Rotation * Vector3.forward;
            float acceleration = Vector3.Dot(fwd, predictedDelta) * _chaseFactor;
            
            control.SetTargetVelocity(predictedDelta.normalized * (vMag + acceleration), MovementType.Direct);
        }
    }
}