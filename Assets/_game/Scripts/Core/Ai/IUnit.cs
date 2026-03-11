using System.Collections.Generic;
using Core.Ai;
using Core.World;

namespace Core.Ai
{
    public interface IUnit
    {
        public void SetTactic(IUnitTactic tactic);
        public void SetManeuvers((IManeuver, IManeuverEndpoint)[] maneuvers);
        public bool IsManeuversDone { get; }
        public int EntityId { get; }
        public void InjectEntity(UnitEntity entity);
    }
}