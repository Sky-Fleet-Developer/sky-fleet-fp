using System.Collections.Generic;
using Core.Configurations;
using Core.Items;
using UnityEditor;
using UnityEngine;

namespace Runtime.Items
{
    public class ItemObject : MonoBehaviour, IItemObjectHandle
    {
        [SerializeField] private string guid;
        [SerializeField] private List<string> tags;
        public string Guid => guid;
        public List<string> Tags => tags;
        private ItemInstance _sourceItem;
        private string _ownerId;
        ItemInstance IItemObject.SourceItem => _sourceItem;
        public string OwnerId => _ownerId;

        void IItemObjectHandle.SetSourceItem(ItemInstance sign)
        {
            _sourceItem = sign;
        }

        void IItemObjectHandle.SetOwnership(string ownerId)
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