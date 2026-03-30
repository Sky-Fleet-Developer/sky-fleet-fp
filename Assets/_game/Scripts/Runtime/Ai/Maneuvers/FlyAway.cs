using Core.Ai;

namespace Runtime.Ai.Maneuvers
{
    public class FlyAway : IManeuver
    {
        private DirectionToTarget _directionToTarget;
        private ITargetData _target;

        public void InjectControls(IUnit unit, IUnitControl control, Sensor sensor)
        {
            
        }

        public void Enter()
        {
            
        }

        public bool Tick()
        {
            return false;
        }

        public void Exit()
        {
        }
    }
}