using Core.Ai;
using UnityEngine;

namespace Runtime.Ai
{
    public class MoveToDeadZoneManeuver : IManeuver
    {
        private ManualTargetData _targetData = new();
        private DirectionToTarget _directionToTarget;
        private IUnitControl _control;
        private UnitTechCharacteristic _characteristic;
        private Sensor _sensor;

        public void InjectControls(IUnit unit, IUnitControl control, Sensor sensor)
        {
            _sensor = sensor;
            _control = control;
            _characteristic = unit.GetTechCharacteristic();
        }

        public void Enter()
        {
            _directionToTarget = new DirectionToTarget(_targetData);
            _control.SetForwardDirection(_directionToTarget);
            _control.SetDriftCompensation(0.15f);
            _control.SetUpVector(new ConstantDirection(Vector3.up));
            _control.SetRollYawFactor(0.2f);
            _control.SetRollBackFactor(0.1f);
            _control.SetPredictionTime(0f);
            _control.SetSpeed(_characteristic.cruiseSpeed * 2);
            _control.SetAcuity(1.5f);
        }

        public bool Tick()
        {
            MenaceRef mainMenace = _sensor.Menaces[0];
            Vector3 blindZoneUnitSpace = mainMenace.Menace.MyUnit.GetTechCharacteristic().blindZone.normalized;
            Vector3 blindZone = mainMenace.Menace.MyUnit.GetGlobalDirectionThreadSafe(blindZoneUnitSpace);
            Vector3 projection = _sensor.Position - mainMenace.Menace.MyUnit.Position;
            Vector3 projectedDirection = Vector3.ProjectOnPlane(blindZone, projection).normalized;
            _targetData.Position = mainMenace.Menace.MyUnit.Position + projectedDirection * Mathf.Max(60, projection.magnitude);
            _targetData.Rotation = Quaternion.identity;
            _targetData.Velocity = mainMenace.Menace.MyUnit.Velocity;
            return Vector3.Dot(projection, blindZone) > 0.8f;
        }

        public void Exit()
        {
            _control.SetAcuity(1);
        }
    }
}