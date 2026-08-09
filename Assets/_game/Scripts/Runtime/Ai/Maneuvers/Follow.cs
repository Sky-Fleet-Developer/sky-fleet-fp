using Core.Ai;
using UnityEngine;

namespace Runtime.Ai.Maneuvers
{
    public class Follow : IManeuver
    {
        private DirectionToTarget _directionToTarget;
        private ITargetData _target;
        private float _predictionTime = 2f;
        private float _chaseFactor = 0.5f;
        private Vector3 _followOffset;
        private IUnitControl _control;
        private UnitTechCharacteristic _characteristic;
        private Sensor _sensor;

        public Follow(ITargetData target, Vector3 followOffset)
        {
            _target = target;
            _followOffset = followOffset;
            _directionToTarget = new DirectionToTarget(target);
        }

        public void InjectControls(IUnit unit, IUnitControl control, Sensor sensor)
        {
            _sensor = sensor;
            _control = control;
            _characteristic = unit.GetTechCharacteristic();
        }

        public void Enter()
        {
            _control.SetForwardDirection(_directionToTarget);
            _control.SetDriftCompensation(0.15f);
            _control.SetUpVector(new ConstantDirection(Vector3.up));
            _control.SetRollYawFactor(0.4f);
            _control.SetRollBackFactor(0.8f);
            _control.SetPredictionTime(4f);
        }

        public bool Tick()
        {
            _control.SetFollowSpeed(_target, _sensor, _predictionTime, _followOffset, _chaseFactor, _characteristic.minimalForwardSpeed);
            return false;
        }

        public void Exit()
        {
        }
    }
}