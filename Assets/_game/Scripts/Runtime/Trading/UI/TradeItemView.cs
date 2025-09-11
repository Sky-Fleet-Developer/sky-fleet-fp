using System;
using System.Globalization;
using Core.Trading;
using Core.UIStructure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Runtime.Trading.UI
{
    public class TradeItemView : MonoBehaviour, ISelectionTarget, ISelectHandler
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
        private TradeItem _data;
        private float _amount;
        private Action<TradeItem, float> _inCardAmountChangedCallback;
        public TradeItem Data => _data;
        public Action<ISelectionTarget> OnSelected { get; set; }

        private void Awake()
        {
            addToCartButton.onClick.AddListener(AddToCart);
            moreButton.onClick.AddListener(OnMoreClick);
            lessButton.onClick.AddListener(OnLessClick);
            inCartAmountInputField.onEndEdit.AddListener(OnAmountInputSubmit);
        }

        private void OnDestroy()
        {
            addToCartButton.onClick.RemoveListener(AddToCart);
            moreButton.onClick.RemoveListener(OnMoreClick);
            lessButton.onClick.RemoveListener(OnLessClick);
            inCartAmountInputField.onEndEdit.RemoveListener(OnAmountInputSubmit);
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

        public void Selected()
        {
            selectionFrame.gameObject.SetActive(true);
        }

        public void Deselected()
        {
            selectionFrame.gameObject.SetActive(false);
        }

        public void SetData(TradeItem data)
        {
            _data = data;
            _amount = 0;
            signView.SetData(data.sign);
            RefreshView();
        }

        public void SetInCardAmountChangedCallback(Action<TradeItem, float> callback)
        {
            _inCardAmountChangedCallback = callback;
        }
        
        public void RefreshView()
        {
            costLabel.text = _data.cost.ToString("C", CultureInfo.InvariantCulture);
            inCartAmountInputField.contentType = _data.IsConstantMass
                ? TMP_InputField.ContentType.IntegerNumber
                : TMP_InputField.ContentType.DecimalNumber;
            inCartAmountInputField.text = _amount.ToString(new NumberFormatInfo());
            amountLabel.text = _data.amount.ToString();
            moreButton.interactable = _amount < _data.amount;
            addToCartButton.gameObject.SetActive(_amount == 0);
            selectAmountGroup.gameObject.SetActive(_amount > 0);
        }

        public void OnSelect(BaseEventData eventData)
        {
            OnSelected?.Invoke(this);
        }
    }
}