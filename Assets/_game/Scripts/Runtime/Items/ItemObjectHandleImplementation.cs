using System;
using System.Collections.Generic;
using Core.Items;
using UnityEditor;
using UnityEngine;

namespace Runtime.Items
{
    [Serializable]
    public class ItemObjectHandleImplementation : IItemObjectHandle
    {
        [SerializeField] private string guid;
        [SerializeField] private List<string> tags;
        public Transform transform => _component.transform;
        public string Guid => guid;
        public List<string> Tags => tags;
        private ItemInstance _sourceItem;
        private Component _component;
        public ItemInstance SourceItem => _sourceItem;

        public ItemObjectHandleImplementation(Component component)
        {
            _component = component;
        }

        public void SetSourceItem(ItemInstance sign)
        {
            _sourceItem = sign;
        }

#if UNITY_EDITOR
        public void Reset()
        {
            if (PrefabUtility.IsPartOfAnyPrefab(_component.gameObject))
            {
                var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_component.gameObject);
                guid = AssetDatabase.GUIDFromAssetPath(path).ToString();
            }
        } 
#endif
    }
}