using System;

namespace Core.Trading
{
    public interface IWalletOwner
    {
        string WalletKey { get; }
    }
    [Serializable]
    public class Wallet
    {
        public string WalletKey { get; }
        private int _balance;

        public Wallet(string walletKey, int balance)
        {
            if (balance < 0) throw new ArgumentException("Balance cannot be negative");
            WalletKey = walletKey;
            _balance = balance;
        }

        public bool TryTakeCurrency(int amount)
        {
            if (_balance < amount) return false;
            _balance -= amount;
            return true;
        }

        public void PutCurrency(int amount)
        {
            _balance += amount;
        }

        public int GetBalance()
        {
            return _balance;
        }
    }
    
}