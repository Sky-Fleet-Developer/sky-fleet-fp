using Core.Ai;
using Core.Character.Interaction;
using Core.Misc;
using UnityEngine;

namespace Runtime.Ai.Maneuvers
{
    public class Aiming : IManeuver
    {
        private IUnitControl _control;
        private DirectionToTarget _aimingDir;
        private DirectionToTarget _followDir;
        private UnitTechCharacteristic _characteristic;
        private ITargetData _target;
        private Sensor _sensor;
        private float _chaseFactor = 0.5f;
        private IUnit _unit;
        private bool _shootWhenReady;
        private IWeaponHandler _mainWeapon;
        private float _accuracyCos;
        

        public Aiming(ITargetData target, bool shootWhenReady)
        {
            _shootWhenReady = shootWhenReady;
            _target = target;
        }

        public void InjectControls(IUnit unit, IUnitControl control, Sensor sensor)
        {
            _unit = unit;
            _sensor = sensor;
            _control = control;
            _aimingDir = new DirectionToTarget(_target);
            _followDir = new DirectionToTarget(_target);
            _characteristic = unit.GetTechCharacteristic();
            _mainWeapon = control.GetMainWeapon();
            if (_mainWeapon != null)
            {
                _accuracyCos = Mathf.Cos(_mainWeapon.Accuracy * Mathf.Deg2Rad);
            }
        }
        
        public void Enter()
        {
            //Debug.Log($"Enter Aiming ({_unit.EntityId})");
            _control.SetForwardDirection(_followDir);
            _control.SetDriftCompensation(0.1f);
            _control.SetUpVector(new ConstantDirection(Vector3.up));
            _control.SetRollYawFactor(0.5f);
            _control.SetRollBackFactor(0.15f);
            _control.SetAcuity(1.5f);
            _control.SetAimingVector(_aimingDir);
        }

        public bool Tick()
        {
            _aimingDir.Correction = Quaternion.Inverse(Quaternion.LookRotation(_sensor.MainCaliberWantedDirectionLocalSpace));
            _followDir.Correction = _aimingDir.Correction;
            float distance = _sensor.Distance(_target);
            float chargeFlyTime = distance / _sensor.MainCaliberChargeInitialSpeed;
            
            _control.SetPredictionTime(chargeFlyTime/* + Mathf.PingPong(_dash, 1f)*/);
            _control.SetFollowSpeed(_target, _sensor, chargeFlyTime, Vector3.zero/*- _aimingDir.GetDirection(_sensor.Position) * distance*/, _chaseFactor, _characteristic.minimalForwardSpeed);
            _aimingDir.DrawGizmos(_sensor.Position, Color.red);

            if (_shootWhenReady && _control.IsWeaponActive) // TODO: make a dash toward aim target when close to target
            {
                TransformCache weaponMuzzle = _mainWeapon.MuzzleThreadSafe;
                Vector3 dir = _target.Position - weaponMuzzle.Position + (_target.Velocity - _sensor.Velocity) * chargeFlyTime;
                float dot = Vector3.Dot(weaponMuzzle.Rotation * Vector3.forward, dir.normalized);
                Debug.DrawRay(weaponMuzzle.Position, weaponMuzzle.Rotation * Vector3.forward * 100, Color.yellow);
                //Debug.DrawRay(weaponMuzzle.Position, dir, Color.green);
                if (dot > Mathf.Cos(10))
                {
                    //Debug.Log(Mathf.Acos(dot) * Mathf.Rad2Deg);
                    Vector3 dashAxis = Vector3.Cross(dir, weaponMuzzle.Rotation * Vector3.forward);
                    _followDir.Correction *= Quaternion.AngleAxis(-1.5f + Mathf.PingPong(Time.time * 0.7f, 1f) * 3, dashAxis);
                    Debug.DrawRay(_sensor.Position, _followDir.GetDirection(_sensor.Position), Color.red);
                }

                if (dot > _accuracyCos)
                {
                    _mainWeapon.Fire();
                }
            }
            return false;
        }

        public void Exit()
        {
            _control.SetAcuity(1);
        }
    }
}