using System;
using System.Collections.Generic;
using Core.Character;
using Core.Character.Interaction;
using Core.Configurations;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Trading
{
    [Serializable]
    public class Shop : Block, IInteractiveObject, ITradeHandler
    {
        [SerializeField] private string shopId;

        public bool EnableInteraction => IsActive;
        public Transform Root => transform;
        private List<TradeItem> _assortment = new();
        public IEnumerable<TradeItem> GetItems() => _assortment;
        public event Action ItemsChanged;
        [Inject] private ShopTable _shopTable;
        [Inject] private ItemsTable _itemsTable;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            _assortment.Clear();
            if (_shopTable.TryGetSettings(shopId, out ShopSettings settings))
            {
                for (var i = 0; i < _itemsTable.Data.Length; i++)
                {
                    if (settings.IsItemMatch(_itemsTable.Data[i]))
                    {
                        _assortment.Add(new TradeItem(_itemsTable.Data[i], 3, settings.GetCost(_itemsTable.Data[i])));
                    }
                }
            }
            base.InitBlock(structure, parent);
        }

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
    }
}