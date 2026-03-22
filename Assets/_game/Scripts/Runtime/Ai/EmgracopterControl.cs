using System;
using System.Collections.Generic;
using Core.Ai;
using Core.Character.Interaction;
using Core.Structure;
using NUnit.Framework;
using NWH.Common.Utility;
using Runtime.Structure.Ship;
using UnityEngine;
using UnityEngine.Serialization;

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
        private DynamicStructure _structure;
        private List<IDriveHandler> _driveHandlers = new();
        private IDriveHandler _mainDriveHandler;
        private IDirectionData _up;
        private IDirectionData _forward;
        private float _wantedSpeed;
        private float _rollYawFactor;
        private float _rollBackFactor;
        private float _driftCompensation;
        private float _predictionTime;

        public bool IsActive => _mainDriveHandler != null;
        
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
        }

        private void OnDisable()
        {
            _structure.OnBlockAddedEvent -= BlockAdded;
            _structure.OnBlockRemovedEvent -= BlockRemoved;
        }

        private void Update()
        {
            if (IsActive)
            {
                Vector3 fwd = transform.InverseTransformDirection(_forward.GetPredictedDirection(transform.position, Vector3.zero, _predictionTime)).normalized;
                Vector3 up = transform.InverseTransformDirection(_up.GetDirection(transform.position)).normalized;
                Vector3 velocity = transform.InverseTransformDirection(_structure.Velocity);
                Vector3 angularVelocity = transform.InverseTransformDirection(_structure.AngularVelocity);
                
                _mainDriveHandler.PitchAxis = Mathf.Clamp(-fwd.y * pitchSensitivity - angularVelocity.x * pitchDumper, -1, 1);
                _mainDriveHandler.RollAxis = Mathf.Clamp((-fwd.x * (1 - _rollYawFactor) //turn by roll
                                             - up.x * (_rollYawFactor + _rollBackFactor) //align to up
                                             + velocity.x * _driftCompensation) * rollSensitivity 
                                             - angularVelocity.z * rollDumper,
                                            -1, 1);

                float yawControlValue = fwd.x * _rollYawFactor * yawSensitivity; //turn by yaw
                float yawDumping = -angularVelocity.y * yawDumper;
                _mainDriveHandler.YawAxis = Mathf.Clamp(yawControlValue + yawDumping, -1, 1);
                
                _mainDriveHandler.ThrustAxis = Mathf.Clamp01((_wantedSpeed - velocity.z) * throttleSensitivity);
                _mainDriveHandler.SupportsPowerAxis = 1;
                //Debug.DrawRay(transform.position - transform.forward * 4, transform.right * _mainDriveHandler.YawAxis * 5, Color.yellow);
                //Debug.DrawRay(transform.position + transform.right * 3, transform.up * _mainDriveHandler.RollAxis * 5, Color.red);
                //Debug.DrawRay(transform.position - transform.right * 3, -transform.up * _mainDriveHandler.RollAxis * 5, Color.red);
                //Debug.DrawRay(transform.position + transform.forward * 4, transform.up * _mainDriveHandler.PitchAxis * 5, Color.green);
            }
        }

        private void OnGUI()
        {
            var skin = GUI.skin;
            skin.box.fontSize = 20;
            GUI.skin = skin;
            GUILayout.BeginVertical();
            GUILayout.Box($"Pitch: {_mainDriveHandler.PitchAxis}");
            GUILayout.Box($"Roll: {_mainDriveHandler.RollAxis}");
            GUILayout.Box($"Yaw: {_mainDriveHandler.YawAxis}");
            GUILayout.Box($"Thrust: {_mainDriveHandler.ThrustAxis}");
            GUILayout.EndVertical();
        }

        public void SetUpVector(IDirectionData direction)
        {
            _up = direction;
        }

        public void SetForwardVector(IDirectionData direction)
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
    }
}