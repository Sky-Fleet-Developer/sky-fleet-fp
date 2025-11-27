using System.Linq;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.SessionManager;
using Core.Utilities;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Core
{
    public class Bootstrapper
    {
        public static LateEvent OnLoadComplete = new LateEvent();

        private const string GameSceneName = "GameScene";
        private const string LobbySceneName = "LobbyScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Main()
        {
            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnStateChanged;
            void OnStateChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    bootstrapper.Stop();
                }
            }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ForceRunInBackground()
        {
            Application.runInBackground = true;
            Application.backgroundLoadingPriority = ThreadPriority.High;
        }

        private DiContainer _projectContainer;
        private Session _sessionContext;

        private void Run()
        {
            Debug.Log("Bootstrapper running");
            _projectContainer = ProjectContext.Instance.Container;
            _projectContainer.Bind<DynamicPool>().FromNewComponentOnRoot().AsSingle();
            _sessionContext = new Session();
            _projectContainer.Bind<Session>().FromInstance(_sessionContext).AsSingle();

            _projectContainer.Inject(_sessionContext);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Stop()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var sceneContextContainer = _projectContainer.CreateSubContainer();
            /*if (scene.name == GameSceneName)
            {
                var context = sceneContextContainer.Resolve<Session>();
                context.SetSessionAsCreated();
            }*/

            InstallScene(scene, sceneContextContainer);
            RunServicesAsync(scene).Forget();
        }

        private void InstallScene(Scene scene, DiContainer container)
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
                    foreach (var installer in entry.GetComponentsInChildren<IMyInstaller>().OrderBy(x => ((Component)x).transform.GetSiblingIndex()))
                    {
                        installer.InstallBindings(container);
                    }
                }
            }

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
                    foreach (var monoBehaviour in entry.GetComponentsInChildren<MonoBehaviour>().OrderBy(x => x.transform.GetSiblingIndex()))
                    {
                        container.Inject(monoBehaviour);
                    }
                }
            }
        }

        private async UniTask RunServicesAsync(Scene scene)
        {
            foreach (var entry in scene.GetRootGameObjects().OrderBy(x => x.transform.GetSiblingIndex()))
            {
                foreach (var load in entry.GetComponents<ILoadAtStart>().OrderBy(x => ((Component)x).transform.GetSiblingIndex()))
                {
                    if (load.enabled)
                    {
                        Debug.Log($"BOOTSTRAPPER: Begin load {load}");
                        await load.Load();
                    }
                }

                if (entry.name.Contains("[Translator]"))
                {
                    foreach (var load in entry.GetComponentsInChildren<ILoadAtStart>(true).OrderBy(x => ((Component)x).transform.GetSiblingIndex()))
                    {
                        if (load.enabled)
                        {
                            Debug.Log($"BOOTSTRAPPER: Begin load {load}");
                            await load.Load();
                        }
                    }
                }
            }

            OnLoadComplete.Invoke();
        }
    }
}