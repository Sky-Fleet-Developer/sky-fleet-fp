using System.Collections.Generic;
using Core.Ai;
using Core.World;

namespace Core.Ai
{
    public interface IUnit
    {
        public void SetTactic(IUnitTactic tactic);
        public void SetManeuvers(params IManeuver[] maneuvers);
        public bool IsManeuversComplete { get; }
        public int EntityId { get; }
        public void InjectEntity(UnitEntity entity);
        public Sensor Sensor { get; }
        public UnitTechCharacteristic GetTechCharacteristic();
    }
}