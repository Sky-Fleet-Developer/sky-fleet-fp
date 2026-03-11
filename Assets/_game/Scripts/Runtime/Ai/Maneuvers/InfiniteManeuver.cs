using Core.Ai;

namespace Runtime.Ai.Maneuvers
{
    public class InfiniteManeuver : IManeuverEndpoint
    {
        public static InfiniteManeuver Instance { get; } = new InfiniteManeuver();
        public bool IsComplete(IManeuver maneuver, IUnitControl control, Sensor sensor)
        {
            return false;
        }
    }
}