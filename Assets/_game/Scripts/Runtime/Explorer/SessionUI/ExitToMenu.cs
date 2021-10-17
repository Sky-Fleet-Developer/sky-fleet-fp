using Core.SessionManager;
using Core.UiStructure;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.SessionUI
{
    public class ExitToMenu : UiBlockBase
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

        private void OnYes()
        {
            SceneLoader.LoadMenuScene();
        }

        private void OnNo()
        {
            Frame.Close();
        }
    }
}