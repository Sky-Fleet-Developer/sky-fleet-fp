namespace Core.Structure.Rigging
{
    public interface IFuelPowerGenerator : IFuelUser, IPowerUser
    {
        float MaximalOutput { get; }
        float FuelConsumption { get; }
        float MaxFuelConsumption { get; }
        float CurrentFuelConsumption { get; }
        float CurrentPowerUsage { get; }
    }
}