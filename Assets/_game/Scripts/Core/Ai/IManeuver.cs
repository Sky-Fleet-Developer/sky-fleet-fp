using Cysharp.Threading.Tasks;

namespace Core.Ai
{
    public interface IManeuver
    {
        public void Enter(IUnitControl control, Sensor sensor);
        public void Tick(IUnitControl control, Sensor sensor);
        public void Exit(IUnitControl control, Sensor sensor);
    }
}