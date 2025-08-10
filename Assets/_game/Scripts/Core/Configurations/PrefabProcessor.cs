using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Core.Configurations
{
    public abstract class PrefabProcessor : ScriptableObject
    {
        public void ProcessPrefabs(IEnumerable<RemotePrefabItem> prefabItems)
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Prefabs process");
#endif
            foreach (var remotePrefabItem in prefabItems)
            {
                Process(remotePrefabItem);
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
        protected abstract void Process(RemotePrefabItem item);
    }
}