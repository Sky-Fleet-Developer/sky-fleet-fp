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
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

namespace Runtime.Trading.UI
{
    public class TradeInterface : FirstPersonService, ISelectionListener<TradeItemView>
    {
        [SerializeField] private TradeItemsListView sellerItemsView;
        [SerializeField] private ItemSignDescriptionView signDescriptionView;
        [SerializeField] private Button acceptButton;
        [SerializeField] private TextMeshProUGUI dealCostText;
        private ITradeHandler _handler;
        private FirstPersonController.UIInteractionState _interactionState;
        private FirstPersonInterfaceInstaller _master;
        private TradeDeal _deal;
        [Inject] private BankSystem _bankSystem;

        protected override void Awake()
        {
            base.Awake();
            sellerItemsView.SelectionHandler.AddListener(this);
            acceptButton.onClick.AddListener(AcceptClick);
            sellerItemsView.OnItemInCardAmountChanged += OnSellerItemInCardAmountChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            acceptButton.onClick.RemoveListener(AcceptClick);
            _handler?.RemoveListener(sellerItemsView);
            sellerItemsView.OnItemInCardAmountChanged -= OnSellerItemInCardAmountChanged;
        }

        private void OnSellerItemInCardAmountChanged(TradeItem item, float amount)
        {
            if (_deal.SetInCartItemAmount(item, amount, out var innerItem))
            {
                RefreshCostView();
            }
        }

        public override void Init(FirstPersonInterfaceInstaller master)
        {
            _master = master;
            _interactionState = ((FirstPersonController.UIInteractionState)_master.TargetState);
            _handler = (ITradeHandler)_interactionState.Handler;
            _handler.AddListener(sellerItemsView);
            _deal = new TradeDeal(_interactionState.Master, _handler);
        }
        
        /*private void AddToCartClick()
        {
            if (sellerItemsView.SelectionHandler.Selected)
            {
                int amountToAdd = 1;
                if (_deal.TryAddToCart(sellerItemsView.SelectionHandler.Selected.Data, amountToAdd, sellerItemsView.SelectionHandler.Selected.Data.amount, out var innerItem))
                {
                    sellerItemsView.SelectionHandler.Selected.SetInCardAmount(innerItem.amount);
                    RefreshCostView();
                }
            }
        }*/
        
        /*private void RemoveFromCartClick()
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
        }*/

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
                _deal = new TradeDeal(_interactionState.Master, _handler);
                dealCostText.text = "0";
                sellerItemsView.SetItems(_handler.GetTradeItems());
            }
        }

        public override bool IsMatch(IState state)
        {
            return state is FirstPersonController.UIInteractionState { Handler: ITradeHandler };
        }

        public override void Show()
        {
            base.Show();
            sellerItemsView.SetItems(_handler.GetTradeItems());
        }

        public override IEnumerator Hide(BlockSequenceSettings settings = null)
        {
            _interactionState.LeaveState();
            _handler?.RemoveListener(sellerItemsView);
            return base.Hide(settings);
        }

        public void OnSelectionChanged(TradeItemView prev, TradeItemView next)
        {
            if (next)
            {
                signDescriptionView.SetData(next.Data.Sign);
                //bool isSellerItem = next.transform.IsChildOf(sellerItemsView.transform);
            }
            else
            {
                signDescriptionView.Clear();
            }
        }
    }
}