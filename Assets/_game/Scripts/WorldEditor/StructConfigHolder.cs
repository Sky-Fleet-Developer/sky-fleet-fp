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
        [MenuItem("Tools/Make config for structure")]
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
            var graph = structure.GetComponent<IGraph>();
            if (graph != null)
            {
                config.graphConfiguration = new GraphConfiguration(graph);
            }
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
            if (!existRoot.GetComponent<DynamicWorldObject>())
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
                await blocksConfiguration.ApplyGameObject(structure.transform.gameObject);
                await graphConfiguration.ApplyGameObject(structure.transform.gameObject);
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
                    _worldSpace.RegisterStructure(configurationHead, new Configuration[] {blocksConfiguration, graphConfiguration});
                    return;
                }
            }
#if UNITY_EDITOR
            var structureFactory = new StructureFactory();
            await structureFactory.Create(configurationHead, new Configuration[] {blocksConfiguration, graphConfiguration});
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
