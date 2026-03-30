using Cysharp.Threading.Tasks;

namespace Core.Ai
{
    public interface IManeuver
    {
        public void InjectControls(IUnit unit, IUnitControl control, Sensor sensor);
        public void Enter();
        public bool Tick();
        public void Exit();
    }
}