using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtension
{
    public static Bounds GetBounds(this Transform root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        var bound = renderers[0].bounds;
        foreach (var hit in renderers)
        {
            bound.Encapsulate(hit.bounds);
        }
        Debug.DrawLine(bound.min, bound.max, Color.red, 10);
        Debug.DrawLine(bound.min + Vector3.up * bound.size.y, bound.max - Vector3.up * bound.size.y, Color.red, 10);
        Debug.DrawLine(bound.min + Vector3.right * bound.size.x, bound.max - Vector3.right * bound.size.x, Color.red, 10);
        Debug.DrawLine(bound.min + Vector3.forward * bound.size.z, bound.max - Vector3.forward * bound.size.z, Color.red, 10);

        return bound;
    }

    public static void ApplyLayer(this Transform transform, int layer)
    {
        transform.gameObject.layer = layer;
        foreach (Transform hit in transform) hit.ApplyLayer(layer);
    }
}
