namespace Core.Structure.Rigging
{
    public interface IDamagableBlock : IBlock
    {
        float Durability { get; }
        ArmorData Armor { get; }
    }
}