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
        public Inventory Inventory => _inventory;
        public event Action ItemsChanged;
        [Inject] private ShopTable _shopTable;
        [Inject] private ItemsTable _itemsTable;
        private Inventory _inventory;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            List<TradeItem> assortment = new List<TradeItem>();
            if (_shopTable.TryGetSettings(shopId, out ShopSettings settings))
            {
                for (var i = 0; i < _itemsTable.Items.Length; i++)
                {
                    if (settings.IsItemMatch(_itemsTable.Items[i]))
                    {
                        assortment.Add(new TradeItem(_itemsTable.Items[i], 3, settings.GetCost(_itemsTable.Items[i])));
                    }
                }
            }

            _inventory = new Inventory(assortment);
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