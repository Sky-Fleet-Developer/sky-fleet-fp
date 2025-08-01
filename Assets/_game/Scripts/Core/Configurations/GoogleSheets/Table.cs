using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core.Configurations.GoogleSheets
{
    public abstract class Table : ScriptableObject
    {
        public abstract string TableName { get; }
        public abstract void LoadData(string url, char separator, char arraySeparator);
    }
    public abstract class Table<T> : Table where T : new()
    {
        public virtual T[] Data { get; protected set; }

        public override void LoadData(string url, char separator, char arraySeparator)
        {
            LoadDataAsync(url, separator, arraySeparator);
        }

        private async void LoadDataAsync(string url, char separator, char arraySeparator)
        {
            var data = await TableUtilities.LoadAsync(url);
#if UNITY_EDITOR
            Undo.RecordObject(this, "Set data to table");
#endif
            Data = TableUtilities.ParseAs<T>(data, separator, arraySeparator);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}