using Core.Ai;
using UnityEngine;

namespace Runtime.Ai.Maneuvers
{
    public class RotateTowards : IManeuver
    {
        private float _speed;
        private DirectionToTarget _direction;
        private ITargetData _target;
        private IUnitControl _control;
        private Sensor _sensor;
        private IUnit _unit;

        public RotateTowards(ITargetData target, float howFast = 1)
        {
            _target = target;
            _speed = howFast;
        }
        
        public void InjectControls(IUnit unit, IUnitControl control, Sensor sensor)
        {
            _unit = unit;
            _sensor = sensor;
            _control = control;
            _direction = new DirectionToTarget(_target);
            _speed *= unit.GetTechCharacteristic().cruiseSpeed;
        }

        public void Enter()
        {
            Debug.Log($"Enter RotateTowards ({_unit.EntityId})");
            _control.SetForwardDirection(_direction);
            _control.SetDriftCompensation(0.1f);
            _control.SetUpVector(new ConstantDirection(Vector3.up));
            _control.SetRollYawFactor(0.6f);
            _control.SetRollBackFactor(0.5f);
            _control.SetAcuity(0.6f);
            _control.SetSpeed(_speed);
        }

        public bool Tick()
        {
            Vector3 current = _sensor.Rotation * _sensor.MainCaliberWantedDirectionLocalSpace;
            Vector3 wanted = _direction.GetDirection(_sensor.Position).normalized;
            Debug.DrawRay(_sensor.Position, wanted * 15, Color.yellow);
            Debug.DrawRay(_sensor.Position, current * 15, Color.green);
            var dot = Vector3.Dot(current, wanted);
            bool done = dot > 0.95f;
            if (done)
            {
                Debug.Log($"Exit RotateTowards ({_unit.EntityId})");
            }
            return done;
        }

        public void Exit()
        {
            _control.SetAcuity(1);
        }
    }
}