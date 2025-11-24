using Core.Items;
using Core.Trading;
using Zenject;

namespace Runtime.Items
{
    public class ItemInstanceFactory : IItemInstanceFactory
    {
        [Inject] private BankSystem _bankSystem;
        
        public ItemInstance Create(ItemSign sign, float amount)
        {
            var instance = new ItemInstance(sign, amount, _bankSystem.BindInventoryToContainerSettings, _bankSystem.UnbindInventoryToContainerSettings);
            return instance;
        }
    }
}