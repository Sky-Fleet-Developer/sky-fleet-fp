using System;
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

        private static AsyncOperation operationLoad;

        public static event Action StartChangeScene;

        public static void LoadGameScene(Action<AsyncOperation> onComplete = null)
        {
            if (operationLoad != null)
                return;

            StartChangeScene?.Invoke();
            operationLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
            operationLoad.completed += EndLoad;
            operationLoad.completed += onComplete;
        }

        private static void EndLoad(AsyncOperation asyncOpr)
        {
            operationLoad.completed -= EndLoad;
            operationLoad = null;
            if (asyncOpr.isDone)
            {
                Debug.Log("Session scene loaded.");
            }
        }
    }
}