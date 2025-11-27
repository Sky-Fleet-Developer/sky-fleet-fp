using Core.Configurations;
using FakeItEasy;
using FakeItEasy.Creation;

namespace Core.Trading.Tests
{
    public class FakeShopTable : IShopDataSource
    {
        public bool TryGetSettings(string id, out ShopSettings settings)
        {
            settings = new ShopSettings(id);
            return true;
        }
    }
}