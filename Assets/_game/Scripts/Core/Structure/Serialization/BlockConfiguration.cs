using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Structure.Rigging;
using Core.Utilities;
using UnityEditor;
using UnityEngine;

namespace Core.Structure.Serialization
{
    [System.Serializable]
    public class BlockConfiguration
    {
        public string path; //путь к модолю по трансформам/парент
        public string blockName;
        public int sibilingIdx;
        public string currentGuid; // текущий гуид
        public Vector3 localPosition;
        public Vector3 localRotation;
        
        private Dictionary<string, string> setup;//свойства помеченные [PlayerProperty]
        [SerializeField] private List<string> setupKeys = new List<string>();
        [SerializeField] private List<string> setupValues = new List<string>();

        public BlockConfiguration(IBlock block)
        {
            path = block.GetPath();
            blockName = block.transform.name;
            sibilingIdx = block.transform.GetSiblingIndex();
            currentGuid = block.Guid;
            localPosition = block.transform.localPosition;
            localRotation = block.transform.localEulerAngles;
            PropertyInfo[] properties = block.GetProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                string value = properties[i].GetValue(block).ToString();
                AddSetup(properties[i].Name, value);
            }
        }

        public void AddSetup(string key, string value)
        {
            setupKeys.Add(key);
            setupValues.Add(value);
            setup?.Add(key, value);
        }

        public bool TryGetSetup(string key, out string value)
        {
            if (setup == null)
            {
                setup = new Dictionary<string, string>();
                for (var i = 0; i < setupKeys.Count; i++)
                {
                    setup.Add(setupKeys[i], setupValues[i]);
                }
            }

            return setup.TryGetValue(key, out value);
        }

        public void ApplyPrimarySetup(IBlock block)
        {
            Transform transform = block.transform;
            transform.localPosition = localPosition;
            transform.localEulerAngles = localRotation;
            transform.localScale = Vector3.one;
            transform.name = blockName;
            transform.SetSiblingIndex(sibilingIdx);
        }

        public async Task ApplyConfiguration(IStructure structure)
        {
            IBlock block = structure.GetBlockByPath(path, blockName);

            if (block != null)
            {
                if (block.Guid != currentGuid)
                {
                    if (Application.isPlaying)
                    {
                        DynamicPool.Instance.Return(block.transform);
                    }
                    else
                    {
                        Object.DestroyImmediate(block.transform.gameObject);
                    }
                    block = await Instantiate(structure);
                }
            }
            else
            {
                block = await Instantiate(structure);
            }
            
            ApplyPrimarySetup(block);
        }

        private async Task<IBlock> Instantiate(IStructure structure)
        {
            Parent parent = null;
            for (int i = 0; i < 10; i++)
            {
                parent = structure.Parents.FirstOrDefault(x => x.Path == path);
                if (parent == null)
                {
                    await Task.Yield();
                    continue;
                }
                break;
            }
            
            if(parent == null) return null;
            
            RemotePrefabItem wantedBlock = TablePrefabs.Instance.GetItem(currentGuid);
            GameObject source = await wantedBlock.LoadPrefab();
            Transform instance;

            if (Application.isPlaying)
            {
                instance = DynamicPool.Instance.Get(source.transform, parent.Transform);
            }
            else
            {
#if UNITY_EDITOR
                instance = PrefabUtility.InstantiatePrefab(source.transform) as Transform;
                instance.SetParent(parent.Transform, false);
#else
                instance = Object.Instantiate(source.transform, parent.Transform);
#endif
            }

            return instance.GetComponent<IBlock>();
        }
        
        public void ApplySetup(IBlock block)
        {
            PropertyInfo[] properties = block.GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                if (TryGetSetup(properties[i].Name, out string value))
                {
                    block.ApplyProperty(properties[i], value);
                }
            }
        }
    }
}