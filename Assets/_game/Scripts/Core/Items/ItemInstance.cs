namespace Core.Items
{
    public class ItemInstance
    {
        private readonly ItemSign _sign;
        private float _amount;
        
        public ItemSign Sign => _sign;
        public float Amount => _amount;
        public ItemInstance(){}
        public ItemInstance(ItemSign sign, float amount)
        {
            _amount = amount;
            _sign = sign;
        }
        
    }
}