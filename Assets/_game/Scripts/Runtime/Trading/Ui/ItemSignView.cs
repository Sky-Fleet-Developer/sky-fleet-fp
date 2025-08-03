using System;
using Core.Localization;
using Core.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Runtime.Trading.Ui
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
            icon.sprite = await Addressables.LoadAssetAsync<Sprite>($"ui_{_data.Id}").Task;
        }

        private void OnDestroy()
        {
            _data = null;
        }
    }
}