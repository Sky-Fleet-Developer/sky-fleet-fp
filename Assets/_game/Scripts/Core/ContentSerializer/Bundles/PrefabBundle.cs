using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Structure.Rigging;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.ContentSerializer.Bundles
{
    [Serializable]
    public class PrefabBundle : Bundle
    {
        [ShowInInspector] public Dictionary<int, PrefabBundleObject> Tree = new Dictionary<int, PrefabBundleObject>();

        public PrefabBundle()
        {
            
        }
        
        public PrefabBundle(GameObject root, ISerializationContext context)
        {
            name = root.name;
            id = root.GetInstanceID();
            context.AddTag = v =>
            {
                if (!tags.Contains(v)) tags.Add(v);
            };
            GetTree(root.transform, context);
        }

        public void GetTree(Transform root, ISerializationContext context)
        {
            Tree.Add(root.GetInstanceID(), new PrefabBundleObject(root.gameObject, context));
            foreach (Transform tr in root.transform)
            {
                GetTree(tr, context);
            }
        }

        public async Task<Transform> ConstructTree(Transform parent, ISerializationContext context)
        {
            Transform root = null;
            Dictionary<int, Transform> transforms = new Dictionary<int, Transform>(Tree.Count);
            foreach (KeyValuePair<int, PrefabBundleObject> obj in Tree)
            {
                GameObject go = new GameObject(obj.Value.name);
                if (obj.Value.parent == -1)
                {
                    go.transform.SetParent(parent);
                    root = go.transform;
                }

                transforms.Add(obj.Key, go.transform);
            }

            foreach (KeyValuePair<int, Transform> obj in transforms)
            {
                if (Tree[obj.Key].parent == -1) continue;
                obj.Value.parent = transforms[Tree[obj.Key].parent];
            }

            if (!context.IsCurrentlyBuilded)
            {
                root.gameObject.SetActive(false);
                Object.DontDestroyOnLoad(root.gameObject);
            }

            Dictionary<int, Component> reconstruction = new Dictionary<int, Component>();

            foreach (KeyValuePair<int, PrefabBundleObject> obj in Tree)
            {
                obj.Value.ReconstructTypes(transforms[obj.Key], ref reconstruction, context);
            }

            foreach (KeyValuePair<int, PrefabBundleObject> obj in Tree)
            {
                await obj.Value.ReconstructCache(reconstruction, context);
            }

            return root;
        }
    }

    [Serializable]
    public class PrefabBundleObject
    {
        public string name;
        public int layer;
        public int parent = -1;
        public List<string> components;
        [ShowInInspector] public Dictionary<string, string> Cache;

        public PrefabBundleObject()
        {
        }

        public PrefabBundleObject(GameObject source, ISerializationContext context)
        {
            name = source.name;
            layer = source.layer;
            Transform p = source.transform.parent;
            if (p)
            {
                parent = p.GetInstanceID();
            }

            Cache = new Dictionary<string, string>();
            components = new List<string>();
            foreach (Component component in source.GetComponents<Component>())
            {
                Type type = component.GetType();
                if (type.InheritsFrom(typeof(IBlock)))
                {
                    context.AddTag("Block");
                    IBlock block = component as IBlock;
                    context.AddTag(block.Guid);
                    context.AddTag(block.MountingType);
                }
                components.Add(type.FullName + "|" + component.GetInstanceID());
                context.Behaviour.GetNestedCache(type.FullName, component, Cache);
            }
        }

        private List<int> reconstructedTypes;

        public void ReconstructTypes(Transform transform, ref Dictionary<int, Component> reconstruction, ISerializationContext context)
        {
            reconstructedTypes = new List<int>();
            foreach (string componentName in components)
            {
                string[] split = componentName.Split(new[] {'|'});
                Type type = context.GetTypeByName(split[0]);
                if (!transform.gameObject.TryGetComponent(type, out Component component))
                    component = transform.gameObject.AddComponent(type);

                int id = int.Parse(split[1]);
                reconstruction.Add(id, component);
                reconstructedTypes.Add(id);
            }
        }

        public async Task ReconstructCache(Dictionary<int, Component> reconstruction, ISerializationContext context)
        {
            foreach (KeyValuePair<int, Component> component in reconstruction)
            {
                if (reconstructedTypes.Contains(component.Key))
                {
                    object val = component.Value;
                    await context.Behaviour.SetNestedCache(component.Value.GetType().FullName, val, Cache, reconstruction);
                }
            }
        }
    }
}