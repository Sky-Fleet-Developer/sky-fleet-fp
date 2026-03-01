using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Configurations;
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
        
        public Dictionary<string, string> setup;//свойства помеченные [PlayerProperty](удалено) или [ConstantField]
        public List<string> setupKeys = new List<string>();
        public List<string> setupValues = new List<string>();
        
        public Dictionary<string, string> constantFields;//[ConstantField]
        public List<string> constantFieldsKeys = new List<string>();
        public List<string> constantFieldsValues = new List<string>();
        
        public Dictionary<string, string> playerProperties;//[PlayerProperty]
        public List<string> playerPropertiesKeys = new List<string>();
        public List<string> playerPropertiesValues = new List<string>();
        public BlockConfiguration(){}
        public BlockConfiguration(IBlock block)
        {
            path = block.GetPath();
            blockName = block.transform.name;
            sibilingIdx = block.transform.GetSiblingIndex();
            currentGuid = block.AssetId;
            localPosition = block.transform.localPosition;
            localRotation = block.transform.localEulerAngles;
            
            PropertyInfo[] properties = block.GetBlockPlayerPropertiesCached();
            for (int i = 0; i < properties.Length; i++)
            {
                string value = properties[i].GetValue(block).ToString();
                AddSetup(properties[i].Name, value);
                playerProperties?.Add(properties[i].Name, value);
                playerPropertiesKeys.Add(properties[i].Name);
                playerPropertiesValues.Add(value);
            }
            
            FieldInfo[] fields = block.GetBlockConstantFieldsCached();
            for (int i = 0; i < fields.Length; i++)
            {
                string value = fields[i].GetValue(block).ToString();
                AddSetup(fields[i].Name, value);
                constantFields?.Add(fields[i].Name, value);
                constantFieldsKeys.Add(fields[i].Name);
                constantFieldsValues.Add(value);
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
                    setup[setupKeys[i]] = setupValues[i];
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
                if (block.AssetId != currentGuid)
                {
                    //structure.RemoveBlock(block);
                    if (Application.isPlaying)
                    {
                        DynamicPool.Instance.Return(block.transform);
                    }
                    else
                    {
                        Object.DestroyImmediate(block.transform.gameObject);
                    }

                    block = await Instantiate(structure);
                    //structure.AddBlock(block);
                }
            }
            else
            {
                block = await Instantiate(structure);
                //structure.AddBlock(block);
            }

            if (block == null)
            {
                return;
            }
            Transform transform = block.transform;
            transform.localPosition = localPosition;
            transform.localEulerAngles = localRotation;
            transform.localScale = Vector3.one;
            transform.name = blockName;
            transform.SetSiblingIndex(sibilingIdx);
        }

        private async Task<IBlock> Instantiate(IStructure structure)
        {
            Parent parent = null;
            for (int i = 0; i < 10; i++)
            {
                parent = structure.GetOrFindParent(path);
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
            PropertyInfo[] properties = block.GetBlockPlayerPropertiesCached();
            for (int i = 0; i < properties.Length; i++)
            {
                if (TryGetSetup(properties[i].Name, out string value))
                {
                    block.ApplyProperty(properties[i], value);
                }
            }
            
            FieldInfo[] fields = block.GetBlockConstantFieldsCached();
            for (int i = 0; i < fields.Length; i++)
            {
                if (TryGetSetup(fields[i].Name, out string value))
                {
                    block.ApplyField(fields[i], value);
                }
            }
        }
    }
}