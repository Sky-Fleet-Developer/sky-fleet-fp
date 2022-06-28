using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.SessionManager
{
    public static class SceneLoader
    {
        public enum TypeScene
        {
            Menu = 0,
            Session = 1
        }

        private static AsyncOperation loadingOperation;
        private static TaskCompletionSource<bool> sceneLoading;
        private static int nextSceneBuildIdx;

        public static event Action StartChangeScene;

        public static Task LoadGameScene() => LoadScene(1);
        public static Task LoadScene(int buildIdx)
        {
            if (sceneLoading == null)
            {
                StartChangeScene?.Invoke();
                nextSceneBuildIdx = buildIdx;
                loadingOperation = SceneManager.LoadSceneAsync(buildIdx, LoadSceneMode.Single);

                sceneLoading = new TaskCompletionSource<bool>();
            }
            else
            {
                if (nextSceneBuildIdx != buildIdx) throw new Exception("Previous loading is not complete!\n");
            }

            return sceneLoading.Task;
        }

        public static Task LoadMenuScene() => LoadScene(0);

        private static void EndLoad(AsyncOperation operation)
        {
            loadingOperation.completed -= EndLoad;
            sceneLoading.SetResult(true);
            if (loadingOperation.isDone && SceneManager.GetActiveScene().buildIndex > 0)
            {
                Debug.Log("Session scene loaded.");
            }
            loadingOperation = null;
            sceneLoading = null;
        }
    }
}