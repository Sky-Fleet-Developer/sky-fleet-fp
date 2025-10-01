using System.Threading.Tasks;
using Core.Configurations;
using Core.Structure;
using Core.Structure.Serialization;
using Core.Utilities;
using Core.World;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Runtime.Structure
{
    public class StructureFactory : IFactory<StructureCreationRuntimeInfo, StructureConfiguration, GraphConfiguration, Task<IStructure>>
    {
        public async Task<IStructure> Create(StructureCreationRuntimeInfo runtimeInfo,
            StructureConfiguration structureConfiguration, GraphConfiguration graphConfiguration)
        {
            var root = runtimeInfo.ExistRoot ?? await CreateRoot(runtimeInfo, structureConfiguration);
            var structure = root.GetComponent<IStructure>();
            if (Application.isPlaying)
            {
                root.transform.position += WorldOffset.Offset;
            }
            if (structure is IDynamicStructure && !root.GetComponent<DynamicWorldObject>())
            {
                root.AddComponent<DynamicWorldObject>();
            }

            await structureConfiguration.TryApply(root);
            await graphConfiguration.TryApply(root);

            structure.Init();
            CycleService.RegisterStructure(structure);
            return structure;
        }

        private async Task<GameObject> CreateRoot(StructureCreationRuntimeInfo runtimeInfo, StructureConfiguration structureConfiguration)
        {
            RemotePrefabItem prefabItem = TablePrefabs.Instance.GetItem(structureConfiguration.bodyGuid);
            GameObject source = await prefabItem.LoadPrefab();
            Transform instance;

            if (Application.isPlaying)
            {
                instance = DynamicPool.Instance.Get(source.transform, runtimeInfo.Parent);
            }
            else
            {
#if UNITY_EDITOR
                instance = PrefabUtility.InstantiatePrefab(source.transform) as Transform;
                instance.SetParent(runtimeInfo.Parent, false);
#else
                instance = Object.Instantiate(source.transform, runtimeInfo.parent);
#endif
            }

            return instance.gameObject;
        }
    }
}