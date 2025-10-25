using TMPro;
using UnityEngine;

namespace Runtime.Trading.UI
{
    public class CurrencyView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        private string _prefix;

        public void SetPrefix(string prefix)
        {
            _prefix = prefix;
        }
        public void SetCurrency(int currency)
        {
            text.text = _prefix + currency.ToString("C0", System.Globalization.CultureInfo.CurrentCulture);
        }

        public void SetColor(Color color)
        {
            text.color = color;
        }
    }
}
