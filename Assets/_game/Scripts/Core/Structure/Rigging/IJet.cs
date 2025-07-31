namespace Core.Structure.Rigging
{
    public interface IJet : IFuelUser, IForceUser
    {
        float MaximalThrust { get; }
        float CurrentThrust { get; }
    }
}