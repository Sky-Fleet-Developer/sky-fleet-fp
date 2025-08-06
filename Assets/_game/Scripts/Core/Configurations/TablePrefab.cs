using System;
using System.Collections.Generic;
using Core.Structure;
using Core.Trading;
using UnityEditor;
using UnityEngine;

namespace Core.Configurations
{
    public class TablePrefab : MonoBehaviour, ITablePrefab, IItemInstanceHandle
    {
        [SerializeField] private string guid;
        [SerializeField] private List<string> tags;
        public string Guid => guid;
        public List<string> Tags => tags;
        private ItemSign _sourceItem;
        ItemSign IItemInstance.SourceItem => _sourceItem;
        void IItemInstanceHandle.SetSourceItem(ItemSign sign)
        {
            _sourceItem = sign;
        }
#if UNITY_EDITOR
        private void Reset()
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