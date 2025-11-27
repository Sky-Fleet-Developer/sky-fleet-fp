namespace Core.Configurations
{
    public interface IShopDataSource
    {
        bool TryGetSettings(string id, out ShopSettings settings);
    }
}