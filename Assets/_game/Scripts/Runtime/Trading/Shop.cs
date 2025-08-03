using System;
using System.Collections.Generic;
using Core.Character;
using Core.Character.Interaction;
using Core.Structure.Rigging;
using Core.Trading;
using UnityEngine;

namespace Runtime.Trading
{
    [Serializable]
    public class Shop : Block, IInteractiveBlock, ITradeHandler
    {
        [SerializeField] private string shopId;

        public bool EnableInteraction => IsActive;
        public Transform Root => transform;
        private TradeItem[] _assortment;
        public IEnumerable<TradeItem> GetItems() => _assortment;
        public event Action ItemsChanged;

        public bool TryMakeDeal(TradeDeal deal, out Transaction transaction)
        {
            transaction = null;
            return true;
        }

        public bool RequestInteractive(ICharacterController character, out string data)
        {
            data = string.Empty;
            return true;
        }

        public void Interaction(ICharacterController character)
        {
            character.
        }
    }
}