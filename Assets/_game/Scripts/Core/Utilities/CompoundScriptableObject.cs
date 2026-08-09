using System.Collections.Generic;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Core.Utilities
{
    public class CompoundScriptableObject : ScriptableObject
    {
        [HideInInspector] public List<Object> children;

        public IEnumerable<T> GetChildAssets<T>() where T : Object
        {
            for (var i = 0; i < children.Count; i++)
            {
                if (children[i] is T t)
                {
                    yield return t;
                }
            }
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(CompoundScriptableObject), true)]
    public class MyScriptableObjectEditor : OdinEditor
    {
        private CompoundScriptableObject targetSO;
        private bool _assetSelectionMode;
        private Object _selectedAsset;
        private SerializedProperty _children;
        private List<Editor> _childObjects;
        protected override void OnEnable()
        {
            base.OnEnable();
        
            targetSO = (CompoundScriptableObject)target;
            _children = new SerializedObject(target).FindProperty("children");
            
            var path = AssetDatabase.GetAssetPath(targetSO);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            bool wasChanged = false;
            for (var i = 0; i < allAssets.Length; i++)
            {
                if (!targetSO.children.Contains(allAssets[i]) && allAssets[i] != targetSO)
                {
                    targetSO.children.Add(allAssets[i]);
                    wasChanged = true;
                }
            }
            _childObjects = new List<Editor>();
            for (var i = 0; i < targetSO.children.Count; i++)
            {
                _childObjects.Add(CreateEditor(targetSO.children[i]));
                _childObjects[i].CreateInspectorGUI();
            }

            if (wasChanged)
            {
                EditorUtility.SetDirty(this);
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        
            if (targetSO.children != null && !_assetSelectionMode)
            {
                if (GUILayout.Button("Attach asset"))
                {
                    _assetSelectionMode = true;;
                }

                _children.isExpanded = EditorGUILayout.Foldout(_children.isExpanded, new GUIContent("Children"));
                if (_children.isExpanded)
                {
                    int toRemove = -1;
                    for (int i = 0; i < _children.arraySize; i++)
                    {
                        var p = _children.GetArrayElementAtIndex(i);
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(p);
                        var asset = p.objectReferenceValue;
                        if (GUILayout.Button("Delete"))
                        {
                            if (asset != null)
                            {
                                AssetDatabase.RemoveObjectFromAsset(asset);
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                            }

                            toRemove = i;
                        }
                        GUILayout.EndHorizontal();
                    }

                    if (toRemove >= 0)
                    {
                        _childObjects.RemoveAt(toRemove);
                        _children.DeleteArrayElementAtIndex(toRemove);
                        targetSO.children.RemoveAt(toRemove);
                    }

                    for (int i = 0; i < _childObjects.Count; i++)
                    {
                        _childObjects[i].DrawHeader();
                        _childObjects[i].OnInspectorGUI();
                    }
                }
            }

            if (_assetSelectionMode)
            {
                _selectedAsset = EditorGUILayout.ObjectField(_selectedAsset, typeof(Object), false);
                if (_selectedAsset)
                {
                    if (GUILayout.Button("Attach asset"))
                    {
                        AddOtherAsset(_selectedAsset);
                    }
                }
            }
        }

        private void AddOtherAsset(Object otherAsset)
        {
            var myPath = AssetDatabase.GetAssetPath(targetSO);
            var clone = Instantiate(otherAsset);
            clone.name = otherAsset.name;
            AssetDatabase.AddObjectToAsset(clone, myPath);
            targetSO.children.Add(clone);
            _childObjects.Add(CreateEditor(clone));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
    #endif
}