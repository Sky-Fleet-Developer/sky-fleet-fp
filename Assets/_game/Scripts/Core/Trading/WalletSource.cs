using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Trading
{
    public partial class BankSystem
    {
        [Serializable]
        private class WalletSource
        {
            [Serializable]
            private class WalletData
            {
                public string id;
                public int balance;
            }
            [SerializeField] private List<WalletData> data = new();
            
            public Wallet LoadWallet(string id)
            {
                foreach (var walletData in data)
                {
                    if (walletData.id == id)
                    {
                        return new Wallet(walletData.id, walletData.balance);
                    }
                }
                return new Wallet(id, 0);
            }

            public void SaveWallet(Wallet wallet)
            {
                foreach (var walletData in data)
                {
                    if (walletData.id == wallet.WalletKey)
                    {
                        walletData.balance = wallet.GetBalance();
                        return;
                    }
                }
                data.Add(new WalletData {id = wallet.WalletKey, balance = wallet.GetBalance()});
            }
        }
    }
}