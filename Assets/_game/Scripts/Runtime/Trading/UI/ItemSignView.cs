using Core.Localization;
using Core.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Runtime.Trading.UI
{
    public class ItemSignView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameLabel;
        private ItemSign _data;

        public void SetData(ItemSign data)
        {
            _data = data;
            nameLabel.text = LocalizationService.Localize($"{data.Id}_name");
            LoadSpriteAsync();
        }

        private async void LoadSpriteAsync()
        {
            icon.gameObject.SetActive(true);
            icon.sprite = await Addressables.LoadAssetAsync<Sprite>($"ui_{_data.Id}").Task;
        }

        private void OnDestroy()
        {
            _data = null;
        }

        public void Clear()
        {
            icon.gameObject.SetActive(false);
            _data = null;
            nameLabel.text = string.Empty;
        }
    }
}