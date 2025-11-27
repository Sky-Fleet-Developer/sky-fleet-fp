using Core.Character.Stuff;
using Core.Configurations;

namespace Core.Trading.Tests
{
    public class FakeInventoryFactory : IInventoryFactory
    {
        private const string CostumerId = "costumer";
        private readonly IItemsContainerMasterHandler _cells;
        private int _pointer;
        private BankSystem _bankSystem;

        public FakeInventoryFactory(BankSystem bankSystem)
        {
            _bankSystem = bankSystem;
            _cells = new SlotsGrid(CostumerId, new[]
            {
                new SlotCell("lot", new TagCombination[]
                {
                    new() { tags = new string[] { "all" } }
                }, new TagCombination[] { }, 1)
            });
        }
        
        public IItemsContainerMasterHandler CreateInventory(string key)
        {
            if (key == CostumerId)
            {
                return _cells;
            }

            return new Inventory(key, _bankSystem);
        }
    }
}