using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            var blockType = block.GetType();
            if (blocksProperties == null) blocksProperties = new Dictionary<Type, PropertyInfo[]>();
            if (blocksProperties.ContainsKey(blockType)) return blocksProperties[blockType];

            var properties = GetBlockProperties(blockType);
            blocksProperties.Add(blockType, properties);
            return properties;
        }

        private static PropertyInfo[] GetBlockProperties(Type type)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();

            var attribute = typeof(PlayerPropertyAttribute);

            string log = $"Properties for type {type.Name}:\n";
            
            foreach (var property in type.GetProperties())
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
            var type = propery.PropertyType;
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
            var properties = GetBlockProperties(block);
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

            structure.RefreshParents();

            List<Task> awaiters = new List<Task>();

            for (int i = 0; i < structure.Blocks.Count; i++)
            {
                var block = structure.Blocks[i];
                var path = GetPath(block);
                var blockConfig = configuration.GetBlock(path);

                if (blockConfig == null) continue;

                if (block.Guid != blockConfig.current)
                {
                    var task = ReplaceBlock(structure, i, blockConfig.current);
                    awaiters.Add(task);
                }
            }

            await Task.WhenAll(awaiters);
            await new WaitForEndOfFrame();
            
            structure.RefreshParents();

            foreach (var block in structure.Blocks)
            {
                var path = GetPath(block);
                var blockConfig = configuration.GetBlock(path);

                if (blockConfig == null) continue;

                ApplySetup(block, blockConfig.setup);
            }

            structure.InitBlocks();
            structure.InitWires();
            structure.OnInitComplete();

            Debug.Log($"{structure.transform.name} configuration success!");
        }

        public static async Task ReplaceBlock(IStructure structure, int blockIdx, string guid)
        {
            var block = structure.Blocks[blockIdx];

            var wantedBlock = TableRigging.Instance.GetItem(guid);
            var blockSource = await wantedBlock.GetBlock();
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
            var properties = GetBlockProperties(block);

            Dictionary<string, string> setup = new Dictionary<string, string>(); 
            
            for (int i = 0; i < properties.Length; i++)
            {
                string value = properties[i].GetValue(block).ToString();
                setup.Add(properties[i].Name, value);
            }

            return new BlockConfiguration {setup = setup, path = GetPath(block), current = block.Guid };
        }

        public static string GetPath(IBlock block)
        {
            string result = block.transform.name;
            var tr = block.transform.parent;
            while (tr.GetComponent<IStructure>() == null)
            {
                tr = tr.parent;
                result = tr.name + "/" + result;
            }

            return result;
        }

        public static (Transform parent, string name) GetParent(IStructure structure, string path)
        {
            var pathStrings = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            var tr = structure.transform;
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
            
            var configuration = new StructureConfiguration
            {
                blocks = new List<BlockConfiguration>(structure.Blocks.Count),
                wires = new List<string>()
            };

            for(int i = 0; i < structure.Blocks.Count; i++)
            {
                var block = structure.Blocks[i];
                
                configuration.blocks.Add(GetConfiguration(block));
            }

            return configuration;
        }
        

        public static Dictionary<Type, FieldInfo[]> blocksPorts;

        public static FieldInfo[] GetPortsInfo(IBlock block)
        {
            List<FieldInfo> fields = new List<FieldInfo>();

            var blockType = block.GetType();
            var type = typeof(Port);

            string log = $"Ports for type {blockType.Name}:\n";
            
            foreach (var property in blockType.GetFields())
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
        
        public static List<Port> GetAllPorts(IStructure structure)
        {
            List<Port> result = new List<Port>();
            foreach (var structureBlock in structure.Blocks)
            {
                GetAllPorts(structureBlock, ref result);
            }

            return result;
        }
        
        public static void GetAllPorts(IBlock block, ref List<Port> result)
        {
            GetPorts(block, ref result);
            GetSpecialPorts(block, ref result);
        }
        
        public static void GetPorts(IBlock block, ref List<Port> result)
        {
            var properties = GetPortsInfo(block);
            foreach (var property in properties)
            {
                result.Add(property.GetValue(block) as Port);
            }
        }
        
        public static void GetSpecialPorts(IBlock block, ref List<Port> result)
        {
            if(block is ISpecialPorts specialPortsBlock)
            {
                var specialPorts = specialPortsBlock.GetPorts();
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
    
    [AttributeUsage(AttributeTargets.Property)]
    public class PlayerPropertyAttribute : Attribute { }

    [Serializable]
    public class BlockConfiguration
    {
        public string path;
        public string current;
        public Dictionary<string, string> setup;
    }

    [Serializable]
    public class StructureConfiguration
    {
        public List<BlockConfiguration> blocks = new List<BlockConfiguration>();
        public List<string> wires = new List<string>();
        
        private Dictionary<string, BlockConfiguration> blocksCache;
        
        public BlockConfiguration GetBlock(string path)
        {
            blocksCache ??= blocks.ToDictionary(block => block.path);

            return blocksCache[path];
        }
    }
}
