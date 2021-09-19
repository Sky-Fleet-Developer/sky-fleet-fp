using System;
using System.Linq;
using UnityEngine;

namespace Core.Utilities
{
    public class DontDestroyOnLoad : System.Attribute { }

    public abstract class Singleton<T> : UnityEngine.MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    Instantiate();
                }

                return _instance;
            }
        }

        public static void CheckInstance()
        {
            if (_instance == null)
            {
                Instantiate();
            }
        }

        public static void DestroyInstance()
        {
            if(_instance) Destroy(_instance.gameObject);
            _instance = null;
        }

        public static bool hasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                AddToDontDestroy();

                Setup();
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        public static void ResetInstance()
        {
            _instance = null;
        }

        protected static T Instantiate()
        {
            _instance = FindObjectOfType<T>();
            if (_instance != null)
            {
                Debug.Log($"Find instance for {typeof(T)}");
                _instance.Setup();
            }
            else
            {
                Debug.Log($"Create instance for {typeof(T)}");
                var go = new GameObject($"[{typeof(T)}]");
                _instance = go.AddComponent<T>();
                if(Application.isPlaying == false) _instance.Setup();
            }

            AddToDontDestroy();

            return _instance;
        }

        protected static void AddToDontDestroy()
        {
            var attribute = typeof(T).GetCustomAttributes(true).FirstOrDefault(x => x is DontDestroyOnLoad);
            if (attribute != null)
            {
                DontDestroyOnLoad(_instance);
            }
        }

        protected virtual void Setup()
        {
        
        }
    }
}