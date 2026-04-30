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
        public int EntityId => Entity.Id;
        public UnitEntity Entity { get; }
        public void InjectEntity(UnitEntity entity);
        public Sensor Sensor { get; }
        public UnitTechCharacteristic GetTechCharacteristic();
    }

    public static class UnitExtensions
    {
        public static bool HasSignificantMenace(this IUnit unit)
        {
            return unit.Sensor.Menaces.Count > 0 && unit.Sensor.Menaces[0].Dot > 0.995f;
        }
        public static bool IsMenaceSignificant(this IUnit unit, int menaceIndex)
        {
            return unit.Sensor.Menaces[menaceIndex].Dot > 0.995f;
        }
        public static bool IsMenaceSignificant(this IUnit unit, MenaceRef menace)
        {
            return menace.Dot > 0.995f;
        }
    }
}