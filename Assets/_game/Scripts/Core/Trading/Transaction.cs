using System.Collections.Generic;

namespace Core.Trading
{
    public class Transaction
    {
        private readonly TradeDeal _deal;
        private readonly List<DeliveredProductInfo> _deliverInfo;
        
        public Transaction(TradeDeal deal, List<DeliveredProductInfo> deliverInfo)
        {
            _deal = deal;
            _deliverInfo = deliverInfo;
        }

        public async void FinilizeAsync()
        {
            for (var i = 0; i < _deliverInfo.Count; i++)
            {
                if (_deliverInfo[i].PrefabLoading != null)
                {
                    await _deliverInfo[i].PrefabLoading;
                }
            }
        }
    }
}