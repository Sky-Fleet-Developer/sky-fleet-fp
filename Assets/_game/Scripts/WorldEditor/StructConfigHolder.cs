using Core;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
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
            Selection.activeGameObject = GetComponentInChildren<IStructure>().transform.gameObject;
            WiresEditor.OpenWindow();
            WiresEditor.CurrentEditor.GetFomSelection();
        }
#endif
        
        private void Start()
        {
            Bootstrapper.OnLoadComplete.Subscribe(Instance);
        }

        [Button]
        private async void Instance()
        {
            var structure = GetComponentInChildren<IStructure>();
            if (structure == null)
            {
                RemotePrefabItem wantedBlock = TablePrefabs.Instance.GetItem(configuration.bodyGuid);
                GameObject source = await wantedBlock.LoadPrefab();
                Transform instance;

                if (Application.isPlaying)
                {
                    instance = DynamicPool.Instance.Get(source.transform, transform);
                }
                else
                {
#if UNITY_EDITOR
                    instance = PrefabUtility.InstantiatePrefab(source.transform) as Transform;
                    instance.SetParent(transform, false);
#else
                    instance = Instantiate(blockSource.transform, transform);
#endif
                }

                structure = instance.GetComponent<IStructure>();
            }
            await Factory.ApplyConfiguration(structure, configuration);
            structure.Init();
        }
    }
}
