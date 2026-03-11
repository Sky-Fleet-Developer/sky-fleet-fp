using Cysharp.Threading.Tasks;

namespace Core.Ai
{
    public interface IManeuver
    {
        public void Tick(IUnitControl control, Sensor sensor);
    }
}