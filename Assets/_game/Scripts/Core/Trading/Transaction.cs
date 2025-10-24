using System.Collections.Generic;

namespace Core.Trading
{
    public class Transaction
    {
        private readonly TradeDeal _deal;
        
        public Transaction(TradeDeal deal)
        {
            _deal = deal;
        }
    }
}