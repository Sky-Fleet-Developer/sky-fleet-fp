using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Configurations;
using Core.Graph;
using Core.Structure;
using Core.Structure.Serialization;
using Core.Utilities;
using Core.World;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Runtime.Structure
{
    public class StructureFactory : IStructureFactory
    {
        [Inject] private DiContainer _diContainer;

        public async Task<IStructure> Create(StructureConfigurationHead head,
            IEnumerable<Configuration<IStructure>> configurations)
        {
            try
            {
                var root = head.Root ?? await CreateRoot(head.bodyGuid);
                root.transform.position = head.position;
                root.transform.rotation = head.rotation;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    root.hideFlags = HideFlags.DontSave;
                }
#endif
                var structure = root.GetComponent<IStructure>();

                root.transform.position += WorldOffset.Offset;
                if (structure is IDynamicStructure && !root.GetComponent<DynamicWorldObject>())
                {
                    root.AddComponent<DynamicWorldObject>();
                }

                foreach (Configuration<IStructure> configuration in configurations)
                {
                    await configuration.Apply(structure);
                }

                _diContainer?.Inject(structure);
                structure.Init();
                List<Task> tasks = new List<Task>();
                foreach (var load in root.GetComponentsInChildren<ILoadAtStart>())
                {
                    _diContainer?.Inject(load);
                    tasks.Add(load.Load());
                }

                foreach (var task in tasks)
                {
                    await task;
                }

                return structure;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }
        

        private async Task<GameObject> CreateRoot(string guid)
        {
            RemotePrefabItem prefabItem = TablePrefabs.Instance.GetItem(guid);
            GameObject source = await prefabItem.LoadPrefab();
            Transform instance;

            if (Application.isPlaying)
            {
                instance = DynamicPool.Instance.Get(source.transform);
            }
            else
            {
#if UNITY_EDITOR
                instance = PrefabUtility.InstantiatePrefab(source.transform) as Transform;
#else
                instance = Object.Instantiate(source.transform, runtimeInfo.parent);
#endif
            }

            return instance.gameObject;
        }

        public void Destruct(IStructure structure)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                foreach (IBlock structureBlock in structure.Blocks)
                {
                    DynamicPool.Instance.Return(structureBlock.transform);
                }

                DynamicPool.Instance.Return(structure.transform);
#if UNITY_EDITOR
            }
            else
            {
                if (structure != null && structure.transform)
                {
                    Object.DestroyImmediate(structure.transform.gameObject);
                }
            }
#endif
        }

        public Configuration<IStructure>[] GetDefaultConfigurations(IStructure structure,
            out StructureConfigurationHead head)
        {
            head = new StructureConfigurationHead
            {
                bodyGuid = structure.Guid,
                position = structure.transform.position - WorldOffset.Offset,
                rotation = structure.transform.rotation,
                Root = structure.transform.gameObject,
            };
            return new Configuration<IStructure>[]
                { new BlocksConfiguration(structure), new GraphConfiguration(structure) };
        }
    }
}