using System;
using System.Collections.Generic;
using Core.Ai;
using Core.World;
using Runtime.Structure.Ship;
using UnityEngine;

namespace Runtime.Ai
{
    [RequireComponent(typeof(IUnitControl), typeof(DynamicStructure))]
    public class StructureUnit : MonoBehaviour, IUnit
    {
        [SerializeField, SerializeReference] private IUnitTactic myTactic = new EmptyTactic();
        private IUnitControl _myControl;
        private DynamicStructure _structure;
        private Sensor _sensor = new Sensor();
        private IManeuver _currentManeuver;
        private IManeuverEndpoint _currentEndpoint;
        private Queue<(IManeuver, IManeuverEndpoint)> _maneuvers = new();
        private UnitEntity _entity;
        public bool IsManeuversDone => _currentEndpoint == null;
        public int EntityId => _entity.Id;
        
        private void Awake()
        {
            _myControl = GetComponent<IUnitControl>();
            _structure = GetComponent<DynamicStructure>();
        }

        private void OnEnable()
        {
            ((MonoBehaviour)_myControl).enabled = true;
        }

        private void OnDisable()
        {
            ((MonoBehaviour)_myControl).enabled = false;
        }

        public void InjectEntity(UnitEntity entity)
        {
            _entity = entity;
        }

        public void SetTactic(IUnitTactic tactic)
        {
            myTactic = tactic ?? new EmptyTactic();
        }

        public void SetManeuvers((IManeuver, IManeuverEndpoint)[] maneuvers)
        {
            _maneuvers.Clear();
            if (maneuvers.Length == 0)
            {
                _currentManeuver = null;
                _currentEndpoint = null;
                return;
            }
            for (var i = 0; i < maneuvers.Length; i++)
            {
                _maneuvers.Enqueue(maneuvers[i]);
            }

            RefreshNextManeuver();
        }

        private bool RefreshNextManeuver()
        {
            while (_maneuvers.Count > 0)
            {
                (IManeuver maneuver, IManeuverEndpoint endpoint) = _maneuvers.Dequeue();
                if (!endpoint.IsComplete(maneuver, _myControl, _sensor))
                {
                    _currentEndpoint = endpoint;
                    _currentManeuver?.Exit(_myControl, _sensor);
                    _currentManeuver = maneuver;
                    _currentManeuver.Enter(_myControl, _sensor);
                    return true;
                }
            }
            return false;
        }

        public void Update()
        {
            UpdateSensor();
            TickManeuver();
            myTactic.ControlUnit(this, _sensor);
        }

        private void UpdateSensor()
        {
            _sensor.Position = transform.position;
            _sensor.Rotation = transform.rotation;
            _sensor.Velocity = _structure.Velocity;
            _sensor.LocalVelocity = transform.InverseTransformDirection(_sensor.Velocity);
        }
        
        private void TickManeuver()
        {
            if (!_myControl.IsActive)
            {
                return;
            }
            if (_currentEndpoint == null)
            {
                return;
            }
            if (_currentEndpoint.IsComplete(_currentManeuver, _myControl, _sensor))
            {
                if (!RefreshNextManeuver())
                {
                    return;
                }
            }
            _currentManeuver.Tick(_myControl, _sensor);
        }
    }
}