using System.Collections.Generic;
using Core.Configurations;
using Core.Items;
using UnityEditor;
using UnityEngine;

namespace Runtime.Items
{
    public class ItemInstance : MonoBehaviour, IItemInstanceHandle
    {
        [SerializeField] private string guid;
        [SerializeField] private List<string> tags;
        public string Guid => guid;
        public List<string> Tags => tags;
        private ItemSign _sourceItem;
        private string _ownerId;
        ItemSign IItemInstance.SourceItem => _sourceItem;
        public string OwnerId => _ownerId;

        void IItemInstanceHandle.SetSourceItem(ItemSign sign)
        {
            _sourceItem = sign;
        }

        void IItemInstanceHandle.SetOwnership(string ownerId)
        {
            _ownerId = ownerId;
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