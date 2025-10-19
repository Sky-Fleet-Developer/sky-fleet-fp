using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class StructureFactory : IFactory<StructureConfigurationHead, IEnumerable<Configuration>, Task<IStructure>>, IStructureDestructor
    {
        public async Task<IStructure> Create(StructureConfigurationHead head, IEnumerable<Configuration> configurations)
        {
            var root = head.Root ?? await CreateRoot(head.bodyGuid);
            root.transform.position = head.position;
            root.transform.rotation = head.rotation;

            var structure = root.GetComponent<IStructure>();
            if (Application.isPlaying)
            {
                root.transform.position += WorldOffset.Offset;
            }
            if (structure is IDynamicStructure && !root.GetComponent<DynamicWorldObject>())
            {
                root.AddComponent<DynamicWorldObject>();
            }

            foreach (Configuration configuration in configurations)
            {
                if (configuration is Configuration<IStructure> structureConfiguration)
                {
                    await structureConfiguration.Apply(structure);
                }
            }

            structure.Init();
            return structure;
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
            foreach (IBlock structureBlock in structure.Blocks)
            {
                DynamicPool.Instance.Return(structureBlock.transform);
            }
            DynamicPool.Instance.Return(structure.transform);
        }

        public Configuration[] GetDefaultConfigurations(IStructure structure, out StructureConfigurationHead head)
        {
            head = new StructureConfigurationHead
            {
                bodyGuid = structure.Guid,
                position = structure.transform.position - WorldOffset.Offset,
                rotation = structure.transform.rotation,
                Root = structure.transform.gameObject,
            };
            if (structure is IGraph graph)
            {
                return new Configuration[] { new BlocksConfiguration(structure), new GraphConfiguration(structure) };
            }
            return new Configuration[] { new BlocksConfiguration(structure) };
        }
    }
}