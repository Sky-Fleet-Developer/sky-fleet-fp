using System.Collections.Generic;
using System.Linq;
using Core.SessionManager;
using Core.UiStructure;
using Core.UIStructure;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Core.SessionManager.GameProcess;
using Core.Boot_strapper;
using System.Threading.Tasks;

namespace Runtime.Explorer.SessionUI
{
    public class GameUI : BearerCanvas, ILoadAtStart
    {
        [SerializeField] private GameObject gameMenuObj;

        public Task LoadStart()
        {
            PauseGame.Instance.OnPause += OpenGameMenu;
            PauseGame.Instance.OnResume += CloseGameMenu;
            CloseGameMenu();
            return Task.CompletedTask;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            PauseGame.Instance.OnPause -= OpenGameMenu;
            PauseGame.Instance.OnResume -= CloseGameMenu;
        }

        private void CloseGameMenu()
        {
            gameMenuObj.SetActive(false);
        }

        private void OpenGameMenu()
        {
            gameMenuObj.SetActive(true);
        }
    }
}
