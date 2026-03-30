using Core.Ai;
using UnityEngine;

namespace Runtime.Ai.Maneuvers
{
    public class Aiming : IManeuver
    {
        private IUnitControl _control;
        private DirectionToTarget _aimingDir;
        private UnitTechCharacteristic _characteristic;
        private ITargetData _target;
        private Sensor _sensor;
        private float _chaseFactor = 0.5f;

        public Aiming(ITargetData target)
        {
            _target = target;
        }

        public void InjectControls(IUnit unit, IUnitControl control, Sensor sensor)
        {
            _sensor = sensor;
            _control = control;
            _aimingDir = new DirectionToTarget(_target);
            _characteristic = unit.GetTechCharacteristic();
        }
        
        public void Enter()
        {
            _control.SetForwardDirection(_aimingDir);
            _control.SetDriftCompensation(0.1f);
            _control.SetUpVector(new ConstantDirection(Vector3.up));
            _control.SetRollYawFactor(0.3f);
            _control.SetRollBackFactor(0.4f);
        }

        public bool Tick()
        {
            _aimingDir.Correction = Quaternion.Inverse(Quaternion.LookRotation(_sensor.MainCaliberWantedDirectionLocalSpace));
            float distance = _sensor.Distance(_target);
            float chargeFlyTime = distance / _sensor.MainCaliberChargeInitialSpeed;
            
            _control.SetPredictionTime(chargeFlyTime);
            _control.SetFollowSpeed(_target, _sensor, chargeFlyTime, Vector3.zero/*- _aimingDir.GetDirection(_sensor.Position) * distance*/, _chaseFactor, _characteristic.minimalForwardSpeed);
            
            return false;
        }

        public void Exit()
        {
        }
    }
}