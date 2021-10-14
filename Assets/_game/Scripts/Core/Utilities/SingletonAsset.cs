using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Core.Utilities
{
    public class AssetPath : System.Attribute
    {
        public string Path = string.Empty;
    }
    public class SingletonAsset<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;
        private static Object _lock = new Object();

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    CheckInstance();
                    return _instance;
                }
            }
        }

        protected static T Instantiate()
        {
            AssetPath attribute = typeof(T).GetCustomAttributes(true).FirstOrDefault(x => x is AssetPath) as AssetPath;
            string searchPath = "";
            if (attribute != null)
                searchPath += attribute.Path;

            _instance = Resources.Load<T>(searchPath + typeof(T).Name);

            if (_instance == null)
            {
                _instance = CreateInstance<T>();
#if UNITY_EDITOR
                string Path = "Assets/Resources/";

                if (attribute != null)
                    Path += attribute.Path;

                string folderPath = Path[Path.Length - 1] == '/' ? Path.Remove(Path.Length - 1) : Path;

                int folderIndex = folderPath.LastIndexOf('/');

                string folderName = folderPath.Remove(0, folderIndex + 1);

                string parentFolder = folderPath.Remove(folderIndex);

                if (AssetDatabase.IsValidFolder(folderPath) == false)
                    AssetDatabase.CreateFolder(parentFolder, folderName);

                AssetDatabase.CreateAsset(_instance, Path + typeof(T).Name + ".asset");
#endif
                _instance.name = typeof(T).Name;
            }
            return _instance;
        }
    
        public static void CheckInstance()
        {
            if (_instance == null)
            {
                Instantiate();
            }
        }
    }
}