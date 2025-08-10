using System;
using UnityEngine;

namespace Core.Utilities
{
    public static class TransformExtension
    {
        public static Bounds GetBounds(this Transform root)
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.zero);
            }
            Bounds bound = renderers[0].bounds;
            foreach (MeshRenderer hit in renderers)
            {
                bound.Encapsulate(hit.bounds);
            }

            bound.center = root.InverseTransformPoint(bound.center);
            /*Debug.DrawLine(bound.min, bound.max, Color.red, 10);
            Debug.DrawLine(bound.min + Vector3.up * bound.size.y, bound.max - Vector3.up * bound.size.y, Color.red, 10);
            Debug.DrawLine(bound.min + Vector3.right * bound.size.x, bound.max - Vector3.right * bound.size.x, Color.red, 10);
            Debug.DrawLine(bound.min + Vector3.forward * bound.size.z, bound.max - Vector3.forward * bound.size.z, Color.red, 10);*/

            return bound;
        }

        public static void ApplyLayer(this Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (Transform hit in transform) hit.ApplyLayer(layer);
        }
        
        public static string GetPath(this Transform transform, Transform root = null)
        {
            string result = string.Empty;
            Transform tr = transform;
            for(int i = 0; i < 10; i++)
            {
                if(!tr || tr == root) break;
                result = tr.name + "/" + result;
                tr = tr.parent;
            }

            return result;
        }
    }
}
