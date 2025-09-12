using System;
using System.Globalization;
using Core.Items;
using Core.Trading;
using Core.UIStructure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runtime.Trading.UI
{
    public class ItemInstanceView : ThingView<ItemInstance>, ISelectHandler
    {
        [SerializeField] private ItemSignView signView;
        [SerializeField] private TextMeshProUGUI amountLabel;
        [SerializeField] private Image selectionFrame;
        private ItemInstance _data;
        private float _amount;
        public override ItemInstance Data => _data;

        private void Awake()
        {
        }

        private void OnDestroy()
        {
        }

        public override void Selected()
        {
            selectionFrame.gameObject.SetActive(true);
        }

        public override void Deselected()
        {
            selectionFrame.gameObject.SetActive(false);
        }

        public override void SetData(ItemInstance data)
        {
            _data = data;
            _amount = 0;
            signView.SetData(data.Sign);
            RefreshView();
        }

        public override void RefreshView()
        {
            amountLabel.text = _data.Amount.ToString(NumberFormatInfo.CurrentInfo);
        }

        public void OnSelect(BaseEventData eventData)
        {
            OnSelectPrivate();
        }

        public override void EmitSelection()
        {
            OnSelectPrivate();
        }

        private void OnSelectPrivate()
        {
            OnSelected?.Invoke(this);
        }
    }
}