using System;
using System.Globalization;
using Core.Trading;
using Core.UIStructure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runtime.Trading.Ui
{
    public class TradeItemView : MonoBehaviour, ISelectionTarget, ISelectHandler
    {
        [SerializeField] private ItemSignView signView;
        [SerializeField] private TextMeshProUGUI costLabel;
        [SerializeField] private Image selectionFrame;
        private TradeItem _data;
        public TradeItem Data => _data;
        public Action<ISelectionTarget> OnSelected { get; set; }

        public void SetData(TradeItem data)
        {
            _data = data;
            signView.SetData(data.sign);
            costLabel.text = data.cost.ToString("C", CultureInfo.InvariantCulture);
        }

        public void OnSelect(BaseEventData eventData)
        {
            OnSelected?.Invoke(this);
        }

        public void SetSelectionState(bool state)
        {
            selectionFrame.gameObject.SetActive(state);
        }
    }
}