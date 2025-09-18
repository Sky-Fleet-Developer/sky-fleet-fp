using System;
using System.Globalization;
using Core.Items;
using Core.Trading;
using Core.UIStructure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;
using IDropHandler = Core.UIStructure.Utilities.IDropHandler;

namespace Runtime.Trading.UI
{
    public class ItemInstanceView : ThingView<ItemInstance>, IPointerClickHandler
    {
        [SerializeField] private ItemSignView signView;
        [SerializeField] private TextMeshProUGUI amountLabel;
        [SerializeField] private Image selectionFrame;
        private ItemInstance _data;
        public override ItemInstance Data => _data;

        private void Awake()
        {
        }

        private void OnDestroy()
        {
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

        public override void SetData(ItemInstance data)
        {
            _data = data;
            signView.SetData(data.Sign);
            RefreshView();
        }

        public override void RefreshView()
        {
            amountLabel.text = _data.Amount.ToString(NumberFormatInfo.CurrentInfo);
        }

        /*public void OnSelect(BaseEventData eventData)
        {
            OnClickPrivate();
        }*/

        public override void EmitSelection()
        {
            OnClickPrivate();
        }

        private void OnClickPrivate()
        {
            OnSelected?.Invoke(this);
            OnInput(this, MultipleSelectionModifiers.None.GetFromInput());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClickPrivate();
        }
    }
}