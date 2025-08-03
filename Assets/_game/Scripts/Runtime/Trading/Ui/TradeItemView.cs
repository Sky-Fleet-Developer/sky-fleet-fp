using Core.Trading;
using TMPro;
using UnityEngine;

namespace Runtime.Trading.Ui
{
    public class TradeItemView : MonoBehaviour
    {
        [SerializeField] private ItemSignView signView;
        [SerializeField] private TextMeshProUGUI costLabel;

        public void SetData(TradeItem data)
        {
            signView.SetData(data.sign);
            costLabel.text = data.cost.ToString("C");
        }
    }
}