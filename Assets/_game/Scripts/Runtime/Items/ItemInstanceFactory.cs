using System;
using System.Linq;
using Core.Configurations;
using Core.Items;
using Core.Misc;
using Core.Trading;
using Zenject;

namespace Runtime.Items
{
    public class ItemInstanceFactory : IItemInstanceFactory
    {
        [Inject] private BankSystem _bankSystem;
        [Inject] private ItemsTable _itemsTable;
        
        public ItemInstance Create(ItemSign sign, float amount)
        {
            var instance = new ItemInstance(sign, amount, _bankSystem.BindInventoryToContainerSettings, _bankSystem.UnbindInventoryToContainerSettings);
            return instance;
        }

        /*public ItemInstance CreateUniq(ItemSign sign, float amount, string guid)
        {
            if (!sign.HasTag(ItemSign.IdentifiableTag))
            {
                throw new Exception($"Error when creating item instance: Trying setup uniqId to item without Identifiable tag. Wanted id: {guid}, item sign: {sign.Id}");
            }
            
            return new ItemInstance(sign, amount, guid, _bankSystem.BindInventoryToContainerSettings, _bankSystem.UnbindInventoryToContainerSettings);
        }*/

        public ItemInstance CreateByDescription(ItemDescription description)
        {
            var sign = _itemsTable.GetItem(description.signId);
            ItemInstance instance = new ItemInstance(sign, description, _bankSystem.BindInventoryToContainerSettings, _bankSystem.UnbindInventoryToContainerSettings);
            return instance;
        }
    }
}