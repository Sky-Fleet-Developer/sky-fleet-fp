using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Core.Trading;
using Core.UIStructure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runtime.Trading.UI
{
    public class TradeItemView : ThingView<TradeItem>, IPointerClickHandler
    {
        [SerializeField] private ItemSignView signView;
        [SerializeField] private TextMeshProUGUI costLabel;
        [SerializeField] private TextMeshProUGUI amountLabel;
        [SerializeField] private Button addToCartButton;
        [SerializeField] private Transform selectAmountGroup;
        [SerializeField] private Button moreButton;
        [SerializeField] private Button lessButton;
        [SerializeField] private TMP_InputField inCartAmountInputField;
        [SerializeField] private Image selectionFrame;
        [SerializeField] private TMP_Dropdown deliverySelection;
        private TradeItem _data;
        private float _amount;
        private Action<TradeItem, float> _inCardAmountChangedCallback;
        private ProductDeliverySettings _deliverySettings;
        public override TradeItem Data => _data;
        private bool _isDataDirtyForDeliverySettings = true;

        private void Awake()
        {
            addToCartButton.onClick.AddListener(AddToCart);
            moreButton.onClick.AddListener(OnMoreClick);
            lessButton.onClick.AddListener(OnLessClick);
            inCartAmountInputField.onEndEdit.AddListener(OnAmountInputSubmit);
            deliverySelection.onValueChanged.AddListener(DeliverySelected);
        }

        private void DeliverySelected(int index)
        {
            int counter = 0;
            foreach (var service in _deliverySettings.Services)
            {
                if (service.IsCanDeliver(_data.Sign, _deliverySettings.Destination))
                {
                    if (counter == index)
                    {
                        _data.SetDeliveryService(service);
                        _inCardAmountChangedCallback.Invoke(_data, _amount);
                    }
                    counter++;
                }
            }
        }

        private void OnDestroy()
        {
            addToCartButton.onClick.RemoveListener(AddToCart);
            moreButton.onClick.RemoveListener(OnMoreClick);
            lessButton.onClick.RemoveListener(OnLessClick);
            inCartAmountInputField.onEndEdit.RemoveListener(OnAmountInputSubmit);
        }

        public void SetDeliverySettings(ProductDeliverySettings settings)
        {
            _deliverySettings = settings;
            if (_isDataDirtyForDeliverySettings)
            {
                RefreshDeliveryServices();
            }
        }

        private async void SetIconToDeliveryOption(TMP_Dropdown.OptionData data, int index, string spriteKey)
        {
            var load = Addressables.LoadAssetAsync<Sprite>(spriteKey).Task;
            data.image = Addressables.LoadAssetAsync<Sprite>("ui_default-loading-icon").WaitForCompletion();
            await Task.Yield();
            deliverySelection.options[index] = data;
            var icon = await load;
            if (icon != null)
            {
                data.image = icon;
            }
            deliverySelection.options[index] = data;
        }

        private void OnAmountInputSubmit(string text)
        {
            if (_data.IsConstantMass)
            {
                if (int.TryParse(text, out int value))
                {
                    SetInCartAmount(value);
                    return;
                }
            }
            else
            {
                if (float.TryParse(text, out float value))
                {
                    SetInCartAmount(value);
                    return;
                }
            }

            inCartAmountInputField.text = _amount.ToString(new NumberFormatInfo());
        }

        private void AddToCart()
        {
            SetInCartAmount(1);
        }

        private void OnMoreClick()
        {
            SetInCartAmount(_amount + 1);
        }
        
        private void OnLessClick()
        {
            SetInCartAmount(_amount - 1);
        }

        private void SetInCartAmount(float value)
        {
            _amount = Mathf.Clamp(value, 0, _data.amount);
            RefreshView();
            _inCardAmountChangedCallback.Invoke(_data, _amount);
        }

        public override void Selected()
        {
            base.Selected();
            selectionFrame.gameObject.SetActive(true);
        }

        public override void Deselected()
        {
            base.Deselected();
            selectionFrame.gameObject.SetActive(false);
        }

        public override void SetData(TradeItem data)
        {
            _data = data;
            _isDataDirtyForDeliverySettings = true;
            _amount = 0;
            signView.SetData(data.Sign);
            RefreshView();
        }

        public void SetInCardAmountChangedCallback(Action<TradeItem, float> callback)
        {
            _inCardAmountChangedCallback = callback;
        }
        
        public override void RefreshView()
        {
            costLabel.text = _data.Cost.ToString("C", CultureInfo.InvariantCulture);
            inCartAmountInputField.contentType = _data.IsConstantMass
                ? TMP_InputField.ContentType.IntegerNumber
                : TMP_InputField.ContentType.DecimalNumber;
            inCartAmountInputField.text = _amount.ToString(new NumberFormatInfo());
            amountLabel.text = _data.amount.ToString();
            moreButton.interactable = _amount < _data.amount;
            addToCartButton.gameObject.SetActive(_amount == 0);
            selectAmountGroup.gameObject.SetActive(_amount > 0);
            if (_isDataDirtyForDeliverySettings)
            {
                RefreshDeliveryServices();
            }
        }

        /*public void OnSelect(BaseEventData eventData)
        {
            OnSelectPrivate();
        }*/
        public void OnPointerClick(PointerEventData eventData)
        {
            OnSelectPrivate();
        }
        public override void EmitSelection()
        {
            OnSelectPrivate();
        }

        private void OnSelectPrivate()
        {
            OnInput?.Invoke(this, MultipleSelectionModifiers.None);
        }
        
        private void RefreshDeliveryServices()
        {
            if(_deliverySettings.IsNull) return;
            _isDataDirtyForDeliverySettings = false;
            deliverySelection.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>(_deliverySettings.Services.Count);
            int selectedIndex = 0;
            int counter = 0;
            foreach (var service in _deliverySettings.Services)
            {
                if (service.IsCanDeliver(_data.Sign, _deliverySettings.Destination))
                {
                    var option = new TMP_Dropdown.OptionData(service.NameToView);
                    options.Add(option);
                    SetIconToDeliveryOption(option, options.Count - 1, service.IconKey);
                    if (selectedIndex == 0 && _data.GetDeliveryService() == service)
                    {
                        selectedIndex = counter;
                    }
                    counter++;
                }
            }
            deliverySelection.AddOptions(options);
            deliverySelection.value = selectedIndex;
        }
    }
}