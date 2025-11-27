using Core.Character.Stuff;
using Core.Configurations;
using Moq;
using NUnit.Framework;
using UnityEngine;
using Zenject;


namespace Core.Trading.Tests
{
    [TestFixture]
    public class BankSystemTest
    {
        [Test]
        public void Purchase()
        {
            var bank = CreateInstance();
            DiContainer container = new DiContainer();
            SetupDi(container, bank);
            container.Inject(bank);
            
            bank.InitializeShop("test", "test");
            var inv = bank.GetOrCreateInventory("test");
            Assert.IsNotNull(inv);
        }

        private void SetupDi(DiContainer container, BankSystem bankSystem)
        {
            container.Bind<IInventoryFactory>().FromInstance(new FakeInventoryFactory(bankSystem));
            container.Bind<IShopDataSource>().FromInstance(new FakeShopTable());
        }

        private BankSystem CreateInstance()
        {
            return ScriptableObject.CreateInstance<BankSystem>();
        }
    }
}