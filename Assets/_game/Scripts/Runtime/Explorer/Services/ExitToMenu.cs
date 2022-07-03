using System.Collections;
using Core.SessionManager;
using Core.UiStructure;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.Services
{
    public class ExitToMenu : Service
    {
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        public override IEnumerator Show(BlockSequenceSettings settings = null)
        {
            ClearSignals();
            yesButton.onClick.AddListener(OnYes);
            noButton.onClick.AddListener(OnNo);
            return base.Show(settings);
        }

        public override IEnumerator Hide(BlockSequenceSettings settings = null)
        {
            return base.Hide();
        }

        private void ClearSignals()
        {
            yesButton.onClick.RemoveAllListeners();
            noButton.onClick.RemoveAllListeners();
        }

        private async void OnYes()
        {
            await SceneLoader.LoadMenuScene();
        }

        private void OnNo()
        {
            Window.Close();
        }
    }
}