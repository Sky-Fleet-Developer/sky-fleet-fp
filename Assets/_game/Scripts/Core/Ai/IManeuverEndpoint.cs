namespace Core.Ai
{
    public interface IManeuverEndpoint
    {
        public bool IsComplete(IManeuver maneuver, IUnitControl control, Sensor sensor);
    }
}