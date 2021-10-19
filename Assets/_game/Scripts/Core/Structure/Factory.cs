using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Core.SessionManager.SaveService;
using Core.Structure.Rigging;
using Core.Utilities;
using Core.Utilities.AsyncAwaitUtil.Source;
using Sirenix.Utilities;
using UnityEngine;
using  Object = UnityEngine.Object;

namespace Core.Structure
{
    public static class Factory
    {
        public static Dictionary<Type, PropertyInfo[]> blocksProperties;

        public static PropertyInfo[] GetBlockProperties(IBlock block)
        {
            Type blockType = block.GetType();
            if (blocksProperties == null) blocksProperties = new Dictionary<Type, PropertyInfo[]>();
            if (blocksProperties.ContainsKey(blockType)) return blocksProperties[blockType];

            PropertyInfo[] properties = GetBlockProperties(blockType);
            blocksProperties.Add(blockType, properties);
            return properties;
        }

        private static PropertyInfo[] GetBlockProperties(Type type)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();

            Type attribute = typeof(PlayerPropertyAttribute);

            string log = $"Properties for type {type.Name}:\n";
            
            foreach (PropertyInfo property in type.GetProperties())
            {
                if (property.GetCustomAttributes().FirstOrDefault(x => x.GetType() == attribute) != null)
                {
                    properties.Add(property);
                    log += $"{property.Name},";
                }
            }

            Debug.Log(log);
            
            return properties.ToArray();
        }
        
        private static void ApplyProperty(IBlock instance, PropertyInfo propery, string value)
        {
            Type type = propery.PropertyType;
            if (type == typeof(string))
            {
                propery.SetValue(instance, value);
            }
            else if (type == typeof(float))
            {
                if (float.TryParse(value, out float val))
                {
                    propery.SetValue(instance, val);
                }
                else
                {
                    Debug.LogError($"Cannot parce {value} into float!");
                }
            }
            else if (type == typeof(int))
            {
                if (int.TryParse(value, out int val))
                {
                    propery.SetValue(instance, val);
                }
                else
                {
                    Debug.LogError($"Cannot parce {value} into int!");
                }
            }
            else if (type == typeof(bool))
            {
                if (bool.TryParse(value, out bool val))
                {
                    propery.SetValue(instance, val);
                }
                else
                {
                    Debug.LogError($"Cannot parce {value} into bool!");
                }
            }
        }

        public static void ApplySetup(IBlock block, Dictionary<string, string> setup)
        {
            PropertyInfo[] properties = GetBlockProperties(block);
            for (int i = 0; i < properties.Length; i++)
            {
                if (setup.TryGetValue(properties[i].Name, out string value))
                {
                    ApplyProperty(block, properties[i], value);
                }
            }
        }

        public static async Task ApplyConfiguration(IStructure structure, StructureConfiguration configuration)
        {
            if (structure.transform.gameObject.activeInHierarchy == false)
            {
                Debug.LogError("Apply configuration only to instances!");
                return;
            }

            structure.RefreshBlocksAndParents();

            List<Task> awaiters = new List<Task>();

            for (int i = 0; i < structure.Blocks.Count; i++)
            {
                IBlock block = structure.Blocks[i];
                string path = GetPath(block);
                BlockConfiguration blockConfig = configuration.GetBlock(path);

                if (blockConfig == null) continue;

                if (block.Guid != blockConfig.currentGuid)
                {
                    Task task = ReplaceBlock(structure, i, blockConfig.currentGuid);
                    awaiters.Add(task);
                }
            }
            await Task.WhenAll(awaiters);
            await new WaitForEndOfFrame();
            structure.RefreshBlocksAndParents();

            foreach (IBlock block in structure.Blocks)
            {
                string path = GetPath(block);
                BlockConfiguration blockConfig = configuration.GetBlock(path);

                if (blockConfig == null) continue;

                ApplySetup(block, blockConfig.setup);
            }

            try
            {
                structure.InitBlocks();
                structure.InitWires();
                structure.OnInitComplete();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            Debug.Log($"{structure.transform.name} configuration success!");
        }

        public static async Task ReplaceBlock(IStructure structure, int blockIdx, string guid)
        {
            IBlock block = structure.Blocks[blockIdx];

            RemotePrefabItem wantedBlock = TablePrefabs.Instance.GetItem(guid);
            GameObject blockSource = await wantedBlock.LoadPrefab();
            Transform instance;

            if (Application.isPlaying)
            {
                instance = DynamicPool.Instance.Get(blockSource.transform, block.transform.parent);
            }
            else
            {
                instance = Object.Instantiate(blockSource.transform, block.transform.parent);
            }

            instance.localPosition = block.transform.localPosition;
            instance.localRotation = block.transform.localRotation;
            instance.localScale = block.transform.lossyScale;
            instance.name = block.transform.name;
            instance.SetSiblingIndex(block.transform.GetSiblingIndex());

            if (Application.isPlaying)
            {
                DynamicPool.Instance.Return(block.transform);
            }
            else
            {
                Object.DestroyImmediate(block.transform.gameObject);
            }
        }

        public static BlockConfiguration GetConfiguration(IBlock block)
        {
            PropertyInfo[] properties = GetBlockProperties(block);

            Dictionary<string, string> setup = new Dictionary<string, string>(); 
            
            for (int i = 0; i < properties.Length; i++)
            {
                string value = properties[i].GetValue(block).ToString();
                setup.Add(properties[i].Name, value);
            }

            return new BlockConfiguration {setup = setup, path = GetPath(block), currentGuid = block.Guid };
        }

        public static string GetPath(IBlock block)
        {
            string result = block.transform.name;
            Transform tr = block.transform.parent;
            while (tr.GetComponent<IStructure>() == null)
            {
                tr = tr.parent;
                result = tr.name + "/" + result;
            }

            return result;
        }

        public static (Transform parent, string name) GetParent(IStructure structure, string path)
        {
            string[] pathStrings = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            Transform tr = structure.transform;
            for (int i = 0; i < pathStrings.Length - 1; i++)
            {
                tr = tr.Find(pathStrings[i]);
                if (tr == null) return (null, pathStrings[pathStrings.Length - 1]);
            }

            return (tr, pathStrings[pathStrings.Length - 1]);
        }

        public static StructureConfiguration GetConfiguration(IStructure structure)
        {
            /*StructureConfiguration oldConfig = null;
            if (!string.IsNullOrEmpty(structure.Configuration))
            {
                oldConfig = JsonConvert.DeserializeObject<StructureConfiguration>(structure.Configuration);
            }*/
            
            StructureConfiguration configuration = new StructureConfiguration
            {
                blocks = new List<BlockConfiguration>(structure.Blocks.Count),
                wires = new List<string>()
            };

            for(int i = 0; i < structure.Blocks.Count; i++)
            {
                IBlock block = structure.Blocks[i];
                
                configuration.blocks.Add(GetConfiguration(block));
            }

            return configuration;
        }
        

        public static Dictionary<Type, FieldInfo[]> blocksPorts;

        public static FieldInfo[] GetPortsInfo(IBlock block)
        {
            List<FieldInfo> fields = new List<FieldInfo>();

            Type blockType = block.GetType();
            Type type = typeof(Port);

            string log = $"Ports for type {blockType.Name}:\n";
            
            foreach (FieldInfo property in blockType.GetFields())
            {
                if (property.FieldType == type || property.FieldType.InheritsFrom(type))
                {
                    fields.Add(property);
                    log += $"{property.Name},";
                }
            }

            Debug.Log(log);
            
            return fields.ToArray();
        }
        
        public static List<PortPointer> GetAllPorts(IStructure structure)
        {
            List<PortPointer> result = new List<PortPointer>();
            foreach (IBlock structureBlock in structure.Blocks)
            {
                GetAllPorts(structureBlock, ref result);
            }

            return result;
        }
        
        public static void GetAllPorts(IBlock block, ref List<PortPointer> result)
        {
            GetPorts(block, ref result);
            GetSpecialPorts(block, ref result);
        }
        
        public static void GetPorts(IBlock block, ref List<PortPointer> result)
        {
            FieldInfo[] properties = GetPortsInfo(block);
            foreach (FieldInfo property in properties)
            {
                result.Add(new PortPointer(block, property.GetValue(block) as Port));
            }
        }
        
        public static void GetSpecialPorts(IBlock block, ref List<PortPointer> result)
        {
            if(block is ISpecialPorts specialPortsBlock)
            {
                IEnumerable<PortPointer> specialPorts = specialPortsBlock.GetPorts();
                Debug.Log($"+ {specialPorts.Count()} special ports");
                result.AddRange(specialPorts);
            }
        }

        public static string GetWireString(List<string> guids)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < guids.Count; i++)
            {
                result.Append(guids[i]);
                if (i < guids.Count - 1)
                {
                    result.Append(".");
                }
            }

            return result.ToString();
        }
    }
}
