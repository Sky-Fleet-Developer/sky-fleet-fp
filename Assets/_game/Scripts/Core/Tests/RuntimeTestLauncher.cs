using System.Threading.Tasks;
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
            await SceneManager.LoadSceneAsync(SceneName);
            MyContext.MyScene = SceneManager.GetSceneByName(SceneName);
            MyContext.MyContainer = new DiContainer();
            if (installScene)
            {
                InstallScene();
            }

            if (runSystems)
            {
                await RunSystems();
            }
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
            MyContext.MyBootstrapper.InstallScene(MyContext.MyScene, MyContext.MyContainer);
            MyContext.IsSceneInstalled = true;
        }
    }
}