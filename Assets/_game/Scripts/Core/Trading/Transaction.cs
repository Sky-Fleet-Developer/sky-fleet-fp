namespace Core.Trading
{
    public class Transaction
    {
        private TradeDeal _deal;

        public Transaction(TradeDeal deal)
        {
            _deal = deal;
        }
    }
}