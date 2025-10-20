using System;
using System.Threading.Tasks;
using Core.Data;
using Core.Structure;
using Core.TerrainGenerator;
using Core.World;
using Newtonsoft.Json;
using Runtime.Structure;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace WorldEditor
{
    public class LocationContentEditor : EditorWindow
    {
        private const string PrefsKey = nameof(LocationContentEditor);
        private RectInt _currentContentRange;
        private RectInt _rangeSettings;
        private LocationChunksSet _chunksSet;
        private LocationInstaller _locationInstaller;
        private DynamicPositionFromCamera _dynamicPositionFromCamera;
        private WorldGrid _worldGrid;
        private WorldSpace _worldSpace;
        private StructuresLogisticsInstaller _structuresLogisticsInstaller;
        private TerrainProvider _terrainProvider;

        private Task _loading;
        private bool _isInitialized;

        [MenuItem("Window/SF/Location Content Editor")]
        public static void Open()
        {
            var window = GetWindow<LocationContentEditor>();
        }

        private void OnEnable()
        {
            _isInitialized = false;
            DiContainer diContainer = new DiContainer();

            if (!SetupLocation(diContainer)) return;
            if(!SetupWorld(diContainer)) return;
            if(!SetupWorldSpace(diContainer)) return;
            if(!SetupStructuresLogisticsInstaller(diContainer)) return;
            SetupTerrainProvider();
            
            _chunksSet = new LocationChunksSet(new LocationChunkEditorLoadStrategy());
            diContainer.BindInstance(_chunksSet);
            _dynamicPositionFromCamera = new DynamicPositionFromCamera();
            diContainer.Bind<IDynamicPositionProvider>().WithId("Player").FromInstance(_dynamicPositionFromCamera);
            
            diContainer.Inject(_chunksSet);
            diContainer.Inject(_locationInstaller);
            diContainer.Inject(_worldGrid);
            diContainer.Inject(_worldSpace);
            diContainer.Inject(_terrainProvider);
            
            _worldGrid.Load();
            
            var rangeFromSave = JsonConvert.DeserializeObject<RectInt?>(PlayerPrefs.GetString(PrefsKey + "." + nameof(_currentContentRange), ""));
            _currentContentRange = rangeFromSave ?? new RectInt(0, 0, 1, 1);
            _rangeSettings = _currentContentRange;
            
            Load(_currentContentRange);
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            _chunksSet.Unload();
        }

        private bool SetupLocation(DiContainer diContainer)
        {
            _locationInstaller = FindAnyObjectByType<LocationInstaller>();
            if (!_locationInstaller)
            {
                Debug.LogError("LocationInstaller is not found");
                return false;
            }
            _locationInstaller.InstallBindings(diContainer);
            return true;
        }

        private bool SetupWorld(DiContainer diContainer)
        {
            _worldGrid = FindAnyObjectByType<WorldGrid>();
            if (!_worldGrid)
            {
                Debug.LogError("WorldGrid is not found");
                return false;
            }
            _worldGrid.InstallBindings(diContainer);
            return true;
        }

        private bool SetupWorldSpace(DiContainer diContainer)
        {
            _worldSpace = FindAnyObjectByType<WorldSpace>();
            if (!_worldSpace)
            {
                Debug.LogError("WorldSpace is not found");
                return false;
            }
            _worldSpace.InstallBindings(diContainer);
            return true;
        }

        private bool SetupStructuresLogisticsInstaller(DiContainer diContainer)
        {
            _structuresLogisticsInstaller = FindAnyObjectByType<StructuresLogisticsInstaller>();
            if (!_structuresLogisticsInstaller)
            {
                Debug.LogError("StructuresLogisticsInstaller is not found");
                return false;
            }
            diContainer.Inject(_structuresLogisticsInstaller);
            _structuresLogisticsInstaller.InstallBindings();
            return true;
        }

        private void SetupTerrainProvider()
        {
            _terrainProvider = FindAnyObjectByType<TerrainProvider>();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDown  || Event.current.type == EventType.MouseUp || Event.current.type == EventType.KeyDown || Event.current.type == EventType.KeyUp)
            {
                DrawHeader();
                if (!_isInitialized)
                {
                    GUILayout.Label("Something went wrong, check console for details");
                    if (GUILayout.Button("Reload"))
                    {
                        OnEnable();
                    }
                    return;
                }
                if (_loading != null)
                {
                    if (_loading.IsCompleted)
                    {
                        FinishLoading();
                    }
                    GUILayout.Label("Loading...");
                    return;
                }

                DrawContentRangeSettings();
                DrawEntities();
            }
        }

        private async void FinishLoading()
        {
            if (!_loading.IsFaulted)
            {
                await _loading;
            }

            _loading = null;
        }

        private void DrawEntities()
        {
            int entitiesCount = 0;
            foreach (var entity in _worldGrid.EnumerateRadius(_dynamicPositionFromCamera.WorldPosition,
                         GameData.Data.lodDistances.GetLodDistance(GameData.Data.lodDistances.lods.Length - 1)))
            {
                entitiesCount++;
            }
            
            GUILayout.Label($"Entities: {entitiesCount}");
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
                if (GUILayout.Button("Load", GUILayout.Width(180), GUILayout.Height(30)))
                {
                    _currentContentRange = _rangeSettings;
                    PlayerPrefs.SetString(PrefsKey + "." + nameof(_currentContentRange), 
                        JsonConvert.SerializeObject(_currentContentRange));
                    Load(_rangeSettings);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh entities", GUILayout.Width(200), GUILayout.Height(25)))
                {
                    ConvertEditorEntitiesToWorld();
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

        private void ConvertEditorEntitiesToWorld()
        {
            var configHolders = FindObjectsByType<StructConfigHolder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var structConfigHolder in configHolders)
            {
                structConfigHolder.TryConvertToPrefab();
                var head = structConfigHolder.configurationHead;
                head.position = structConfigHolder.transform.position;
                head.rotation = structConfigHolder.transform.rotation;
                _worldSpace.RegisterStructure(head, structConfigHolder.blocksConfiguration, structConfigHolder.graphConfiguration);
                //DestroyImmediate(structConfigHolder.gameObject);
            }
            
            _loading = _chunksSet.Save();
        }
    }
}