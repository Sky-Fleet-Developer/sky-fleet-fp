using System;
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

namespace Runtime.Trading.Ui
{
    public class TradeInterface : Service, IFirstPersonInterface, ISelectionListener<TradeItemView>
    {
        [SerializeField] private TradeItemsListView sellerItemsView;
        [SerializeField] private TradeItemsListView purchaserItemsView;
        [SerializeField] private TradeItemDescriptionView descriptionView;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button acceptButton;
        [SerializeField] private TextMeshProUGUI dealCostText;
        private ITradeHandler _handler;
        private FirstPersonController.TradeState _targetState;
        private FirstPersonInterfaceInstaller _master;
        private TradeDeal _deal;
        
        protected override void Awake()
        {
            base.Awake();
            sellerItemsView.SelectionHandler.AddListener(this);
            actionButton.onClick.AddListener(ActionClick);
            acceptButton.onClick.AddListener(AcceptClick);
        }
        
        private void ActionClick()
        {
            if (sellerItemsView.SelectionHandler.Selected)
            {
                _deal.TryAddToCart(sellerItemsView.SelectionHandler.Selected.Data, 1, sellerItemsView.SelectionHandler.Selected.Data.amount);
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
                _deal = new TradeDeal(_targetState.Master.GetInventory(), _handler.Inventory);
                dealCostText.text = "0";
            }
        }

        public void Init(FirstPersonInterfaceInstaller master)
        {
            _master = master;
            _targetState = ((FirstPersonController.TradeState)_master.TargetState);
            _handler = _targetState.Handler;
            _deal = new TradeDeal(_targetState.Master.GetInventory(), _handler.Inventory);
        }

        public bool IsMatch(IState state)
        {
            return state is FirstPersonController.TradeState;
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
            _targetState.LeaveState();
            return base.Hide(settings);
        }

        public void OnSelectionChanged(TradeItemView prev, TradeItemView next)
        {
            if (next)
            {
                descriptionView.SetData(next.Data.sign);
            }
            else
            {
                descriptionView.Clear();
            }
        }
    }
}