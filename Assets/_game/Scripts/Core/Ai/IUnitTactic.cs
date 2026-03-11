namespace Core.Ai
{
    public interface IUnitTactic
    {
        public void ControlUnit(IUnit unit, Sensor sensor);
    }
}