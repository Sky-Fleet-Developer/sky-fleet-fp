using System;
using System.Collections.Generic;
using Core.Configurations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.World
{
    public partial class PrefabEntityInstaller
    {
        #if UNITY_EDITOR
        private static TablePrefabs _tablePrefabsEditor;

        [ShowInInspector]
        [ValueDropdown("GetAbleItems")]
        private string PrefabId
        {
            get => prefabId;
            set => prefabId = value;
        }

        private ITablePrefab _current;
        
        private IEnumerable<ValueDropdownItem<string>> GetAbleItems()
        {
            EnsureObjects();
            if (_current != null)
            {
                yield return new ("self", _current.Guid);
                PrefabId = _current.Guid;
                yield break;
            }
            foreach (var remotePrefabItem in _tablePrefabsEditor.items)
            {
                yield return new (remotePrefabItem.GetReferenceInEditor().name, remotePrefabItem.guid);
            }
        }
        
        private void EnsureObjects()
        {
            _tablePrefabsEditor ??= Resources.FindObjectsOfTypeAll<TablePrefabs>()[0];
            _current ??= GetComponent<ITablePrefab>();
        }
        
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureObjects();
            }
        }
        #endif
    }
}