using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Configurations.GoogleSheets
{
    [CreateAssetMenu(menuName = "SF/Configs/PathLoader")]
    public class PathLoader : ScriptableObject
    {
        private class Link
        {
            public string TableName;
            public string Url;
        }

        [SerializeField] private string url;
        [SerializeField] private char arraySeparator;
        [SerializeField] private Table[] tables;

        [Button]
        public void Load()
        {
            LoadAsync();
        }

        private async void LoadAsync()
        {
            var data = await TableUtilities.LoadAsync(url);
            var links = TableUtilities.ParseAs<Link>(data, '\t', arraySeparator);
            for (var i = 0; i < tables.Length; i++)
            {
                for (int j = 0; j < links.Length; j++)
                {
                    if (links[j].TableName == tables[i].TableName)
                    {
                        tables[i].LoadData(links[j].Url, '\t', arraySeparator);
                    }
                }
            }
        }

    }
}