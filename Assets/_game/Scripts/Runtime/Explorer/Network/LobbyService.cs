using System;
using System.Threading.Tasks;
using Client;
using Core.UiStructure;
using Core.UIStructure;
using Core.UIStructure.Utilities;
using Fusion;
using Runtime.Explorer.Services;
using Server;
using Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Runtime.Explorer.Network
{
    public class LobbyService : Service
    {
        [SerializeField] private Button disconnectButton;
        private NetworkRunner networkRunner;
        private NetworkSceneManagerDefault sceneManager;
        private INetworkBehaviour networkBehaviour;

        protected override void Awake()
        {
            base.Awake();
            if (!gameObject.TryGetComponent(out networkRunner))
            {
                networkRunner = gameObject.AddComponent<NetworkRunner>();
            }
            if (!gameObject.TryGetComponent(out sceneManager))
            {
                sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            }

            disconnectButton.onClick.AddListener(Disconnect);
        }

        public void Create()
        {
            networkBehaviour = new BaseServer();
            InitInternal(GameMode.Host);
        }

        public void Join()
        {
            networkBehaviour = new BaseClient();
            InitInternal(GameMode.Client);
        }

        private async void InitInternal(GameMode gameMode)
        {
            networkRunner.ProvideInput = true;
            networkRunner.AddCallbacks(networkBehaviour);
            var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            var sceneInfo = new NetworkSceneInfo();
            if (scene.IsValid)
            {
                sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
            }

            var startGameResult = await networkRunner.StartGame(new StartGameArgs()
            {
                GameMode = gameMode,
                SessionName = "TestRoom",
                Scene = scene,
                SceneManager = sceneManager
            });

            if (startGameResult.Ok)
            {
                Debug.Log($"Game started as {gameMode.ToString()}");
            }
            else
            {
                Debug.LogError(startGameResult.ErrorMessage);
            }
        }

        private void Disconnect()
        {
            networkRunner.Shutdown();
            Window.Close();
        }
    }
}
