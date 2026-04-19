using System;
using Core.Ai;
using Core.Misc;
using Core.World;
using UnityEngine;

namespace Runtime.Ai
{
    public class MenaceReactionTactic : SingleUnitTacticBase
    {
        private Action<UnitEntity> _onMenaceDisappeared;

        public MenaceReactionTactic(Action<UnitEntity> onMenaceDisappeared, TickService tickService) : base(tickService)
        {
            _onMenaceDisappeared = onMenaceDisappeared;
        }

        public override void Tick()
        {
            Debug.Log($"MenaceReactionTactic Tick {ControlledEntity.Unit}");
        }
    }
}