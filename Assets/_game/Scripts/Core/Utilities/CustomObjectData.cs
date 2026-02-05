using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Core.Utilities
{
    public class CustomObjectData : MonoBehaviour
    {
        [SerializeField] private List<string> keys = new();
        [SerializeField] private List<string> values = new();

        public void SetData(string key, string value)
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Set Custom Object Data");
#endif
            var index = keys.IndexOf(key);
            if (index != -1)
            {
                values[index] = value;
            }
            else
            {
                keys.Add(key);
                values.Add(value);
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void RemoveData(string key)
        {
            var index = keys.IndexOf(key);
            if (index != -1)
            {
#if UNITY_EDITOR
                Undo.RecordObject(this, "Remove Custom Object Data");
#endif
                keys.RemoveAt(index);
                values.RemoveAt(index);
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public string GetData(string key)
        {
            var index = keys.IndexOf(key);
            return index != -1 ? values[index] : null;
        }

        public bool TryGetData(string key, out string value)
        {
            var index = keys.IndexOf(key);
            value = index != -1 ? values[index] : null;
            return value != null;
        }
    }

    public static class CustomObjectDataExtensions
    {
        public static CustomObjectData GetOrAddCustomObjectData(this GameObject gameObject)
        {
#if UNITY_EDITOR
            Undo.RecordObject(gameObject, "Add Custom Object Data");
#endif
            var result = gameObject.GetComponent<CustomObjectData>() ?? gameObject.AddComponent<CustomObjectData>();
#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObject);
#endif
            return result;
        }

        public static CustomObjectData GetOrAddCustomObjectData(this Transform transform)
        {
#if UNITY_EDITOR
            Undo.RecordObject(transform.gameObject, "Add Custom Object Data");
#endif
            var result = transform.gameObject.GetComponent<CustomObjectData>() ??
                         transform.gameObject.AddComponent<CustomObjectData>();
#if UNITY_EDITOR
            EditorUtility.SetDirty(transform.gameObject);
#endif
            return result;
        }
    }
}