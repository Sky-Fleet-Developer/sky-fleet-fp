using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using  UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

public class Array : EditorWindow
{
    public static Array currentWindow;
    public ArraySettings Settings;
    public Editor SettingsEditor;
    
    [System.Serializable]
    public class ArraySettings : ScriptableObject
    {
        public Vector3 translation;
        public Vector3 globalRotation;
        public Vector3 translationRotation;
        public Vector3 scale;
        [Space(10)]
        public int count;
    }
    
    [MenuItem("Tools/Array")]
    public static void CreateWindow()
    {
        currentWindow = GetWindow<Array>();
    }

    
    private void OnEnable()
    {
        Settings = CreateInstance<ArraySettings>();
        SettingsEditor = Editor.CreateEditor(Settings);
    }

    private void OnGUI()
    {
        SettingsEditor.OnInspectorGUI();

        if (Selection.activeGameObject)
        {
            if (GUILayout.Button("Apply"))
            {
                MakeArray();
            }
        }
    }

    private void MakeArray()
    {
        var scene = EditorSceneManager.GetActiveScene();
        Transform reference = Selection.activeGameObject.transform;
        if (reference.parent)
        {
            Undo.RecordObject(reference.parent.transform, "Create new object");
        }

        for (int i = 0; i < Settings.count; i++)
        {
            reference = Instantiate(reference, reference.parent);
            reference.name = Selection.activeGameObject.name + "_" + i;
            ApplyTransformation(reference);
        }

        Selection.activeGameObject = reference.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private void ApplyTransformation(Transform transform)
    {
        Quaternion translationRotation = Quaternion.Euler(Settings.translationRotation);
        transform.localPosition = translationRotation * transform.localPosition;
        transform.eulerAngles += Settings.globalRotation;
        transform.localPosition += Settings.translation;
    }
}
