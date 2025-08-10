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
        [FormerlySerializedAs("countLabel")] [SerializeField] private TextMeshProUGUI amountLabel;
        [SerializeField] private Image selectionFrame;
        private TradeItem _data;
        public TradeItem Data => _data;
        public Action<ISelectionTarget> OnSelected { get; set; }
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
            signView.SetData(data.sign);
            RefreshView();
        }

        public void RefreshView()
        {
            costLabel.text = _data.cost.ToString("C", CultureInfo.InvariantCulture);
            amountLabel.text = _data.amount.ToString();
        }

        public void OnSelect(BaseEventData eventData)
        {
            OnSelected?.Invoke(this);
        }
    }
}