using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Game;
using Core.Graph;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Serialization;
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
        public StructureConfiguration blocksConfiguration;
        public GraphConfiguration graphConfiguration;
        public IEnumerable<Configuration> GetAllConfigs()
        {
            yield return blocksConfiguration;
            yield return graphConfiguration;
        }
        
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
            config.graphConfiguration = new GraphConfiguration(structure.GetComponent<IGraph>());
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
        private Rigidbody _rigidbody;
        private bool _needFreezeRigidbody;
        private void Start()
        {
            _needFreezeRigidbody = false;
            _rigidbody = GetComponentInChildren<Rigidbody>();
            if (_rigidbody)
            {
                _needFreezeRigidbody = !_rigidbody.isKinematic;
                _rigidbody.isKinematic = true;
            }
            Bootstrapper.OnLoadComplete.Subscribe(InstantiateStructure);
        }

        [Button]
        private async void InstantiateStructure()
        {
            GameObject root = await GetOrCreateRoot();
            if (_needFreezeRigidbody)
            {
                _rigidbody.isKinematic = false;
            }
            foreach (Configuration config in GetAllConfigs())
            {
                Type genericType = config.GetType().BaseType.GenericTypeArguments[0];
                await config.TryApply(root.GetComponent(genericType));
            }
            
            root.GetComponent<IStructure>().Init();
            //TODO move to other class
          /*  IGraph graph = structure.transform.gameObject.GetComponent<IGraph>();
            if (graph != null)
            {
                graph.InitGraph();
                configuration.ApplyWires(graph);
            }*/
        }

        private async Task<GameObject> GetOrCreateRoot()
        {
            ITablePrefab prefab = GetComponentInChildren<ITablePrefab>();
            if (prefab == null)
            {
                RemotePrefabItem prefabItem = TablePrefabs.Instance.GetItem(blocksConfiguration.bodyGuid);
                GameObject source = await prefabItem.LoadPrefab();
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

                prefab = instance.GetComponent<ITablePrefab>();
            }

            return prefab.transform.gameObject;
        }
    }
}
