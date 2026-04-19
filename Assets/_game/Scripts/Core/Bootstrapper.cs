using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Misc;
using Core.SessionManager;
using Core.Utilities;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

namespace Core
{
    public class Bootstrapper
    {
        private static Bootstrapper _bootstrapper;
        public static LateEvent OnLoadComplete = new LateEvent();

        private const string GameSceneName = "GameScene";
        private const string LobbySceneName = "LobbyScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Main()
        {
            _bootstrapper = new Bootstrapper();
            _bootstrapper.Run();
            //TypeExtensions.Init();
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnStateChanged;
            void OnStateChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    _bootstrapper.Stop();
                    _bootstrapper = null;
                    EditorApplication.playModeStateChanged -= OnStateChanged;
                }
            }
#endif
        }

        private DiContainer _projectContainer;
        private Session _sessionContext;
        private bool _controlledManually;
        private RemoteConfigurationHandler _remoteConfigurationHandler;

        #region Tests
        // Needs to control by Tests
        public static Bootstrapper TakeManualControl()
        {
            _bootstrapper._controlledManually = true;
            _bootstrapper.Stop();
            return _bootstrapper;
        }
        
        #endregion
        
        private void Run()
        {
            Debug.Log("Bootstrapper running");
            _projectContainer = ProjectContext.Instance.Container;
            _projectContainer.Bind<DynamicPool>().FromNewComponentOnRoot().AsSingle();
            _sessionContext = new Session();
            _projectContainer.Bind<Session>().FromInstance(_sessionContext).AsSingle();

            SetupSingletons(_projectContainer, ref _remoteConfigurationHandler).Forget();
            
            _projectContainer.Inject(_sessionContext);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Stop()
        {
            _remoteConfigurationHandler.Dispose();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_controlledManually)
            {
                return;
            }
            var sceneContextContainer = _projectContainer.CreateSubContainer();
            sceneContextContainer.Bind<bool>().FromInstance(true).WithConcreteId("IsRuntime");
            /*if (scene.name == GameSceneName)
            {
                var context = sceneContextContainer.Resolve<Session>();
                context.SetSessionAsCreated();
            }*/

            BindScene(scene, sceneContextContainer);
            InstallScene(scene, sceneContextContainer);
            RunServicesAsync(scene).Forget();
        }

        public static void BindScene(Scene scene, DiContainer container)
        {
            foreach (var entry in scene.GetRootGameObjects().OrderBy(x => x.transform.GetSiblingIndex()))
            {
                if (!entry.gameObject.activeInHierarchy)
                {
                    continue;
                }
                foreach (var installer in entry.GetComponents<IMyInstaller>())
                {
                    installer.InstallBindings(container);
                }
                if (entry.name.Contains("[Translator]"))
                {
                    for (int i = 0; i < entry.transform.childCount; i++)
                    {
                        foreach (var installer in entry.transform.GetChild(i).GetComponents<IMyInstaller>())
                        {
                            installer.InstallBindings(container);
                        }
                    }
                }
            }
        }
        public static void InstallScene(Scene scene, DiContainer container)
        {
            foreach (var entry in scene.GetRootGameObjects().OrderBy(x => x.transform.GetSiblingIndex()))
            {
                if (!entry.gameObject.activeInHierarchy)
                {
                    continue;
                }
                foreach (var monoBehaviour in entry.GetComponents<MonoBehaviour>())
                {
                    container.Inject(monoBehaviour);
                }
                if (entry.name.Contains("[Translator]"))
                {
                    for (int i = 0; i < entry.transform.childCount; i++)
                    {
                        foreach (var monoBehaviour in entry.transform.GetChild(i).GetComponents<MonoBehaviour>())
                        {
                            container.Inject(monoBehaviour);
                        }
                    }
                }
            }
        }

        public async UniTask RunServicesAsync(Scene scene)
        {
            foreach (var entry in scene.GetRootGameObjects().OrderBy(x => x.transform.GetSiblingIndex()))
            {
                foreach (var load in entry.transform.GetComponents<ILoadAtStart>())
                {
                    if (load.enabled)
                    {
                        //Debug.Log($"BOOTSTRAPPER: Begin load {load}");
                        await load.Load();
                    }
                }

                if (entry.name.Contains("[Translator]"))
                {
                    for (int i = 0; i < entry.transform.childCount; i++)
                    {
                        foreach (var load in entry.transform.GetChild(i).GetComponents<ILoadAtStart>())
                        {
                            if (load.enabled)
                            {
                                //Debug.Log($"BOOTSTRAPPER: Begin load {load}");
                                await load.Load();
                            }
                        }
                    }
                }
            }

            OnLoadComplete.Invoke();
        }

        public static UniTask SetupSingletons(DiContainer container, ref RemoteConfigurationHandler remoteConfigurationHandler)
        {
            remoteConfigurationHandler = new RemoteConfigurationHandler();
            var configsLoading = remoteConfigurationHandler.LoadConfigurations();
            container.Bind<RemoteConfigurationHandler>().FromInstance(remoteConfigurationHandler);
            var tickService = new GameObject("[Tick]").AddComponent<TickService>();
            Object.DontDestroyOnLoad(tickService.gameObject);
            container.BindInstance(tickService);
            return configsLoading.AsUniTask();
        }
    }
}