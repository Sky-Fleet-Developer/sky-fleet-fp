using System.Collections.Generic;
using Core.Ai;
using Core.World;
using UnityEngine;

namespace Runtime.Ai
{
    public class StructureUnit : MonoBehaviour, IUnit
    {
        [SerializeField, SerializeReference] private IUnitTactic myTactic;
        private IUnitControl _myControl;
        private Sensor _sensor;
        private IManeuver _currentManeuver;
        private IManeuverEndpoint _currentEndpoint;
        private Queue<(IManeuver, IManeuverEndpoint)> _maneuvers = new();
        private UnitEntity _entity;
        public bool IsManeuversDone => _currentEndpoint == null;
        public int EntityId => _entity.Id;
        
        private void Awake()
        {
            _myControl = GetComponent<IUnitControl>();
        }

        public void InjectEntity(UnitEntity entity)
        {
            _entity = entity;
        }

        public void SetTactic(IUnitTactic tactic)
        {
            myTactic = tactic;
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
            _currentManeuver = maneuvers[0].Item1;
            _currentEndpoint = maneuvers[0].Item2;
            for (var i = 1; i < maneuvers.Length; i++)
            {
                _maneuvers.Enqueue(maneuvers[i]);
            }
        }

        private bool RefreshNextManeuver()
        {
            while (_maneuvers.Count > 0)
            {
                (IManeuver maneuver, IManeuverEndpoint endpoint) = _maneuvers.Dequeue();
                if (!endpoint.IsComplete(maneuver, _myControl, _sensor))
                {
                    _currentEndpoint = endpoint;
                    _currentManeuver = maneuver;
                    return true;
                }
            }
            return false;
        }

        public void Update()
        {
            TickManeuver();
            myTactic.ControlUnit(this, _sensor);
        }

        private void TickManeuver()
        {
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