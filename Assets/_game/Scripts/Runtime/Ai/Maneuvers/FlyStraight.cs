using Core.Ai;
using UnityEngine;

namespace Runtime.Ai.Maneuvers
{
    public class FlyStraight : IManeuver
    {
        private IUnitControl _control;
        private IDirectionData _forward;
        private UnitTechCharacteristic _characteristic;

        public void InjectControls(IUnit unit, IUnitControl control, Sensor sensor)
        {
            _control = control;
            _forward = new ConstantDirection(sensor.Rotation * Vector3.forward);
            _characteristic = unit.GetTechCharacteristic();
        }

        public void Enter()
        {
            _control.SetForwardDirection(_forward);
            _control.SetDriftCompensation(0.15f);
            _control.SetUpVector(new ConstantDirection(Vector3.up));
            _control.SetRollYawFactor(0.4f);
            _control.SetRollBackFactor(0.8f);
            _control.SetPredictionTime(4f);
            _control.SetSpeed(_characteristic.cruiseSpeed);
        }

        public bool Tick()
        {
            return false;
        }

        public void Exit()
        {
        }
    }
}