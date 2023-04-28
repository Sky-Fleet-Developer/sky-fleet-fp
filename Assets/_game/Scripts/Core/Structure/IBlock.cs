using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.SessionManager.SaveService;
using Core.Structure.Rigging;
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

        void InitBlock(IStructure structure, Parent parent);
        Bounds GetBounds();
    }

    public static class BlockExtension
    {
        public static string GetPath(this IBlock block)
        {
            string result = string.Empty;
            Transform tr = block.transform;
            while (tr.GetComponent<IStructure>() == null)
            {
                tr = tr.parent;
                result = tr.name + "/" + result;
            }

            return result;
        }

        private static Dictionary<Type, PropertyInfo[]> _propertiesCache;

        public static PropertyInfo[] GetProperties(this IBlock block)
        {
            Type blockType = block.GetType();
            if (_propertiesCache == null) _propertiesCache = new Dictionary<Type, PropertyInfo[]>();
            if (_propertiesCache.ContainsKey(blockType)) return _propertiesCache[blockType];

            PropertyInfo[] properties = GetBlockProperties(blockType);
            _propertiesCache.Add(blockType, properties);
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

        public static void ApplyProperty(this IBlock block, PropertyInfo property, string value)
        {
            Type type = property.PropertyType;
            if (type == typeof(string))
            {
                property.SetValue(block, value);
            }
            else if (type == typeof(float))
            {
                if (float.TryParse(value, out float val))
                {
                    property.SetValue(block, val);
                }
                else
                {
                    Debug.LogError($"Cannot parse {value} into float!");
                }
            }
            else if (type == typeof(int))
            {
                if (int.TryParse(value, out int val))
                {
                    property.SetValue(block, val);
                }
                else
                {
                    Debug.LogError($"Cannot parse {value} into int!");
                }
            }
            else if (type == typeof(bool))
            {
                if (bool.TryParse(value, out bool val))
                {
                    property.SetValue(block, val);
                }
                else
                {
                    Debug.LogError($"Cannot parse {value} into bool!");
                }
            }
        }
    }
}