
using System;
using UnityEngine;

namespace Core.Trading
{
    [Serializable]
    public class TradeItem : IDisposable
    {
        [SerializeField] private int amount;
        [SerializeField] private ItemSign sign;
        [SerializeField] private int cost;

        public int Cost => cost;
        public ItemSign Sign => sign;
        public int Amount => amount;
        public void Dispose()
        {
            sign = null;
        }
    }
}