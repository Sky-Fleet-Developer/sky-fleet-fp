using System;
using Client;
using Core.UiStructure;
using Core.UIStructure;
using Core.UIStructure.Utilities;
using Runtime.Explorer.Services;
using Server;
using Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Explorer.Network
{
    public class LobbyService : Service
    {
        [SerializeField] private Button disconnectButton;
        private INetworkBehaviour behaviour;

        protected override void Awake()
        {
            base.Awake();
            disconnectButton.onClick.AddListener(Disconnect);
        }

        public void Create()
        {
            behaviour = new BaseServer();
            InitInternal();
        }

        public void Join()
        {
            behaviour = new BaseClient();
            InitInternal();
        }

        private void InitInternal()
        {
            behaviour.Init();
        }

        private void Update()
        {
            behaviour?.UpdateServer();
        }
        
        private void Disconnect()
        {
            behaviour?.Shutdown();
            behaviour = null;
            Window.Close();
        }
    }
}
