using System;
using System.Collections.Generic;
using Core.Items;
using UnityEngine;

namespace Core.Trading
{
    public class TradeDeal : IDisposable
    {
        private List<TradeItem> _itemsToPurchase;
        private IInventoryOwner _seller;
        private IInventoryOwner _purchaser;

        public IEnumerable<TradeItem> GetPurchases() => _itemsToPurchase;
        public IInventoryOwner GetPurchaser() => _purchaser;
        public IInventoryOwner GetSeller() => _seller;
        
        public TradeDeal(IInventoryOwner purchaser, IInventoryOwner seller)
        {
            _purchaser = purchaser;
            _seller = seller;
            _itemsToPurchase = new();
        }

        public bool SetPurchaseItemAmount(TradeItem item, float amount, out TradeItem innerItem)
        {
            return SetItemAmount(item, amount, out innerItem, _itemsToPurchase);
        }

        public bool SetItemAmount(TradeItem item, float amount, out TradeItem innerItem, List<TradeItem> itemsList)
        {
            if (amount > item.amount)
            {
                innerItem = null;
                return false;
            }

            if (amount == 0)
            {
                itemsList.Remove(item);
            }
            
            for (var i = 0; i < itemsList.Count; i++)
            {
                if (itemsList[i].Sign.Equals(item.Sign))
                {
                    itemsList[i].amount = amount;
                    innerItem = itemsList[i];
                    innerItem.SetDeliveryService(item.GetDeliveryService());
                    innerItem.SetSource(item.GetSource());
                    return true;
                }
            }

            var tradeItem = new TradeItem(item.Item, amount, item.Cost);
            tradeItem.SetDeliveryService(item.GetDeliveryService());
            tradeItem.SetSource(item.GetSource());
            itemsList.Add(tradeItem);
            innerItem = tradeItem;
            return true;
        }

        /*public void RemoveFromCart(TradeItem item, int amountToRemove, out bool isItemCompletelyRemoved)
        {
            isItemCompletelyRemoved = false;
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                if (_itemsToPurchase[i] == item)
                {
                    if (_itemsToPurchase[i].amount > amountToRemove)
                    {
                        _itemsToPurchase[i].amount -= amountToRemove;
                        isItemCompletelyRemoved = false;
                    }
                    else
                    {
                        _itemsToPurchase.RemoveAt(i);
                        isItemCompletelyRemoved = true;
                    }
                    break;
                }
            }
        }*/

        public int GetPaymentAmount()
        {
            int counter = 0;
            for (var i = 0; i < _itemsToPurchase.Count; i++)
            {
                counter += Mathf.CeilToInt(_itemsToPurchase[i].Cost * _itemsToPurchase[i].amount + 0.5f);
            }

            return counter;
        }


        public void Dispose()
        {
            _itemsToPurchase = null;
            _seller = null;
            _purchaser = null;
        }
    }
}