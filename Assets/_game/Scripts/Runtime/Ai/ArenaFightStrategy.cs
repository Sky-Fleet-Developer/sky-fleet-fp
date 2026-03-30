using System;
using System.Collections.Generic;
using Core.Ai;
using Core.Misc;
using Core.World;
using Runtime.Misc;
using UnityEngine;
using Zenject;
using ITickable = Zenject.ITickable;

namespace Runtime.Ai
{
    public class ArenaFightStrategy : MonoBehaviour, ITickable, IAiPathStrategy, IWorldEntityDisposeListener
    {
        [SerializeField] private SpsViewRange spline;
        private List<UnitEntity> _controllableUnits = new ();
        private SpsFollowTactic _followSplineTactic;
        private Dictionary<int, int> _pathLinkedUnits = new ();
        [Inject] private TickService _tickService;
        
        private void OnDestroy()
        {
            _followSplineTactic.Dispose();
        }

        [Inject]
        private void Inject(DiContainer container)
        {
            _followSplineTactic = new SpsFollowTactic(_tickService);
            _followSplineTactic.SetSpline(spline);
            _followSplineTactic.SetSplineLinks(_pathLinkedUnits);
            _followSplineTactic.OnEnemySignatureApproached += OnEnemySignatureApproached;
            _followSplineTactic.AlarmRadius = 500;
            container.Inject(_followSplineTactic);
        }

        private void OnEnemySignatureApproached(UnitEntity entity, ISignatureData other)
        {
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
            
        }
    }
}