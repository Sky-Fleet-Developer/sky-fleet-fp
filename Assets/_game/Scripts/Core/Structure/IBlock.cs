using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Cargo;
using Core.Configurations;
using Core.Items;
using Core.SessionManager.SaveService;
using Core.Structure.Rigging;
using Core.Utilities;
using UnityEngine;

namespace Core.Structure
{
    public interface IBlock : ITablePrefab, IMass
    {
        // ReSharper disable once InconsistentNaming
        Vector3 localPosition { get; }
        Parent Parent { get; }
        IStructure Structure { get; }
        string MountingType { get; }
        bool IsActive { get; }

        void InitBlock(IStructure structure, Parent parent);
        Bounds GetBounds();
    }

    public static class BlockExtension
    {
        public static string GetPath(this IBlock block)
        {
            return block.Parent.Transform.GetPath(block.Structure.transform);
        }

        private static Dictionary<Type, PropertyInfo[]> _propertiesCache;
        private static Dictionary<Type, FieldInfo[]> _fieldsCache;

        public static PropertyInfo[] GetProperties(this IBlock block)
        {
            Type blockType = block.GetType();
            if (_propertiesCache == null) _propertiesCache = new Dictionary<Type, PropertyInfo[]>();
            if (_propertiesCache.ContainsKey(blockType)) return _propertiesCache[blockType];

            PropertyInfo[] properties = GetBlockPlayerProperties(blockType);
            _propertiesCache.Add(blockType, properties);
            return properties;
        }
        
        public static FieldInfo[] GetFields(this IBlock block)
        {
            Type blockType = block.GetType();
            if (_fieldsCache == null) _fieldsCache = new Dictionary<Type, FieldInfo[]>();
            if (_fieldsCache.ContainsKey(blockType)) return _fieldsCache[blockType];

            FieldInfo[] fields = GetBlockConstantFields(blockType);
            _fieldsCache.Add(blockType, fields);
            return fields;
        }

        private static PropertyInfo[] GetBlockPlayerProperties(Type type)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();

            Type attribute = typeof(PlayerPropertyAttribute);

            //string log = $"Properties for type {type.Name}:\n";

            foreach (PropertyInfo property in type.GetProperties())
            {
                if (property.GetCustomAttributes().FirstOrDefault(x => x.GetType() == attribute) != null)
                {
                    properties.Add(property);
                    //log += $"{property.Name},";
                }
            }

            //Debug.Log(log);

            return properties.ToArray();
        }
        
        private static FieldInfo[] GetBlockConstantFields(Type type)
        {
            List<FieldInfo> fields = new List<FieldInfo>();

            Type attribute = typeof(ConstantFieldAttribute);

            string log = $"Fields for type {type.Name}:\n";

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.GetCustomAttributes().FirstOrDefault(x => x.GetType() == attribute) != null)
                {
                    fields.Add(field);
                    log += $"{field.Name},";
                }
            }

            Debug.Log(log);

            return fields.ToArray();
        }

        private static void ApplyMember(IBlock block, Type memberType, Action<object, object> setter, string value)
        {
            if (memberType == typeof(string))
            {
                setter(block, value);
            }
            else if (memberType == typeof(float))
            {
                if (float.TryParse(value, out float val))
                {
                    setter(block, val);
                }
                else
                {
                    Debug.LogError($"Cannot parse {value} into float!");
                }
            }
            else if (memberType == typeof(int))
            {
                if (int.TryParse(value, out int val))
                {
                    setter(block, val);
                }
                else
                {
                    Debug.LogError($"Cannot parse {value} into int!");
                }
            }
            else if (memberType == typeof(bool))
            {
                if (bool.TryParse(value, out bool val))
                {
                    setter(block, val);
                }
                else
                {
                    Debug.LogError($"Cannot parse {value} into bool!");
                }
            }
        }
        
        public static void ApplyField(this IBlock block, FieldInfo field, string value)
        {
            Type type = field.FieldType;
            ApplyMember(block, type, field.SetValue, value);
        }

        public static void ApplyProperty(this IBlock block, PropertyInfo property, string value)
        {
            Type type = property.PropertyType;
            ApplyMember(block, type, property.SetValue, value);
        }

        public static Parent GetParentByPath(this IBlock block, ref Parent cache, string path)
        {
            if (cache == null)
            {
                var structure = block.Structure ?? block.transform.GetComponentInParent<IStructure>();
                if (structure == null)
                {
                    return null;
                }
                var parent = structure.GetParentByPath(path);
                if (parent != null)
                {
                    cache = parent;
                }
            }
            return cache;
        }

        public static void SetParentByPath(this IBlock block, Transform value, ref string path)
        {
            var structure = block.Structure ?? block.transform.GetComponentInParent<IStructure>();
            if (structure == null)
            {
                return;
            }
            if (!value.IsChildOf(structure.transform))
            {
                return;
            }
            path = value.GetPath(structure.transform);
        }
    }
}