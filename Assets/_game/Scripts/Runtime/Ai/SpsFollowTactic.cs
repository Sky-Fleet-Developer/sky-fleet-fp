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

        public int RefreshNearSignaturesRate => 5;
        public event Action<UnitEntity, ISignatureData> OnEnemySignatureApproached;
        private Dictionary<int, HashSet<ISignatureData>> _nearSignaturesCache = new();

        public float AlarmRadius
        {
            get => Mathf.Sqrt(_alarmRadiusSqr);
            set => _alarmRadiusSqr = value * value;
        }
        private float _alarmRadiusSqr;

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
            _nearSignaturesCache.Add(entity.Id, new HashSet<ISignatureData>());
            entity.Unit.SetManeuvers(new Follow(new SpsPointAsTarget(_splineLinks[entity.Unit.EntityId], _spline.Spline), Vector3.zero));
        }

        public override void UnitExitTactic(UnitEntity entity)
        {
            base.UnitExitTactic(entity);
            _nearSignaturesCache.Remove(entity.Id);
        }

        public override void Tick()
        {
            for (var i = 0; i < ControlledEntities.Count; i++)
            {
                ControlUnit(ControlledEntities[i], ControlledEntities[i].Unit.Sensor);
            }
        }

        private void ControlUnit(UnitEntity entity, Sensor sensor)
        {
            foreach (var signature in sensor.NearSignatures)
            {
                if (_nearSignaturesCache[entity.Id].Contains(signature))
                {
                    continue;
                }
                if (Vector3.SqrMagnitude(signature.Position - sensor.Position) < _alarmRadiusSqr)
                {
                    _nearSignaturesCache[entity.Id].Add(signature);
                    if (_tableRelations.GetRelation(entity.SignatureId, signature.SignatureId) < RelationType.Neutral)
                    {
                        OnEnemySignatureApproached?.Invoke(entity, signature);
                    }
                }
            }
        }
    }
}