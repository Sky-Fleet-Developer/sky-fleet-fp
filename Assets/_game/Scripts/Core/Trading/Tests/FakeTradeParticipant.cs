namespace Core.Trading.Tests
{
    public class FakeTradeParticipant : ITradeParticipant
    {
        public FakeTradeParticipant(string key) => (InventoryKey, WalletKey) = (key, key);
        public string InventoryKey { get; }
        public string WalletKey { get; }
    }
}