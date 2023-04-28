using Core;
using Core.Game;
using Core.Graph;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Utilities;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace WorldEditor
{
    public class StructConfigHolder : MonoBehaviour
    {
        public StructureConfiguration configuration;
        
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
            return config;
        }

        [Button]
        public void EditConfiguration()
        {
            BaseStructure instance = GetComponentInChildren<BaseStructure>();
            if (!instance)
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
        }

        [Button]
        private async void InstantiateStructure()
        {
            BaseStructure structure = GetComponentInChildren<BaseStructure>();
            if (structure == null)
            {
                RemotePrefabItem wantedBlock = TablePrefabs.Instance.GetItem(configuration.bodyGuid);
                GameObject source = await wantedBlock.LoadPrefab();
                Transform instance;

                if (Application.isPlaying)
                {
                    instance = DynamicPool.Instance.Get(source.transform, transform);
                    instance.position += WorldOffset.Offset;
                }
                else
                {
#if UNITY_EDITOR
                    instance = PrefabUtility.InstantiatePrefab(source.transform) as Transform;
                    instance.SetParent(transform, false);
#else
                    instance = Instantiate(source.transform, transform);
#endif
                }
                
                structure = instance.GetComponent<BaseStructure>();
            }
            await configuration.ApplyConfiguration(structure);
            structure.Init();
            IGraph graph = structure.gameObject.GetComponent<IGraph>();
            if (graph != null)
            {
                graph.InitGraph();
                configuration.ApplyWires(graph);
            }
        }
    }
}
