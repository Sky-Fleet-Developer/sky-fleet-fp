namespace Core.Weapon
{
    public interface IDamageSource
    {
        public float Size { get; }
        public float Impulse { get; }
        public float Durability { get; }
    }
}