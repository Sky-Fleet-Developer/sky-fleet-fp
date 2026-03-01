using System.Collections.Generic;
using Core.Configurations;
using UnityEditor;
using UnityEngine;

namespace Runtime
{
    public class TablePrefab : MonoBehaviour, ITablePrefab
    {
        [SerializeField] private string guid;
        [SerializeField] private List<string> tags;
        public string Guid => guid;
        public List<string> Tags => tags;

#if UNITY_EDITOR
        public void Reset()
        {
            if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
            {
                var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                guid = AssetDatabase.GUIDFromAssetPath(path).ToString();
            }
        } 
#endif
    }
}