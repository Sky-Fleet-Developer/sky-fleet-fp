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
        private DynamicPositionFromWorldRect _dynamicPositionFromWorldRect;
        private WorldGrid _worldGrid;
        private WorldSpace _worldSpace;
        private StructuresLogisticsInstaller _structuresLogisticsInstaller;
        private TerrainProvider _terrainProvider;
        private WorldOffset _worldOffset;

        private Task _loading;
        private bool _isInitialized;
        private WorldOffset.IWorldOffsetHandler _worldOffsetHandler;
        private TerrainProvider.ITerrainProviderHandler _terrainProviderHandler;

        [MenuItem("Window/SF/Location Content Editor")]
        public static void Open()
        {
            var window = GetWindow<LocationContentEditor>();
        }

        private void OnEnable()
        {
            /*_dynamicPositionFromCamera = new DynamicPositionFromCamera();
            while (!_dynamicPositionFromCamera.IsInitialized)
            {
                await Task.Yield();
            } 
            await Task.Delay(100);*/
            Initialize();
            UnityEditor.Compilation.CompilationPipeline.compilationStarted += OnCompilation;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredEditMode:
                    Initialize();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    Load(RectInt.zero);
                    _loading.Wait();
                    break;
            }
        }

        private void Initialize()
        {
            _isInitialized = false; 
            DiContainer diContainer = new DiContainer();

            if (!SetupLocation(diContainer)) return;
            if(!SetupWorld(diContainer)) return;
            if(!SetupWorldSpace(diContainer)) return;
            if(!SetupStructuresLogisticsInstaller(diContainer)) return;
            if(!SetupTerrainProvider(diContainer)) return;
            SetupWorldOffset(diContainer);
            var strategy = new LocationChunkEditorLoadStrategy();
            _chunksSet = new LocationChunksSet(strategy);
            _dynamicPositionFromWorldRect = new DynamicPositionFromWorldRect(_worldGrid, _chunksSet);
            diContainer.BindInstance(_chunksSet);
            diContainer.Bind<IDynamicPositionProvider>().WithId("Player").FromInstance(_dynamicPositionFromWorldRect);
            
            diContainer.Inject(strategy);
            diContainer.Inject(_chunksSet);
            diContainer.Inject(_locationInstaller);
            diContainer.Inject(_worldGrid);
            diContainer.Inject(_worldSpace);
            diContainer.Inject(_terrainProvider);
            _worldOffsetHandler = diContainer.TryResolve<WorldOffset.IWorldOffsetHandler>();
            _worldOffsetHandler?.TakeControl();
            _terrainProviderHandler = diContainer.TryResolve<TerrainProvider.ITerrainProviderHandler>();

            _worldGrid.Load();
            
            var rangeFromSave = JsonConvert.DeserializeObject<RectInt?>(PlayerPrefs.GetString(PrefsKey + "." + nameof(_currentContentRange), ""));
            bool isLoaded = PlayerPrefs.GetInt(PrefsKey + ".isLoaded", 0) == 1;
            _rangeSettings = rangeFromSave ?? new RectInt(0, 0, 1, 1);
            _currentContentRange = isLoaded ? _rangeSettings : RectInt.zero;

            Load(_currentContentRange);
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            _chunksSet.Unload();
            _worldOffsetHandler?.ReleaseControl();
            UnityEditor.Compilation.CompilationPipeline.compilationStarted -= OnCompilation;
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
        
        private void OnCompilation(object obj)
        {
            Load(RectInt.zero);
            _loading.Wait();
        }

        private bool SetupWorldOffset(DiContainer diContainer)
        {
            _worldOffset = FindAnyObjectByType<WorldOffset>();
            if (!_worldOffset)
            {
                Debug.LogError("WorldOffset is not found");
                return false;
            }
            _worldOffset.InstallBindings(diContainer);
            return true;
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

        private bool SetupTerrainProvider(DiContainer diContainer)
        {
            _terrainProvider = FindAnyObjectByType<TerrainProvider>();
            if (!_terrainProvider)
            {
                Debug.LogError("TerrainProvider is not found");
                return false;
            }
            _terrainProvider.InstallBindings(diContainer);
            return true;
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
                        Initialize();
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
            foreach (var entity in _worldGrid.EnumerateRadius(_dynamicPositionFromWorldRect.WorldPosition,
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
                    PlayerPrefs.SetInt(PrefsKey + ".isLoaded", 1); 
                    PlayerPrefs.SetString(PrefsKey + "." + nameof(_currentContentRange), 
                        JsonConvert.SerializeObject(_currentContentRange));
                    Load(_currentContentRange);
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

            if (_currentContentRange.size != Vector2Int.zero)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Unload", GUILayout.Width(180), GUILayout.Height(30)))
                {
                    PlayerPrefs.SetInt(PrefsKey + ".isLoaded", 0); 
                    _currentContentRange = RectInt.zero;
                    Load(_currentContentRange);
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
            
            if (_worldOffsetHandler != null)
            {
                Vector2 center = rangeSettings.center * _worldGrid.GetCellSize();
                _worldOffsetHandler.SetOffset(new Vector3(-center.x, 0, -center.y));
            }
            
            _loading = LoadProcess(rangeSettings);
        }

        private async Task LoadProcess(RectInt rangeSettings)
        {
            await _chunksSet.SetRange(rangeSettings);
            if (rangeSettings.size != Vector2Int.zero)
            {
                await _terrainProviderHandler.LoadPropsForCurrentPosition();
            }
            else
            {
                await _terrainProviderHandler.Unload();
            }
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