using System;
using System.Collections.Generic;
using Core.Ai;
using Core.Misc;
using Core.World;
using Runtime.Misc;
using UnityEngine;
using Zenject;
using ITickable = Core.Misc.ITickable;

namespace Runtime.Ai
{
    public class ArenaFightStrategy : MonoBehaviour, ITickable, IAiPathStrategy, IWorldEntityDisposeListener
    {
        [SerializeField] private SpsViewRange spline;
        [SerializeField] private float alarmRadius = 700;
        private List<UnitEntity> _controllableUnits = new ();
        private SpsFollowTactic _followSplineTactic;
        private Dictionary<int, int> _pathLinkedUnits = new ();
        private Dictionary<int, SpsFrozenPoint> _unitsFrozenPoints = new ();
        [Inject] private TickService _tickService;
        [Inject] private TableRelations _tableRelations;
        public int TickRate => 5;

        private void Start()
        {
            _tickService.Add(this);
        }
        private void OnDestroy()
        {
            _tickService.Remove(this);
            _followSplineTactic.Dispose();
        }

        [Inject]
        private void Inject(DiContainer container)
        {
            _followSplineTactic = new SpsFollowTactic(_tickService);
            _followSplineTactic.SetSpline(spline);
            _followSplineTactic.SetSplineLinks(_pathLinkedUnits);
            container.Inject(_followSplineTactic);
        }

        private void OnEnemySignatureApproached(UnitEntity entity, ISignatureData other)
        {
            if (entity.GetTactic() is SpsFollowTactic)
            {
                _unitsFrozenPoints.Add(entity.Id, spline.FreezePoint(_pathLinkedUnits[entity.Id]));
            }
            var t = new DirectAttackTactic(_tickService);
            t.Target = other;
            entity.SetTactic(t);
        }

        public void AddControllableUnit(UnitEntity unit)
        {
            _controllableUnits.Add(unit);
            unit.RegisterDisposeListener(this);
            if (_pathLinkedUnits.ContainsKey(unit.Id))
            {
                unit.SetTactic(_followSplineTactic);
            }
        }

        public void RemoveControllableUnit(UnitEntity unit)
        {
            _controllableUnits.Remove(unit);
        }

        public IReadOnlyList<UnitEntity> GetControllableUnits() => _controllableUnits;

        public void Link(int unitEntityId, int particleIndex)
        {
            _pathLinkedUnits[unitEntityId] = particleIndex; //doesn't need to clear data when remove entity
        }

        public void OnEntityDisposed(IWorldEntity entity)
        {
            RemoveControllableUnit(entity as UnitEntity);
        }
        
        public void Tick()
        {
            // 1. attack enemies in alarm radius
            // 2. react to incoming menaces

            for (int i = _followSplineTactic.ControlledEntitiesList.Count - 1; i >= 0; i--)
            {
                var unit = _followSplineTactic.ControlledEntitiesList[i];
                SignatureDataWarp? closestEnemy = unit.GetClosestEnemy(_tableRelations);
                if (closestEnemy != null)
                {
                    OnEnemySignatureApproached(unit, closestEnemy.Value.Data);
                }
            }

            foreach (var unit in _controllableUnits)
            {
                if (unit.Unit.Sensor.Menaces.Count > 0 && unit.GetTactic() is not MenaceReactionTactic && unit.Unit.Sensor.Menaces[0].Dot > 0.995f)
                {
                    if (unit.GetTactic() is SpsFollowTactic)
                    {
                        _unitsFrozenPoints.Add(unit.Id, spline.FreezePoint(_pathLinkedUnits[unit.Id]));
                    }
                    unit.SetTactic(new MenaceReactionTactic(OnUnitMenaceDisappeared, _tickService));
                }
            }
        }

        private void OnUnitMenaceDisappeared(UnitEntity unit)
        {
            if (_pathLinkedUnits.ContainsKey(unit.Id))
            {
                spline.UnfreezePoint(_unitsFrozenPoints[unit.Id]);
                _unitsFrozenPoints.Remove(unit.Id);
                unit.SetTactic(_followSplineTactic);
            }
        }
    }
}