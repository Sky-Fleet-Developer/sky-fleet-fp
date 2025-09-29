using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Configurations;
using Core.Game;
using Core.Graph;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Serialization;
using Core.Utilities;
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
        public StructureConfiguration blocksConfiguration;
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
            if (_worldSpace)
            {
                _worldSpace.RegisterStructure(blocksConfiguration, graphConfiguration);
            }
            else
            {
#if UNITY_EDITOR
                ITablePrefab prefab = GetComponentInChildren<ITablePrefab>();
                var structureFactory = new StructureFactory();

                var info = new StructureCreationRuntimeInfo
                    { parent = transform, localPosition = Vector3.zero, localRotaion = Quaternion.identity };
                if (prefab != null)
                {
                    info.existRoot = prefab.transform.gameObject;
                }

                await structureFactory.Create(info, blocksConfiguration, graphConfiguration);
#endif
            }
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
