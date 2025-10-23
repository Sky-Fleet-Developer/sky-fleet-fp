using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
using Core.Structure;
using Core.Structure.Serialization;
using Core.Utilities;
using Core.World;
using UnityEditor;
using UnityEngine;

namespace WorldEditor
{
    public class EditorStructureFactory : IStructureFactory
    {
        public async Task<IStructure> Create(StructureConfigurationHead head,
            IEnumerable<Configuration<IStructure>> configurations)
        {
            try
            {
                var root = head.Root ?? await CreateRoot(head.bodyGuid);
                root.transform.position = head.position;
                root.transform.rotation = head.rotation;
                root.hideFlags = HideFlags.DontSave;
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

                structure.Init();

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
                instance = PrefabUtility.InstantiatePrefab(source.transform) as Transform;
            }

            return instance.gameObject;
        }

        public void Destruct(IStructure structure)
        {
            if (structure != null && structure.transform)
            {
                UnityEngine.Object.DestroyImmediate(structure.transform.gameObject);
            }
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