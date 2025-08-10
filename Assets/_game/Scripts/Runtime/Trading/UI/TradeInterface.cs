using System.Collections;
using Core.Character;
using Core.Character.Interaction;
using Core.Character.Interface;
using Core.Patterns.State;
using Core.Trading;
using Core.UiStructure;
using Core.UIStructure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Trading.UI
{
    public class TradeInterface : Service, IFirstPersonInterface, ISelectionListener<TradeItemView>
    {
        [SerializeField] private TradeItemsListView sellerItemsView;
        [SerializeField] private TradeItemsListView cartItemsView;
        [SerializeField] private TradeItemDescriptionView descriptionView;
        [SerializeField] private Button addToCartButton;
        [SerializeField] private Button removeFromCartButton;
        [SerializeField] private Button acceptButton;
        [SerializeField] private TextMeshProUGUI dealCostText;
        private ITradeHandler _handler;
        private FirstPersonController.UIInteractionState _interactionState;
        private FirstPersonInterfaceInstaller _master;
        private TradeDeal _deal;

        protected override void Awake()
        {
            base.Awake();
            sellerItemsView.SelectionHandler.AddListener(this);
            cartItemsView.SelectionHandler.AddListener(this);
            addToCartButton.onClick.AddListener(AddToCartClick);
            removeFromCartButton.onClick.AddListener(RemoveFromCartClick);
            acceptButton.onClick.AddListener(AcceptClick);
        }

        public void Init(FirstPersonInterfaceInstaller master)
        {
            _master = master;
            _interactionState = ((FirstPersonController.UIInteractionState)_master.TargetState);
            _handler = (ITradeHandler)_interactionState.Handler;
            _deal = new TradeDeal(_interactionState.Master.GetInventory(), _handler.Inventory);
        }
        
        private void AddToCartClick()
        {
            if (sellerItemsView.SelectionHandler.Selected)
            {
                int amountToAdd = 1;
                if (_deal.TryAddToCart(sellerItemsView.SelectionHandler.Selected.Data, amountToAdd, sellerItemsView.SelectionHandler.Selected.Data.amount, out var innerItem))
                {
                    cartItemsView.AddItem(innerItem);
                    if (innerItem.amount == amountToAdd)
                    {
                        cartItemsView.Select(innerItem);
                    }
                    RefreshCostView();
                }
            }
        }
        
        private void RemoveFromCartClick()
        {
            if (cartItemsView.SelectionHandler.Selected)
            {
                _deal.RemoveFromCart(cartItemsView.SelectionHandler.Selected.Data, 1, out bool isItemCompletelyRemoved);
                if (isItemCompletelyRemoved)
                {
                    cartItemsView.RemoveItem(cartItemsView.SelectionHandler.Selected.Data);
                }
                else
                {
                    cartItemsView.RefreshItem(cartItemsView.SelectionHandler.Selected.Data);
                }
                RefreshCostView();
            }
        }

        private void RefreshCostView()
        {
            int paymentAmount =  _deal.GetPaymentAmount();
            if (paymentAmount == 0)
            {
                dealCostText.text = "0";
            }
            else if (paymentAmount > 0)
            {
                dealCostText.text = $"<color=yellow>-{paymentAmount:C}</color>";
            }
            else
            {
                dealCostText.text = $"<color=green>+{-paymentAmount:C}</color>";
            }
        }

        private void AcceptClick()
        {
            if (_handler.TryMakeDeal(_deal, out Transaction transaction))
            {
                _deal = new TradeDeal(_interactionState.Master.GetInventory(), _handler.Inventory);
                dealCostText.text = "0";
                cartItemsView.Clear();
                sellerItemsView.SetItems(_handler.Inventory.GetItems());
            }
        }

        public bool IsMatch(IState state)
        {
            return state is FirstPersonController.UIInteractionState { Handler: ITradeHandler };
        }

        void IFirstPersonInterface.Show()
        {
            Window.Open();
            sellerItemsView.SetItems(_handler.Inventory.GetItems());
        }

        void IFirstPersonInterface.Hide()
        {
            Window.Close();
        }

        public override IEnumerator Hide(BlockSequenceSettings settings = null)
        {
            _interactionState.LeaveState();
            return base.Hide(settings);
        }

        public void OnSelectionChanged(TradeItemView prev, TradeItemView next)
        {
            if (next)
            {
                descriptionView.SetData(next.Data.sign);
                //bool isSellerItem = next.transform.IsChildOf(sellerItemsView.transform);
            }
            else
            {
                descriptionView.Clear();
            }
        }
    }
}