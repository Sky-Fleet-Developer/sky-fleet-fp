using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Session
{
    public static class SelectorScenes
    {
        public enum TypeScene
        {
            Menu = 0,
            Session = 1
        }

        private static AsyncOperation operationLoad;

        public static event Action StartChangeScene;

        public static void StartLoadSession()
        {
            if (operationLoad != null)
                return;

            StartChangeScene();
            operationLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
            operationLoad.completed += EndSessionLoad;
        }

        private static void EndSessionLoad(AsyncOperation asyncOpr)
        {
            operationLoad.completed -= EndSessionLoad;
            operationLoad = null;
            if (asyncOpr.isDone)
            {
                Debug.Log("Session scene loaded.");
            }
        }
    }
}