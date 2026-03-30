using System;
using System.Collections.Generic;
using Core.Ai;
using Core.Data;
using Core.Misc;
using Core.World;
using Runtime.Structure.Ship;
using UnityEngine;
using Zenject;
using ITickable = Core.Misc.ITickable;

namespace Runtime.Ai
{
    [RequireComponent(typeof(IUnitControl), typeof(DynamicStructure))]
    public class StructureUnit : MonoBehaviour, IUnit, ITickable
    {
        static StructureUnit()
        {
            Core.Misc.TickService.SetOrderBefore(typeof(StructureUnit), typeof(MultipleUnitTacticBase));
        }
        
        private const float RefreshNearSignaturesInterval = 1f / 5f;
        
        [SerializeField, SerializeReference] private IUnitTactic myTactic = new EmptyTactic();
        [SerializeField] private UnitTechCharacteristic myTechCharacteristic;
        private IUnitControl _myControl;
        private DynamicStructure _structure;
        private Sensor _sensor = new Sensor() {MainCaliberWantedDirectionLocalSpace = Vector3.forward, MainCaliberChargeInitialSpeed = 800f};
        private IManeuver _currentManeuver;
        private Queue<IManeuver> _maneuvers = new();
        private UnitEntity _entity;
        [Inject] private WorldGrid _worldGrid;
        [Inject] private TickService _tickService;
        public bool IsManeuversComplete => _isManeuversComplete;
        public int EntityId => _entity.Id;
        private float _refreshNearSignaturesTimer;
        private bool _isManeuversComplete;
        public Sensor Sensor => _sensor;
        int ITickable.TickRate => 1;
        public UnitTechCharacteristic GetTechCharacteristic() => myTechCharacteristic;
        
        private void Awake()
        {
            _myControl = GetComponent<IUnitControl>();
            _structure = GetComponent<DynamicStructure>();
        }

        private void OnEnable()
        {
            ((MonoBehaviour)_myControl).enabled = true;
            if (myTactic != null)
            {
                myTactic.UnitEnterTactic(_entity);
            }
            _tickService.Add(this);
        }

        private void OnDisable()
        {
            ((MonoBehaviour)_myControl).enabled = false;
            if (myTactic != null)
            {
                myTactic.UnitExitTactic(_entity);
            }
            _tickService.Remove(this);
        }

        public void InjectEntity(UnitEntity entity)
        {
            _entity = entity;
        }
        
        public void SetTactic(IUnitTactic tactic)
        {
            if (myTactic != null)
            {
                myTactic.UnitExitTactic(_entity);
            }
            myTactic = tactic ?? new EmptyTactic();
            myTactic.UnitEnterTactic(_entity);
        }

        public void SetManeuvers(IManeuver[] maneuvers)
        {
            _maneuvers.Clear();
            if (maneuvers.Length == 0)
            {
                _currentManeuver = null;
                return;
            }
            for (var i = 0; i < maneuvers.Length; i++)
            {
                _maneuvers.Enqueue(maneuvers[i]);
            }

            _isManeuversComplete = !NextManeuver();
        }

        private bool NextManeuver()
        {
            if (_maneuvers.Count == 0)
            {
                return false;
            }

            _currentManeuver?.Exit();
            _currentManeuver = _maneuvers.Dequeue();
            _currentManeuver.InjectControls(this, _myControl, _sensor);
            _currentManeuver.Enter();
            return true;
        }

        public void Tick()
        {
            UpdateSensor();
            TickManeuver();
        }

        private void UpdateSensor()
        {
            _sensor.Position = transform.position;
            _sensor.Rotation = transform.rotation;
            _sensor.Velocity = _structure.Velocity;
            _sensor.LocalVelocity = transform.InverseTransformDirection(_sensor.Velocity);
            if (Physics.Raycast(transform.position, Vector3.down, out var hit, 10000, GameData.Data.terrainLayer))
            {
                _sensor.Height = transform.position.y - hit.point.y;
            }
            else
            {
                _sensor.Height = transform.position.y;
            }

            _refreshNearSignaturesTimer -= Time.deltaTime;
            if (_refreshNearSignaturesTimer < 0)
            {
                _refreshNearSignaturesTimer = RefreshNearSignaturesInterval;
                _sensor.NearSignatures.Clear();
                foreach ((IWorldEntity entity, Vector3Int cell) in
                         _worldGrid.EnumerateNeighbours(transform.position, 1))
                {
                    if (entity != this && entity is UnitEntity unitEntity)
                    {
                        _sensor.NearSignatures.Add(unitEntity);
                    }
                }
            }
        }
        
        private void TickManeuver()
        {
            if (!_myControl.IsActive)
            {
                return;
            }

            if (_currentManeuver.Tick())
            {
                NextManeuver();
            }
        }
    }
}