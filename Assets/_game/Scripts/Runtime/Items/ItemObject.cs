using System;
using System.Collections.Generic;
using Core.Items;
using Core.Utilities;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Runtime.Items
{
    public class ItemObject : MonoBehaviour, IItemObjectHandle
    {
        [SerializeField] private string guid;
        [SerializeField] private List<string> tags;
        [Inject] private IItemFactory _iItemFactory;
        public string Guid => guid;
        public List<string> Tags => tags;
        private ItemInstance _sourceItem;
        public ItemInstance SourceItem => _sourceItem;
        public LateEvent OnItemInitialized = new ();

        void IItemObjectHandle.SetSourceItem(ItemInstance sign)
        {
            _sourceItem = sign;
            OnItemInitialized.Invoke();
        }

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

        public void Deconstruct()
        {
            _iItemFactory.Deconstruct(this);
        }
    }
}