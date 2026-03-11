namespace Core.Ai
{
    public interface IUnitTactic
    {
        public void ControlUnit(IUnit unit, Sensor sensor);
    }

    public class EmptyTactic : IUnitTactic
    {
        public void ControlUnit(IUnit unit, Sensor sensor) { }
    }
}