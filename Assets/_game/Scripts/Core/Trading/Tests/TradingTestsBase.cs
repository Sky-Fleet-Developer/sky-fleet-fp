using Core.Character.Interaction;
using Core.Configurations;
using Core.Tests;
using UnityEngine;

namespace Core.Trading.Tests
{
    public class TradingTestsBase : RuntimeTestLauncher
    {
        public const string TestShop = "test_shop";
        public const string TestCostumer = "test_costumer_c-slots";
        public const string BagItemId = "small_bag";
        public const string IronIngotItemId = "iron_ingot";

        public ItemInstanceToTradeAdapter CreateTradeAdaptor(BankSystem bank, IItemInstancesSource itemsSource, TradeKind tradeKind)
        {
            var delivery = new FakeDeliveryService(bank);
            ItemInstanceToTradeAdapter itemsAdaptor = new ItemInstanceToTradeAdapter(TestShop, itemsSource, tradeKind);
            MyContext.MyContainer.Inject(itemsAdaptor);
            itemsAdaptor.Initialize();
            foreach (var tradeItem in itemsAdaptor.GetTradeItems())
            {
                tradeItem.SetDeliveryService(delivery);
            }

            return itemsAdaptor;
        }

        public TradeDeal CreateTradeDeal(BankSystem bankSystem, TradeKind tradeKind)
        {
            var costumerParticipant = new FakeTradeParticipant(TestCostumer);
            var shopParticipant = new FakeTradeParticipant(TestShop);
            TradeDeal deal = new TradeDeal(tradeKind == TradeKind.Sell ? costumerParticipant : shopParticipant, tradeKind == TradeKind.Sell ? shopParticipant : costumerParticipant, bankSystem);
            return deal;
        }

        public float AddItemToPurchase(string itemId, float amount, ItemInstanceToTradeAdapter itemsAdaptor, TradeDeal deal)
        {
            float addedAmount = 0;
            foreach (var tradeItem in itemsAdaptor.GetTradeItems())
            {
                if (tradeItem.Sign.Id == itemId)
                {
                    if (Mathf.Approximately(amount, -1))
                    {
                        amount = tradeItem.amount.Value;
                    }

                    if (deal.SetPurchaseItemAmount(tradeItem, amount))
                    {
                        addedAmount += amount;
                    }
                    break;
                }
            }
            return addedAmount;
        }

        public IItemsContainerReadonly CreateShop(out ShopSettings shopSettings)
        {
            var bank = MyContext.MyContainer.Resolve<BankSystem>();
            bank.InitializeShop(TestShop, TestShop);
            MyContext.MyContainer.Resolve<IShopDataSource>().TryGetSettings(TestShop, out shopSettings);

            return bank.GetOrCreateInventory(TestShop);
        }
    }
}