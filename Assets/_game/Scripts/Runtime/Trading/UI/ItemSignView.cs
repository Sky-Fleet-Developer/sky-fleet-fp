using System;
using Core.Items;
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
            if (nameLabel)
            {
                nameLabel.text = LocalizationService.Localize($"{data.Id}_name");
            }

            try
            {
                LoadSpriteAsync();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private async void LoadSpriteAsync()
        {
            icon.gameObject.SetActive(true);
            var sprite = await Addressables.LoadAssetAsync<Sprite>($"ui_{_data.Id}_icon").Task;
            icon.sprite = sprite;
            if (!sprite)
            {
                Debug.LogError($"Sprite (ui_{_data.Id}_icon) was not found");
            }
        }

        private void OnDestroy()
        {
            _data = null;
        }

        public void Clear()
        {
            icon.gameObject.SetActive(false);
            _data = null;
            if (nameLabel)
            {
                nameLabel.text = string.Empty;
            }
        }
    }
}