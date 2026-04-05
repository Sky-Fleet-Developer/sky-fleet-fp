using System;
using Core.Ai;
using Core.Misc;
using Core.World;

namespace Runtime.Ai
{
    public class MenaceReactionTactic : SingleUnitTacticBase
    {

        public MenaceReactionTactic(Action<UnitEntity> onMenaceDisappeared, TickService tickService) : base(tickService)
        {
        }

        public override void Tick()
        {
            
        }
    }
}