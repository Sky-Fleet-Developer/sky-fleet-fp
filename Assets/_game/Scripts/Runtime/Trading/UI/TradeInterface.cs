using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public class TradeInterface : FirstPersonService, IMultipleSelectionListener<TradeItemView>
    {
        [SerializeField] private TradeItemsListView sellerItemsView;
        [SerializeField] private TradeItemsListView myItemsView;
        [SerializeField] private ItemSignDescriptionView signDescriptionView;
        [SerializeField] private Button acceptButton;
        [SerializeField] private CurrencyView dealCostView;
        [SerializeField] private CurrencyView costumerCurrencyView;
        [SerializeField] private Color purchaseColor;
        [SerializeField] private Color sellColor;
        [SerializeField] private Color notEnoughMoneyColor;
        private ITradeHandler _handler;
        private FirstPersonController.UIInteractionState _interactionState;
        private FirstPersonInterfaceInstaller _master;
        private TradeDeal _purchase;
        private TradeDeal _sell;
        [Inject] private BankSystem _bankSystem;
        [Inject] private DiContainer _container;
        private TradeItemView _selectedTarget;
        private IItemsContainerReadonly _myInventory;
        private ItemInstanceToTradeAdapter _myInventoryAdapter;
        private ITradeItemsSource _cargoZoneItemsSource;
        private ProductDeliverySettings _sellerDeliverySettings;
        private ProductDeliverySettings _purchaserDeliverySettings;

        protected override void Awake()
        {
            base.Awake();
            sellerItemsView.SelectionHandler.AddListener(this);
            myItemsView.SelectionHandler.AddListener(this);
            acceptButton.onClick.AddListener(AcceptClick);
            sellerItemsView.OnItemInCartAmountChanged += OnSellerItemInCartAmountChanged;
            sellerItemsView.OnDeliveryOptionChanged += OnSellerDeliveryOptionChanged;
            myItemsView.OnItemInCartAmountChanged += OnPurchaserItemInCartAmountChanged;
            myItemsView.OnDeliveryOptionChanged += OnPurchaserDeliveryOptionChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            acceptButton.onClick.RemoveListener(AcceptClick);
            _handler?.RemoveListener(sellerItemsView);
            sellerItemsView.OnItemInCartAmountChanged -= OnSellerItemInCartAmountChanged;
            sellerItemsView.OnDeliveryOptionChanged -= OnSellerDeliveryOptionChanged;
            myItemsView.OnItemInCartAmountChanged -= OnPurchaserItemInCartAmountChanged;
            myItemsView.OnDeliveryOptionChanged -= OnPurchaserDeliveryOptionChanged;
        }

        private void OnSellerDeliveryOptionChanged(TradeItem item, int option)
        {
            SetDeliveryOption(item, option, _sellerDeliverySettings);
            _purchase.UpdateDeliveryService(item);
        }
        
        private void OnPurchaserDeliveryOptionChanged(TradeItem item, int option)
        {
            SetDeliveryOption(item, option, _purchaserDeliverySettings);
            _sell.UpdateDeliveryService(item);
        }

        private void SetDeliveryOption(TradeItem item, int option, ProductDeliverySettings settings)
        {
            int counter = 0;
            foreach (var service in settings.Services)
            {
                if (service.IsCanDeliver(item.Sign, settings.Destination))
                {
                    if (counter == option)
                    {
                        item.SetDeliveryService(service);
                        break;
                    }
                    counter++;
                }
            }
        }
        
        private void OnSellerItemInCartAmountChanged(TradeItem item, float amount)
        {
            if (_purchase.SetPurchaseItemAmount(item, amount))
            {
                sellerItemsView.SetInCartAmount(item, amount);
                RefreshCostView();
            }
        }
        private void OnPurchaserItemInCartAmountChanged(TradeItem item, float amount)
        {
            if (item.Item.TryGetContainerKey(out string key) && !_bankSystem.GetOrCreateInventory(key).IsEmpty)
            {
                return;
            }
            if (_sell.SetPurchaseItemAmount(item, amount))
            {
                myItemsView.SetInCartAmount(item, amount);
                RefreshCostView();
            }
        }

        public override void Init(FirstPersonInterfaceInstaller master)
        {
            _master = master;
            _interactionState = ((FirstPersonController.UIInteractionState)_master.TargetState);
            _handler = (ITradeHandler)_interactionState.Handler;
            _handler.AddListener(sellerItemsView);
            _purchase = new TradeDeal(_interactionState.Master, _handler);
            _sell = new TradeDeal(_handler, _interactionState.Master);
            _container.Inject(_purchase);
            _container.Inject(_sell);
            _sellerDeliverySettings = new ProductDeliverySettings(_interactionState.Master, _handler.GetDeliveryServices());
            sellerItemsView.SetDeliverySettings(_sellerDeliverySettings);
            var deliveryService = new PutToInventoryDeliveryService();
            _container.Inject(deliveryService);
            _purchaserDeliverySettings = new ProductDeliverySettings(_handler, new[] {deliveryService});
            myItemsView.SetDeliverySettings(_purchaserDeliverySettings);

            _myInventoryAdapter?.Dispose();
            _myInventoryAdapter = _handler.GetAdapterToCustomerItems(_interactionState.Master);
            _cargoZoneItemsSource = _handler.GetCargoZoneItemsSource();

            myItemsView.SetItems(_myInventoryAdapter.GetTradeItems().Concat(_cargoZoneItemsSource.GetTradeItems()));
            _cargoZoneItemsSource.AddListener(myItemsView);
            _myInventoryAdapter.AddListener(myItemsView);
            RefreshCostView();
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
            var balance = _bankSystem.GetWalletBalance(_interactionState.Master);
            costumerCurrencyView.SetCurrency(balance);
            int deltaCurrency = _sell.GetPaymentAmount() - _purchase.GetPaymentAmount();
            costumerCurrencyView.SetColor(balance > -deltaCurrency ? Color.white : notEnoughMoneyColor);
            if (deltaCurrency == 0)
            {
                dealCostView.SetPrefix(string.Empty);
                dealCostView.SetColor(Color.white);
                dealCostView.SetCurrency(0);
            }
            else if (deltaCurrency > 0)
            {
                dealCostView.SetPrefix("+");
                dealCostView.SetCurrency(deltaCurrency);
                dealCostView.SetColor(sellColor);
            }
            else
            {
                dealCostView.SetPrefix("-");
                dealCostView.SetCurrency(-deltaCurrency);
                dealCostView.SetColor(purchaseColor);
            }
        }

        private void AcceptClick()
        {
            if (_bankSystem.TryMakeDeal(_sell))
            {
                _sell = new TradeDeal(_handler, _interactionState.Master);
                _container.Inject(_sell);
                myItemsView.SetItems(_myInventoryAdapter.GetTradeItems().Concat(_cargoZoneItemsSource.GetTradeItems()));
            }
            if (_bankSystem.TryMakeDeal(_purchase))
            {
                _purchase = new TradeDeal(_interactionState.Master, _handler);
                _container.Inject(_purchase);
                sellerItemsView.SetItems(_handler.GetTradeItems());
            }
            RefreshCostView();
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
            _myInventoryAdapter.RemoveListener(myItemsView);
            _cargoZoneItemsSource.RemoveListener(myItemsView);
            _interactionState.LeaveState();
            _handler?.RemoveListener(sellerItemsView);
            return base.Hide(settings);
        }
        
        public void OnSelected(TradeItemView target)
        {
        }

        public void OnDeselected(TradeItemView target)
        {
            if (_selectedTarget == target)
            {
                _selectedTarget = null;
                signDescriptionView.Clear();
            }
        }

        public void OnFinalSelected(TradeItemView target)
        {
            _selectedTarget = target;
            signDescriptionView.SetData(target.Data.Sign);
        }
    }
}