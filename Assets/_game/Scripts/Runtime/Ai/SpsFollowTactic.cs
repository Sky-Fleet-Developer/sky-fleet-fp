using System;
using System.Collections.Generic;
using Core.Ai;
using Core.Misc;
using Core.World;
using Runtime.Ai.Maneuvers;
using Runtime.Misc;
using UnityEngine;
using Zenject;

namespace Runtime.Ai
{
    public class SpsFollowTactic : MultipleUnitTacticBase
    {
        [Inject] private TableRelations _tableRelations;
        private SpsViewRange _spline;
        private Dictionary<int, int> _splineLinks;

        public SpsFollowTactic(TickService tickService) : base(tickService)
        {
        }


        public void SetSpline(SpsViewRange spline)
        {
            _spline = spline;
        }

        public void SetSplineLinks(Dictionary<int, int> splineLinks)
        {
            _splineLinks = splineLinks;
        }

        public override void UnitEnterTactic(UnitEntity entity)
        {
            base.UnitEnterTactic(entity);
            entity.Unit.SetManeuvers(new Follow(new SpsPointAsTarget(_splineLinks[entity.Unit.EntityId], _spline.Spline), Vector3.zero));
        }

        public override void Tick()
        {
            for (var i = ControlledEntities.Count - 1; i >= 0; i--)
            {
                ControlUnit(ControlledEntities[i], ControlledEntities[i].Unit.Sensor);
            }
        }

        private void ControlUnit(UnitEntity entity, Sensor sensor)
        {

        }
    }
}