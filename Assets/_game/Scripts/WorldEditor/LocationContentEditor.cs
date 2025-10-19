using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Data;
using Core.World;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace WorldEditor
{
    public class LocationChunkEditorLoadStrategy : ILocationChunkLoadStrategy
    {
        public Task Load(LocationChunkData data, Vector2Int coord)
        {
            List<Task> tasks = new List<Task>();
            foreach (var worldEntity in data.Entities)
            {
                worldEntity.OnLodChanged(0);
                var loadTask = worldEntity.GetAnyLoad();
                if (loadTask != null)
                {
                    tasks.Add(loadTask);
                }
            }
            
            return Task.WhenAll(tasks);
        }

        public Task Unload(LocationChunkData data, Vector2Int coord)
        {
            List<Task> tasks = new List<Task>();
            foreach (var worldEntity in data.Entities)
            {
                worldEntity.OnLodChanged(GameData.Data.lodDistances.lods.Length);
                var loadTask = worldEntity.GetAnyLoad();
                if (loadTask != null)
                {
                    tasks.Add(loadTask);
                }
            }
            
            return Task.WhenAll(tasks);
        }
    }
    public class LocationContentEditor : EditorWindow
    {
        private const string PrefsKey = nameof(LocationContentEditor);
        private RectInt _currentContentRange;
        private RectInt _rangeSettings;
        private LocationChunksSet _chunksSet;
        private LocationInstaller _locationInstaller;
        private Task _loading;

        [MenuItem("Window/SF/Location Content Editor")]
        public static void Open()
        {
            var window = GetWindow<LocationContentEditor>();
        }

        private void OnEnable()
        {
            _locationInstaller = FindAnyObjectByType<LocationInstaller>();
            DiContainer diContainer = new DiContainer();
            _locationInstaller.InstallBindings(diContainer);
            _chunksSet = new LocationChunksSet(diContainer.Resolve<Location>(), new LocationChunkEditorLoadStrategy());
            var rangeFromSave = JsonConvert.DeserializeObject<RectInt?>(PlayerPrefs.GetString(PrefsKey + "." + nameof(_currentContentRange), ""));
            _currentContentRange = rangeFromSave ?? new RectInt(0, 0, 1, 1);
            _rangeSettings = _currentContentRange;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDown  || Event.current.type == EventType.MouseUp)
            {
                DrawHeader();
                if (_loading != null)
                {
                    if (_loading.IsCompleted)
                    {
                        _loading.Wait();
                        _loading = null;
                    }
                    GUILayout.Label("Loading...");
                    return;
                }

                DrawContentRangeSettings();
            }
        }

        private void DrawContentRangeSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Content Range:");
            _rangeSettings = EditorGUILayout.RectIntField(_rangeSettings);
            GUILayout.EndHorizontal();
            if (_rangeSettings != _currentContentRange)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Load", GUILayout.Width(200), GUILayout.Height(40)))
                {
                    _currentContentRange = _rangeSettings;
                    PlayerPrefs.SetString(PrefsKey + "." + nameof(_currentContentRange), 
                        JsonConvert.SerializeObject(_currentContentRange));
                    Load(_rangeSettings);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
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

        private void Load(RectInt rangeSettings)
        {
            Debug.Log($"Loading content range: {rangeSettings}");
            _loading = _chunksSet.SetRange(rangeSettings);
        }
    }
}