using System.Collections;
using System.Collections.Generic;
using Core.TerrainGenerator.Settings;
using UnityEngine;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// Provides data for all channels on current rectangle
    /// </summary>
    public interface IDeformer
    {
        Quaternion Rotation { get; }
        Vector3 Position { get; }
        Rect AxisAlignedRect { get; }
        Vector4 LocalRect { get; }
        float Fade { get; }
        int Layer { get; }
        T GetModules<T>() where T : class, IDeformerModule;
        IEnumerable<Vector2Int> GetAffectChunks(float chunkSize);
        Terrain[] GetTerrainsContacts();
        Vector2 GetLocalPointCoordinates(Vector3 worldPos);
        Vector3 InverseTransformPoint(Vector3 worldPos);
        Vector3 TransformPoint(Vector3 localPos);
    }
}