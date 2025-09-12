using Core.Items;
using Core.Localization;
using Core.Trading;
using TMPro;
using UnityEngine;

namespace Runtime.Trading.UI
{
    public class ItemSignDescriptionView : MonoBehaviour
    {
        [SerializeField] private ItemSignView signView;
        [SerializeField] private TextMeshProUGUI description;
        private ItemSign _data;

        private void OnEnable()
        {
            if (_data != null)
            {
                SetData(_data);
            }
            else
            {
                Clear();
            }
        }

        public void SetData(ItemSign data)
        {
            _data = data;
            signView.SetData(data);
            description.text = LocalizationService.Localize($"{data.Id}_description");
        }

        public void Clear()
        {
            description.text = string.Empty;
            signView.Clear();
        }
    }
}