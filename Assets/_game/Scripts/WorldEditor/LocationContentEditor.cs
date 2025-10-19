using System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace WorldEditor
{
    
    public class LocationContentEditor : EditorWindow
    {
        private const string PrefsKey = nameof(LocationContentEditor);
        private BoundsInt _currentContentRange;
        private BoundsInt _rangeSettings;
        
        [MenuItem("Window/SF/Location Content Editor")]
        public static void Open()
        {
            var window = GetWindow<LocationContentEditor>();
        }

        private void OnEnable()
        {
            var rangeFromSave = JsonConvert.DeserializeObject<BoundsInt?>(PlayerPrefs.GetString(PrefsKey + "." + nameof(_currentContentRange), ""));
            _currentContentRange = rangeFromSave ?? default;
            _rangeSettings = _currentContentRange;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDown  || Event.current.type == EventType.MouseUp)
            {
                DrawHeader();
                DrawContentRangeSettings();
            }
        }

        private void DrawContentRangeSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Content Range:");
            _rangeSettings = EditorGUILayout.BoundsIntField(_rangeSettings);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Load", GUILayout.Width(200), GUILayout.Height(40)))
            {
                _currentContentRange = _rangeSettings;
                PlayerPrefs.SetString(PrefsKey + "." + nameof(_currentContentRange), JsonConvert.SerializeObject(_currentContentRange));
                Load(_rangeSettings);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void DrawHeader()
        {
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var size = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 20;
            var style = GUI.skin.label.fontStyle;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            var color = GUI.skin.label.normal.textColor;
            GUI.skin.label.normal.textColor = Color.gray;
            GUILayout.Label(nameof(LocationContentEditor));
            GUI.skin.label.fontSize = size;
            GUI.skin.label.fontStyle = style;
            GUI.skin.label.normal.textColor = color;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
        }

        private void Load(BoundsInt rangeSettings)
        {
            Debug.Log($"Loading content range: {rangeSettings}");
        }
    }
}