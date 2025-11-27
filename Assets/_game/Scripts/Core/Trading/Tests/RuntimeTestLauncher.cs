using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Trading.Tests
{
    public class RuntimeTestLauncher
    {
        private const string SceneName = "RuntimeTestScene";
        
        public async Task Run()
        {
            await SceneManager.LoadSceneAsync(SceneName);
            
        }
    }
}