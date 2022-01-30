using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Core.UiStructure;
using Core.Utilities;
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