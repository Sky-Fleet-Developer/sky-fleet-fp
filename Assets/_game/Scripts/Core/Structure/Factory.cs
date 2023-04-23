using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Core.SessionManager.SaveService;
using Core.Structure.Rigging;
using Core.Structure.Wires;
using Core.Utilities;
using Core.Utilities.AsyncAwaitUtil.Source;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

        public static void ApplySetup(IBlock block, BlockConfiguration setup)
        {
            PropertyInfo[] properties = GetBlockProperties(block);
            for (int i = 0; i < properties.Length; i++)
            {
                if (setup.TryGetSetup(properties[i].Name, out string value))
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


            if (structure.Blocks.Count > 0)
            {
                await ReplaceExistBlocks(structure, configuration);
            }
            else
            {
                await InstanceBlocks(structure, configuration);
            }

            await Task.Yield();
            structure.RefreshBlocksAndParents();
            structure.InitBlocks();

            foreach (IBlock block in structure.Blocks)
            {
                string path = GetPath(block);
                BlockConfiguration blockConfig = configuration.GetBlock(path, block.transform.name);

                if (blockConfig == null) continue;

                ApplySetup(block, blockConfig);
            }

            try
            {
                structure.InitBlocks();

                configuration.ApplyWires(structure);

                structure.OnInitComplete();
                Debug.Log($"{structure.transform.name} configuration success!");
            }
            catch (Exception e)
            {
                Debug.LogError("Error when init structure: " + e);
            }
        }

        private static Task InstanceBlocks(IStructure structure, StructureConfiguration configuration)
        {
            List<Task> waiting = new List<Task>();
            foreach (BlockConfiguration config in configuration.blocks)
            {
                waiting.Add(InstantiateBlock(config, structure));
            }
            return Task.WhenAll(waiting);
        }

        private static async Task InstantiateBlock(BlockConfiguration configuration, IStructure structure)
        {
            for (int i = 0; i < 10; i++)
            {
                Parent parent = structure.Parents.FirstOrDefault(x => x.Path == configuration.path);
                if (parent == null)
                {
                    await Task.Yield();
                    continue;
                }
                RemotePrefabItem wantedBlock = TablePrefabs.Instance.GetItem(configuration.currentGuid);
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

                configuration.ApplyPrimarySetup(instance.GetComponent<IBlock>());
                break;
            }
        }

        private static Task ReplaceExistBlocks(IStructure structure, StructureConfiguration configuration)
        {
            List<Task> waiting = new List<Task>();
            for (int i = 0; i < structure.Blocks.Count; i++)
            {
                IBlock block = structure.Blocks[i];
                string path = GetPath(block);
                BlockConfiguration blockConfig = configuration.GetBlock(path,block.transform.name);

                if (blockConfig == null) continue;

                if (block.Guid != blockConfig.currentGuid)
                {
                    Task task = ReplaceBlock(structure, i, blockConfig);
                    waiting.Add(task);
                }
                else
                {
                    blockConfig.ApplyPrimarySetup(block);
                }
            }

            return Task.WhenAll(waiting);
        }


        public static async Task ReplaceBlock(IStructure structure, int blockIdx, BlockConfiguration configuration)
        {
            IBlock block = structure.Blocks[blockIdx];

            await InstantiateBlock(configuration, structure);

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
            return new BlockConfiguration(block, GetPath(block));
        }

        /*public static string GetPath(IBlock block)
        {
            string result = block.transform.name;
            Transform tr = block.transform.parent;
            result = GetPath(tr) + "/" + result;

            return result;
        }*/

        public static string GetPath(IBlock block)
        {
            return GetPath(block.transform.parent);
        }

        public static string GetPath(Transform transform)
        {
            string result = string.Empty;
            Transform tr = transform;
            while (tr.GetComponent<IStructure>() == null)
            {
                tr = tr.parent;
                result = tr.name + "/" + result;
            }

            return result;
        }

        /*public static (Transform parent, string name) GetParent(IStructure structure, string path)
        {
            string[] pathStrings = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            Transform tr = structure.transform;
            for (int i = 0; i < pathStrings.Length - 1; i++)
            {
                tr = tr.Find(pathStrings[i]);
                if (tr == null) return (null, pathStrings[pathStrings.Length - 1]);
            }

            return (tr, pathStrings[pathStrings.Length - 1]);
        }*/

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
                wires = new List<WireConfiguration>(),
                bodyGuid = structure.Guid
            };

            for (int i = 0; i < structure.Blocks.Count; i++)
            {
                IBlock block = structure.Blocks[i];

                configuration.blocks.Add(GetConfiguration(block));
            }

            if (structure.Wires != null)
            {
                foreach (Wire structureWire in structure.Wires)
                {
                    configuration.wires.Add(new WireConfiguration(structureWire.ports.Select(x => x.Id).ToList()));
                }
            }

            return configuration;
        }


        public static Dictionary<Type, FieldInfo[]> BlocksPorts;

        public static FieldInfo[] GetPortsInfo(IBlock block)
        {
            Type blockType = block.GetType();

            if (BlocksPorts == null) BlocksPorts = new Dictionary<Type, FieldInfo[]>();
            if (BlocksPorts.TryGetValue(blockType, out FieldInfo[] infos)) return infos;

            List<FieldInfo> fields = new List<FieldInfo>();

            Type type = typeof(Port);

            //string log = $"Ports for type {blockType.Name}:\n";

            foreach (FieldInfo field in blockType.GetFields())
            {
                if (field.FieldType == type || field.FieldType.InheritsFrom(type))
                {
                    fields.Add(field);
                    //log += $"{field.Name},";
                }
            }

            //Debug.Log(log);

            infos = fields.ToArray();

            BlocksPorts.Add(blockType, infos);

            return infos;
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
            GetMultiplePorts(block, ref result);
        }

        public static void GetPorts(IBlock block, ref List<PortPointer> result)
        {
            FieldInfo[] properties = GetPortsInfo(block);
            foreach (FieldInfo property in properties)
            {
                result.Add(new PortPointer(block, property.GetValue(block) as Port));
            }
        }

        public static void GetMultiplePorts(IBlock block, ref List<PortPointer> result)
        {
            if (block is IMultiplePorts specialPortsBlock)
            {
                IEnumerable<PortPointer> specialPorts = specialPortsBlock.GetPorts();
                Debug.Log($"+ {specialPorts.Count()} special ports");
                result.AddRange(specialPorts);
            }
        }
    }
}