using System;
using Core.Ai;
using Core.Misc;
using Core.World;
using Runtime.Ai.Maneuvers;
using UnityEngine;

namespace Runtime.Ai
{
    public class MenaceReactionTactic : SingleUnitTacticBase
    {
        private Action<UnitEntity> _onMenaceDisappeared;
        private bool _shouldBeDisposed;
        public MenaceReactionTactic(Action<UnitEntity> onMenaceDisappeared, TickService tickService) : base(tickService)
        {
            _onMenaceDisappeared = onMenaceDisappeared;
        }

        public override void UnitEnterTactic(UnitEntity entity)
        {
            base.UnitEnterTactic(entity);
            //Debug.Log($"MenaceReactionTactic ({entity.Id}) menace by {entity.Unit.Sensor.Menaces[0].Menace.MyUnit.Id}. Dot: {entity.Unit.Sensor.Menaces[0].Dot}");
            entity.Unit.SetManeuvers(new MoveToDeadZoneManeuver());
        }

        public override bool CanChangeTo(Type newTacticType, UnitEntity entity)
        {
            if (newTacticType == typeof(MenaceReactionTactic))
            {
                return false;
            }
            return true;
        }

        public override void Tick()
        {
            if (_shouldBeDisposed)
            {
                Dispose();
                return;
            }
            if (!ControlledEntity.Unit.HasSignificantMenace())
            {
                //Debug.Log($"Menace disappeared ({ControlledEntity.Id})");
                _onMenaceDisappeared(ControlledEntity);
                _shouldBeDisposed = true;
                return;
            }
        }
    }
}