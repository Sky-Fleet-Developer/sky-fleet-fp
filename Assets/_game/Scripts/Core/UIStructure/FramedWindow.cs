using UnityEngine;
using UnityEngine.UI;

namespace Core.UIStructure
{
    public class FramedWindow : Window
    {
        [SerializeField] private Button exitButton;

        protected override void Awake()
        {
            base.Awake();
            exitButton.onClick.AddListener(OnClickExit);
        }

        private void OnClickExit()
        {
            Close();
        }
    }
}