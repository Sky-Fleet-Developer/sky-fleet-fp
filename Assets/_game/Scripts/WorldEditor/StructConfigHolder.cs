using System.Collections.Generic;
using Core;
using Core.Configurations;
using Core.Graph;
using Core.Structure;
using Core.Structure.Serialization;
using Core.World;
using Runtime.Structure;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using Zenject;

namespace WorldEditor
{
    public class StructConfigHolder : MonoBehaviour
    {
        public StructureConfigurationHead configurationHead;
        public BlocksConfiguration blocksConfiguration;
        public GraphConfiguration graphConfiguration;
        [Inject] private WorldSpace _worldSpace;

#if UNITY_EDITOR
        [MenuItem("Tools/MakeConfigForStructure")]
        public static void MakeConfigForStructure()
        {
            GameObject o = Selection.activeGameObject;
            if (o && o.activeInHierarchy)
            {
                CreateForStructure(o);
                WiresEditor.OpenWindow();
                WiresEditor.CurrentEditor.GetFomSelection();
            }
        }
        [MenuItem("Tools/ConvertBlocksToPrefabs")]
        public static void ConvertBlocksToPrefabs()
        {
            var holder = Selection.activeGameObject.GetComponent<StructConfigHolder>();
            if (holder == null)
            {
                holder = Selection.activeGameObject.GetComponentInParent<StructConfigHolder>();
            }

            if (holder != null)
            {
                holder.TryConvertToPrefab();
            }
        }

        public void TryConvertToPrefab()
        {
            IStructure structure = GetComponentInChildren<IStructure>();
            if (structure == null)
            {
                return;
            }

            if (!structure.transform.gameObject.activeInHierarchy)
            {
                return;
            }

            bool isStructurePrefab = IsTablePrefab(structure);

            var tempObject = new GameObject("temp");
            List<(Transform, Transform)> parents = new();

            var root = (Component)structure;
            var blocks = root.GetComponentsInChildren<IBlock>();
            foreach (var block in blocks)
            {
                var gameObject = ((Component)block).gameObject;
                if (!IsTablePrefab(block) && !PrefabUtility.IsPartOfAnyPrefab(gameObject))
                {
                    gameObject = CreateAndReplacePrefab(gameObject);
                    parents.Add((gameObject.transform, gameObject.transform.parent));
                    gameObject.transform.SetParent(tempObject.transform);
                }
            }

            if (!isStructurePrefab)
            {
                CreateAndReplacePrefab(((Component)structure).gameObject);
            }

            foreach (var item in parents)
            {
                item.Item1.SetParent(item.Item2);
            }

            DestroyImmediate(tempObject);
        }

        private static bool IsTablePrefab(ITablePrefab prefab)
        {
            return TablePrefabs.Instance.GetItem(prefab.Guid) != null;
        }

        private static GameObject CreateAndReplacePrefab(GameObject source)
        {
            var savePath = EditorUtility.SaveFilePanel($"Save {source.name}", Application.dataPath + "/Assets/_game/Prefabs/", source.name, "prefab");
            if (!string.IsNullOrEmpty(savePath))
            {
                savePath = savePath.Replace(Application.dataPath + "/", "Assets/");
                //var name = savePath.Split('/').Last();
                savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);
                var result = PrefabUtility.SaveAsPrefabAssetAndConnect(source, savePath, InteractionMode.UserAction);
                TablePrefabs.Instance.SearchPrefabsInFolders();
                return result;
            }

            return null;
        }

        public static StructConfigHolder CreateForStructure(GameObject structure)
        {
            Undo.RecordObject(structure.transform, "re parent");
            StructConfigHolder config = new GameObject(structure.name + "_Config").AddComponent<StructConfigHolder>();
            Undo.RegisterCreatedObjectUndo(config.gameObject, "re parent");
            Transform tr = config.transform;
            tr.SetParent(structure.transform.parent);
            tr.SetSiblingIndex(structure.transform.GetSiblingIndex());
            tr.position = structure.transform.position;
            tr.rotation = structure.transform.rotation;
            structure.transform.SetParent(tr);
            EditorSceneManager.MarkSceneDirty(structure.scene);
            EditorUtility.SetDirty(structure.transform);
            config.graphConfiguration = new GraphConfiguration(structure.GetComponent<IStructure>());
            return config;
        }

        [Button]
        public void EditConfiguration()
        {
            IStructure instance = GetComponentInChildren<IStructure>();
            if (instance == null)
            {
                Debug.LogError("Instantiate prefab at first!");
                return;
            }
            Selection.activeGameObject = instance.transform.gameObject;
            WiresEditor.OpenWindow();
            WiresEditor.CurrentEditor.GetFomSelection();
        }
#endif

        private void Start()
        {
            Bootstrapper.OnLoadComplete.Subscribe(InstantiateStructure);
            var existRoot = TryGetRoot();
            if (existRoot && !existRoot.GetComponent<DynamicWorldObject>())
            {
                existRoot.AddComponent<DynamicWorldObject>();
            }
        }

        [Button]
        private async void InstantiateStructure()
        {
            IStructure structure = GetComponentInChildren<IStructure>();
            
            configurationHead.position = transform.position;
            configurationHead.rotation = transform.rotation;
            if (structure != null)
            {
                configurationHead.Root = structure.transform.gameObject;
                await blocksConfiguration.Apply(structure);
                await graphConfiguration.Apply(structure);
                structure.Init(true);
                if (_worldSpace)
                {
                    _worldSpace.RegisterStructure(structure);
                    return;
                }
            }
            else
            {
                if (_worldSpace)
                {
                    _worldSpace.RegisterStructure(configurationHead, new Configuration<IStructure>[] {blocksConfiguration, graphConfiguration});
                    return;
                }
            }
#if UNITY_EDITOR
            var structureFactory = new StructureFactory();
            structure = await structureFactory.Create(configurationHead, new Configuration<IStructure>[] {blocksConfiguration, graphConfiguration});
            structure.transform.SetParent(transform);
#endif
        }

        private GameObject TryGetRoot()
        {
            ITablePrefab prefab = GetComponentInChildren<ITablePrefab>();
            if (prefab != null)
            {
                return prefab.transform.gameObject;
            }
            return null;
        }
    }
}
