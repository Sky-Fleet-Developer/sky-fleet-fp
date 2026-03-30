using Core.Ai;
using Core.World;
using UnityEngine;

namespace Runtime.Ai.Maneuvers
{
    public class DownAway : IManeuver
    {
        private const float TurnSpeed = 45;
        private SmoothTurn _forward;
        private Vector3 _rotationCenter;
        private Vector3 _normal;
        private Vector3 _initialForward;
        private IUnitControl _control;
        private Sensor _sensor;
        private bool _isTurnDone;
        private float _cruiseSpeed;
        private float _initialHeight;
        

        public void InjectControls(IUnit unit, IUnitControl control, Sensor sensor)
        {
            _sensor = sensor;
            _control = control;
            _cruiseSpeed = unit.GetTechCharacteristic().cruiseSpeed;
        }

        public void Enter()
        {
            _normal = Vector3.Cross(_sensor.Velocity, Vector3.up); // TODO: turn normal left, when target's course is right and vice versa
            _initialForward = Vector3.ProjectOnPlane(_sensor.Velocity, Vector3.up).normalized;
            _initialHeight = _sensor.Position.y + WorldOffset.Offset.y;
            _forward = new SmoothTurn
            {
                Self = _sensor,
                Value = Quaternion.AngleAxis(TurnSpeed, _normal)
            };
            
            _control.SetForwardDirection(_forward);
            _control.SetUpVector(_forward);
            _control.SetDriftCompensation(0);
            _control.SetRollYawFactor(0.7f);
            _control.SetRollBackFactor(0.9f);
            _control.SetPredictionTime(0);
            _control.SetSpeed(_cruiseSpeed);
        }

        public bool Tick()
        {
            if (!_isTurnDone)
            {
                if (Vector3.Dot(-_initialForward, _sensor.Velocity) > 0.5f)
                {
                    _isTurnDone = true;
                    _control.SetUpVector(new ConstantDirection(Vector3.up));
                    Vector3 currentForward = Vector3.ProjectOnPlane(_sensor.Velocity, Vector3.up).normalized;
                    currentForward.y = 0.3f;
                    _control.SetForwardDirection(new ConstantDirection(currentForward));
                    _control.SetRollYawFactor(0.3f);
                    _control.SetRollBackFactor(0.4f);
                    _control.SetSpeed(_cruiseSpeed * 1.3f);
                }

                return false;
            }
            else
            {
                return _sensor.Position.y + WorldOffset.Offset.y > _initialHeight;
            }
        }

        public void Exit()
        {
        }
    }
}