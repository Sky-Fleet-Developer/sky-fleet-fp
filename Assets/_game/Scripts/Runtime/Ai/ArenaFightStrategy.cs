using System;
using System.Collections.Generic;
using Core.Ai;
using Core.World;
using Runtime.Misc;
using UnityEngine;

namespace Runtime.Ai
{
    public class ArenaFightStrategy : MonoBehaviour, IAiPathStrategy, IWorldEntityDisposeListener
    {
        [SerializeField] private SpsViewRange spline;
        private List<UnitEntity> _controllableUnits = new ();
        private SpsFollowTactic _followSplineTactic;
        private Dictionary<int, int> _pathLinkedUnits = new ();

        private void Awake()
        {
            _followSplineTactic = new SpsFollowTactic();
            _followSplineTactic.SetSpline(spline);
            _followSplineTactic.SetSplineLinks(_pathLinkedUnits);
        }

        public void AddControllableUnit(UnitEntity unit)
        {
            _controllableUnits.Add(unit);
            unit.RegisterDisposeListener(this);
            unit.SetTactic(_followSplineTactic);
        }

        public void RemoveControllableUnit(UnitEntity unit)
        {
            _controllableUnits.Remove(unit);
        }

        public void Link(int unitEntityId, int particleIndex)
        {
            _pathLinkedUnits[unitEntityId] = particleIndex; //doesn't need to clear data when remove entity
        }

        public void OnEntityDisposed(IWorldEntity entity)
        {
            RemoveControllableUnit(entity as UnitEntity);
        }
    }
}