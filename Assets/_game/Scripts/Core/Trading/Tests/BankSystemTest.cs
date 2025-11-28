using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Character.Interaction;
using Core.Configurations;
using Core.Items;
using NUnit.Framework;
using UnityEngine;


namespace Core.Trading.Tests
{
    [TestFixture]
    public class BankSystemTestsBase : TradingTestsBase
    {
        [Test][Order(0)]
        public async Task ShopCreation()
        {
            await LoadAndRunTestScene(true, false, false);

            var inventory = CreateShop(out _);
            
            Assert.IsNotNull(inventory);
            Assert.IsFalse(inventory.IsEmpty);
        }
        
        
        [Test][Order(1)]
        [TestCase(5000, new []{BagItemId}, new []{1}, new []{true})] // bag only
        [TestCase(10, new []{BagItemId}, new []{1}, new []{false})] // bag no money
        [TestCase(5000, new []{BagItemId}, new []{2}, new []{true})] // two bags
        [TestCase(5000, new []{BagItemId, IronIngotItemId}, new []{1, 10}, new []{true, true})] // bag and other items
        [TestCase(5000, new []{BagItemId, IronIngotItemId}, new []{1, 90}, new []{true, false})] // bag and items over limit
        public async Task Buy_SomeItems(int initialBalance, string[] items, int[] amounts, bool[] expectedAmountsMatches)
        {
            await LoadAndRunTestScene(true, false, false);

            CreateShop(out _);
            var bank = MyContext.MyContainer.Resolve<BankSystem>();
            bank.TestCreateWallet(TestCostumer, initialBalance);
            var deal = CreateTradeDeal(bank, TradeKind.Sell);

            var itemsAdaptor = CreateTradeAdaptor(bank, bank.GetPullPutWarp(TestShop), TradeKind.Sell);
            for (int i = 0; i < items.Length; i++)
            {
                AddItemToPurchase(items[i], amounts[i], itemsAdaptor, deal);
            }

            try
            {
                bank.TryMakeDeal(deal);
                var costumerInventory = bank.GetOrCreateInventory(TestCostumer);

                for (int i = 0; i < items.Length; i++)
                {
                    var matchItems = costumerInventory.GetItems().Where(x => x.Sign.Id == items[i]).ToArray();
                    float amount = matchItems.Sum(v => v.Amount);
                    Assert.AreEqual(expectedAmountsMatches[i], Mathf.Approximately(amount, amounts[i]));
                }
            }
            finally
            {
                itemsAdaptor.Dispose();
                deal.Dispose();
                bank.TestDeleteInventory(TestCostumer);
                bank.TestDeleteInventory(TestShop);
                bank.TestDeleteWallet(TestCostumer);
            }
        }
        
        [Test][Order(1)]
        [TestCase(new []{BagItemId}, new []{1})] // bag only
        [TestCase(new []{BagItemId, IronIngotItemId}, new []{1, 10})] // bag and other items
        [TestCase(new []{BagItemId, IronIngotItemId}, new []{1, 100})] // bag and a lot of other items
        public async Task BuyBag_BuySell_SomeItems(string[] items, int[] amounts) //TODO: Detect and validate callbacks
        {
            await LoadAndRunTestScene(true, false, false);

            CreateShop(out _);
            var bank = MyContext.MyContainer.Resolve<BankSystem>();
            bank.TestCreateWallet(TestCostumer, 5000);
            var deal = CreateTradeDeal(bank, TradeKind.Sell);

            var itemsAdaptor = CreateTradeAdaptor(bank, bank.GetPullPutWarp(TestShop), TradeKind.Sell);
            MyContext.MyContainer.Inject(itemsAdaptor);
            itemsAdaptor.Initialize();
            for (int i = 0; i < items.Length; i++)
            {
                AddItemToPurchase(items[i], amounts[i], itemsAdaptor, deal);
            }
            
            var shopInventory = bank.GetOrCreateInventory(TestShop);
            Dictionary<ItemInstance, float> initialItemAmounts = shopInventory.GetItems().ToDictionary(x => x, x => x.Amount);
            
            try
            {
                bank.TryMakeDeal(deal);
                deal.Dispose();
                deal = CreateTradeDeal(bank, TradeKind.Buyout);
                
                float sold = -1;
                while (sold != 0)
                {
                    itemsAdaptor.Dispose();
                    itemsAdaptor = CreateTradeAdaptor(bank, bank.GetPullPutWarp(TestCostumer), TradeKind.Buyout);
                    sold = 0;
                    for (int i = 0; i < items.Length; i++)
                    {
                        sold += AddItemToPurchase(items[i], -1, itemsAdaptor, deal);
                    }

                    bank.TryMakeDeal(deal);
                }

                var costumerInventory = bank.GetOrCreateInventory(TestCostumer);

                for (int i = 0; i < items.Length; i++)
                {
                    Assert.AreEqual(0, costumerInventory.GetItems().Count(x => x.Sign.Id == items[i]));
                }
                
                foreach (var shopItem in shopInventory.GetItems())
                {
                    Assert.AreEqual(initialItemAmounts[shopItem], shopItem.Amount, Mathf.Epsilon);
                }
            }
            finally
            {
                itemsAdaptor.Dispose();
                deal.Dispose();
                bank.TestDeleteInventory(TestCostumer);
                bank.TestDeleteInventory(TestShop);
                bank.TestDeleteWallet(TestCostumer);
            }
        }
    }
}