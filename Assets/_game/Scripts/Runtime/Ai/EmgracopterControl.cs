using System.Collections.Generic;
using Core.Ai;
using Core.Character.Interaction;
using Core.Structure;
using Runtime.Structure.Ship;
using UnityEngine;

namespace Runtime.Ai
{

    [RequireComponent((typeof(DynamicStructure)))]
    public class EmgracopterControl : MonoBehaviour, IUnitControl
    {
        [SerializeField] private float pitchSensitivity = 1;
        [SerializeField] private float pitchDumper = 0.1f;
        [SerializeField] private float rollSensitivity = 1;
        [SerializeField] private float rollDumper = 0.1f;
        [SerializeField] private float yawSensitivity = 1;
        [SerializeField] private float yawDumper = 0.1f;
        [SerializeField] private float throttleSensitivity = 0.05f;
        [SerializeField] private AnimationCurve suppressControlBySpeed;
        private DynamicStructure _structure;
        private List<IDriveHandler> _driveHandlers = new();
        private List<IWeaponHandler> _weaponHandlers = new();
        private IDriveHandler _mainDriveHandler;
        private IWeaponHandler _mainWeaponHandler;
        private IDirectionData _up;
        private IDirectionData _forward;
        private float _wantedSpeed;
        private float _rollYawFactor;
        private float _rollBackFactor;
        private float _driftCompensation;
        private float _predictionTime;
        private float _acuity = 1;
        private IDirectionData _aimingDirection;
        //private Ray _currentAimingRay;

        public bool IsActive => _mainDriveHandler != null;

        public bool IsWeaponActive => _mainWeaponHandler != null;

        private void Awake()
        {
            _structure = GetComponent<DynamicStructure>();
            _forward = new ConstantDirection(transform.forward);
            _up = new ConstantDirection(Vector3.up);
        }

        private void OnEnable()
        {
            _structure.OnBlockAddedEvent += BlockAdded;
            _structure.OnBlockRemovedEvent += BlockRemoved;
            _driveHandlers.Clear();
            _driveHandlers.AddRange(_structure.GetBlocksByType<IDriveHandler>());
            if (_driveHandlers.Count > 0)
            {
                SwitchMainDriveHandler(_driveHandlers[0]);
            }
            _weaponHandlers.Clear();
            _weaponHandlers.AddRange(_structure.GetBlocksByType<IWeaponHandler>());
            if (_weaponHandlers.Count > 0)
            {
                SwitchMainWeaponHandler(_weaponHandlers[0]);
            }
        }

        private void OnDisable()
        {
            _structure.OnBlockAddedEvent -= BlockAdded;
            _structure.OnBlockRemovedEvent -= BlockRemoved;
        }

        private void Update()
        {
            ControlMovement();
            ControlWeapon();
        }

        private void ControlMovement()
        {
            if (!IsActive)
            {
                return;
            }
            
            Vector3 fwd = transform.InverseTransformDirection(_forward.GetPredictedDirection(transform.position, Vector3.zero, _predictionTime)).normalized;
            Vector3 up = transform.InverseTransformDirection(_up.GetDirection(transform.position)).normalized;
            Vector3 velocity = transform.InverseTransformDirection(_structure.Velocity);
            Vector3 angularVelocity = transform.InverseTransformDirection(_structure.AngularVelocity);
            Debug.DrawRay(transform.position, transform.rotation * up * 8, Color.green);
            Debug.DrawRay(transform.position, transform.rotation * fwd * 10, Color.blue);
            // flip when up is down
            float upVal = up.x;
            if (up.y < 0)
            {
                upVal = Mathf.Sign(upVal);
            }

            float suppressionBySpeed = suppressControlBySpeed.Evaluate(velocity.magnitude);
            _mainDriveHandler.PitchAxis = Mathf.Clamp((-fwd.y * pitchSensitivity - angularVelocity.x * pitchDumper) * _acuity, -1, 1) * suppressionBySpeed;
            _mainDriveHandler.RollAxis = Mathf.Clamp(((-fwd.x * (1 - _rollYawFactor) //turn by roll
                                                      - upVal * (_rollYawFactor + _rollBackFactor) //align to up
                                                      + velocity.x * _driftCompensation) * rollSensitivity 
                                                     - angularVelocity.z * rollDumper) * _acuity,
                -1, 1) * suppressionBySpeed;

            float yawControlValue = fwd.x * _rollYawFactor * yawSensitivity; //turn by yaw
            float yawDumping = -angularVelocity.y * yawDumper;
            _mainDriveHandler.YawAxis = Mathf.Clamp((yawControlValue + yawDumping) * _acuity, -1, 1) * suppressionBySpeed;
                
            _mainDriveHandler.ThrustAxis = Mathf.Clamp01((_wantedSpeed - velocity.z) * throttleSensitivity);
            _mainDriveHandler.SupportsPowerAxis = 1;
            //Debug.DrawRay(transform.position - transform.forward * 4, transform.right * _mainDriveHandler.YawAxis * 5, Color.yellow);
            //Debug.DrawRay(transform.position + transform.right * 3, transform.up * _mainDriveHandler.RollAxis * 5, Color.red);
            //Debug.DrawRay(transform.position - transform.right * 3, -transform.up * _mainDriveHandler.RollAxis * 5, Color.red);
            //Debug.DrawRay(transform.position + transform.forward * 4, transform.up * _mainDriveHandler.PitchAxis * 5, Color.green);
        }

        private void ControlWeapon()
        {
            if (!IsWeaponActive)
            {
                return;
            }
            
            if (!_mainWeaponHandler.CanAimHorizontally && !_mainWeaponHandler.CanAimVertically)
            {
                return;
            }
            
            //_currentAimingRay = _mainWeaponHandler.GetAimingRay();
            Vector3 direction = _aimingDirection.GetDirection(transform.position);
            
            //TODO: rotate weapon
        }

        //private void OnGUI()
        //{
        //    var skin = GUI.skin;
        //    skin.box.fontSize = 20;
        //    GUI.skin = skin;
        //    GUILayout.BeginVertical();
        //    GUILayout.Box($"Pitch: {_mainDriveHandler.PitchAxis}");
        //    GUILayout.Box($"Roll: {_mainDriveHandler.RollAxis}");
        //    GUILayout.Box($"Yaw: {_mainDriveHandler.YawAxis}");
        //    GUILayout.Box($"Thrust: {_mainDriveHandler.ThrustAxis}");
        //    GUILayout.EndVertical();
        //}
        
        public void SetUpVector(IDirectionData direction)
        {
            _up = direction;
        }

        public void SetForwardDirection(IDirectionData direction)
        {
            _forward = direction;
        }

        public void SetPredictionTime(float time)
        {
            _predictionTime = time;
        }

        public void SetSpeed(float speed)
        {
            _wantedSpeed = speed;
        }

        public void SetRollYawFactor(float factor)
        {
            _rollYawFactor = factor;
        }

        public void SetRollBackFactor(float factor)
        {
            _rollBackFactor = factor;
        }

        public void SetDriftCompensation(float value)
        {
            _driftCompensation = value;
        }
        
        public IWeaponHandler GetMainWeapon() => _mainWeaponHandler;
        
        public void SetAimingVector(IDirectionData direction)
        {
            _aimingDirection = direction;
        }
        
        public void SetAcuity(float value)
        {
            _acuity = value;
        }

        private void BlockAdded(IBlock block)
        {
            if(block is IDriveHandler driveHandler)
            {
                _driveHandlers.Add(driveHandler);
                SwitchMainDriveHandler(_driveHandlers[0]);
            }
        }
        
        private void BlockRemoved(IBlock block)
        {
            if(block is IDriveHandler driveHandler)
            {
                _driveHandlers.Remove(driveHandler);
                if (_mainDriveHandler == driveHandler)
                {
                    if (_driveHandlers.Count > 0)
                    {
                        SwitchMainDriveHandler(_driveHandlers[0]);
                    }
                    else
                    {
                        SwitchMainDriveHandler(null);
                    }
                }
            }
        }

        private void SwitchMainDriveHandler(IDriveHandler driveHandler)
        {
            if (_mainDriveHandler != null)
            {
                _mainDriveHandler.ResetControls();
            }
            _mainDriveHandler = driveHandler;
        }

        private void SwitchMainWeaponHandler(IWeaponHandler weaponHandler)
        {
            if (_mainWeaponHandler != null)
            {
                _mainWeaponHandler.ResetControls();
            }
            _mainWeaponHandler = weaponHandler;
        }
    }
}