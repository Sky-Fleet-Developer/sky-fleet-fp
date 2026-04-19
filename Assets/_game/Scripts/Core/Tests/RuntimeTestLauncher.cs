using System;
using System.Threading.Tasks;
using Core.Misc;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Core.Tests
{
    public class RuntimeTestLauncher
    {
        public class Context
        {
            public Bootstrapper MyBootstrapper;
            public Scene MyScene;
            public DiContainer MyContainer;
            public RemoteConfigurationHandler MyRemoteConfigurationHandler;
            public bool IsSceneInstalled;
            public bool IsSystemsRun;
        }
        private const string SceneName = "RuntimeTestScene";
        private static Context _staticContext;
        public Context MyContext;

        public async Task LoadAndRunTestScene(bool installScene, bool runSystems, bool needReloadExistScene)
        {
            if (_staticContext != null)
            {
                if (needReloadExistScene)
                {
                    SceneManager.UnloadSceneAsync(MyContext.MyScene);
                    CreateContext();
                }
                else
                {
                    var prevContext = _staticContext;
                    MyContext = _staticContext;

                    if (!prevContext.IsSceneInstalled && installScene)
                    {
                        InstallScene();
                    }
                    
                    if (!prevContext.IsSystemsRun && runSystems)
                    {
                        await RunSystems();
                    }
                    return;
                }
            }
            else
            {
                CreateContext();
            }
            MyContext.MyBootstrapper = Bootstrapper.TakeManualControl();

            TaskCompletionSource<Exception> sceneLoadingTask = new TaskCompletionSource<Exception>();
            async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                MyContext.MyScene = SceneManager.GetSceneByName(SceneName);
                MyContext.MyContainer = new DiContainer();
                var setupSingletons = Bootstrapper.SetupSingletons(MyContext.MyContainer, ref MyContext.MyRemoteConfigurationHandler);

                if (installScene)
                {
                    try
                    {
                        InstallScene();
                    }
                    catch (Exception e)
                    {
                        sceneLoadingTask.SetResult(e);
                        return;
                    }
                }

                await setupSingletons;

                if (runSystems)
                {
                    await RunSystems();
                }
                sceneLoadingTask.SetResult(null);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            var sceneLoading = SceneManager.LoadSceneAsync(SceneName);
            var exception = await sceneLoadingTask.Task;
            if (exception != null)
            {
                throw exception;
            }
            await sceneLoading;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void CreateContext()
        {
            _staticContext = new Context();
            MyContext = _staticContext;
        }

        private async Task RunSystems()
        {
            await MyContext.MyBootstrapper.RunServicesAsync(MyContext.MyScene);
            MyContext.IsSystemsRun = true;
        }

        private void InstallScene()
        {
            Bootstrapper.BindScene(MyContext.MyScene, MyContext.MyContainer);
            Bootstrapper.InstallScene(MyContext.MyScene, MyContext.MyContainer);
            MyContext.IsSceneInstalled = true;
        }
    }
}