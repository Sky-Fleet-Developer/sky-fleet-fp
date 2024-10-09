using Core.UiStructure;
using Core.UIStructure;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.Network
{
    public class CreateServerMenu : Service
    {
        [SerializeField] private Button createServerButton;
        [SerializeField] private Button connectToServerButton;

        protected override void Awake()
        {
            base.Awake();
            createServerButton.onClick.AddListener(CreateServer);
            connectToServerButton.onClick.AddListener(ConnectToServer);
        }

        private void ConnectToServer()
        {
            ServiceIssue.Instance.GetOrMakeService<LobbyService>().Join();
            Window.Close();
        }

        private void CreateServer()
        {
            ServiceIssue.Instance.GetOrMakeService<LobbyService>().Create();
            Window.Close();
        }
    }
}