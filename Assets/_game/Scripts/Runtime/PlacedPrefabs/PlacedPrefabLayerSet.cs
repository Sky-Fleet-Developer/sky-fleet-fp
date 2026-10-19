using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.PlacedPrefabs;
using Core.TerrainGenerator;
using Runtime.Scatter;
using UnityEngine;

namespace Runtime.PlacedPrefabs
{
    /// <summary>
    /// The layers of a placed prefab kind as PlacedPrefabPlacement.compute reads them, and the prefabs they use (the
    /// index of a prefab in <see cref="Prefabs"/> is what an instance carries). Everything that places instances of
    /// the kind (the spawner of the near ring, the far field) builds its own set from the same settings, and a set
    /// built from the same settings and terrain always has the same order: the same tile is placed the same way.
    /// </summary>
    public class PlacedPrefabLayerSet : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LayerData
        {
            public const int Stride = sizeof(float) * 17;

            public Vector2 Scale;
            public Vector2 SlopeCos;
            public Vector2 SlopeAlignment;
            public Vector3 MaxRotation;
            public Vector2 Sink;
            public uint PrefabStart;
            public uint PrefabCount;
            public float Cover;
            public Vector2 AgeRange;
            public float AgeFade;
        }

        private static readonly int LayersProperty = Shader.PropertyToID("placed_layers");
        private static readonly int LayerPrefabsProperty = Shader.PropertyToID("placed_layer_prefabs");
        private static readonly int PrefabHalfWidthProperty = Shader.PropertyToID("placed_prefab_half_width");

        private readonly List<GameObject> _prefabs = new ();
        private readonly List<float> _prefabHalfWidths = new ();
        private readonly List<LayerData> _layerData = new ();
        private readonly List<uint> _layerPrefabData = new ();
        private readonly ScatterLayerTable _layerTable = new ();
        private GraphicsBuffer _layers;
        private GraphicsBuffer _layerPrefabs;
        private GraphicsBuffer _prefabHalfWidthBuffer;

        public IReadOnlyList<GameObject> Prefabs => _prefabs;

        /// <summary>Instances per square meter of the densest surface. 0: nothing is placed.</summary>
        public float MaxDensity => _layerTable.MaxDensity;

        /// <summary>Lists the layers and prefabs of the settings over the surfaces of the terrain. Every instance placed before is stale.</summary>
        public void Build(ITerrainGpuData terrain, PlacedPrefabSettings settings)
        {
            _prefabs.Clear();
            _prefabHalfWidths.Clear();
            _layerData.Clear();
            _layerPrefabData.Clear();
            _layerTable.Build(terrain.Surfaces, settings.Layers, (_, layer) =>
            {
                int prefabStart = _layerPrefabData.Count;
                foreach (GameObject prefab in layer.Prefabs)
                {
                    if (!prefab)
                    {
                        continue;
                    }
                    int index = _prefabs.IndexOf(prefab);
                    if (index < 0)
                    {
                        index = _prefabs.Count;
                        _prefabs.Add(prefab);
                        _prefabHalfWidths.Add(HalfWidth(prefab));
                    }
                    _layerPrefabData.Add((uint)index);
                }

                // Normal y is the cosine of the slope: the steepest slope gives the smallest
                _layerData.Add(new LayerData
                {
                    Scale = layer.Scale,
                    SlopeCos = new Vector2(Mathf.Cos(layer.Slope.y * Mathf.Deg2Rad) - 1e-4f,
                        Mathf.Cos(layer.Slope.x * Mathf.Deg2Rad) + 1e-4f),
                    SlopeAlignment = layer.SlopeAlignment,
                    MaxRotation = layer.MaxRotation * Mathf.Deg2Rad,
                    Sink = layer.Sink,
                    PrefabStart = (uint)prefabStart,
                    PrefabCount = (uint)(_layerPrefabData.Count - prefabStart),
                    Cover = layer.GroundCover,
                    // Without an age range the layer has its full density at every age
                    AgeRange = layer.UseAge ? layer.AgeRange : new Vector2(-1, 2),
                    AgeFade = layer.UseAge ? layer.AgeFade : 1,
                });
            }, HasPrefab);

            // At least one element: every buffer a kernel reads must be bound
            if (_layerData.Count == 0)
            {
                _layerData.Add(default);
            }
            if (_layerPrefabData.Count == 0)
            {
                _layerPrefabData.Add(0);
            }
            if (_prefabHalfWidths.Count == 0)
            {
                _prefabHalfWidths.Add(0);
            }
            ScatterUtility.EnsureBuffer(ref _layers, _layerData.Count, LayerData.Stride);
            ScatterUtility.EnsureBuffer(ref _layerPrefabs, _layerPrefabData.Count, sizeof(uint));
            ScatterUtility.EnsureBuffer(ref _prefabHalfWidthBuffer, _prefabHalfWidths.Count, sizeof(float));
            _layers.SetData(_layerData);
            _layerPrefabs.SetData(_layerPrefabData);
            _prefabHalfWidthBuffer.SetData(_prefabHalfWidths);
        }

        /// <summary>Binds the layers of the table and of the kind to a kernel of PlacedPrefabPlacement.compute.</summary>
        public void Bind(ComputeShader shader, int kernel)
        {
            _layerTable.Bind(shader, kernel);
            shader.SetBuffer(kernel, LayersProperty, _layers);
            shader.SetBuffer(kernel, LayerPrefabsProperty, _layerPrefabs);
            shader.SetBuffer(kernel, PrefabHalfWidthProperty, _prefabHalfWidthBuffer);
        }

        public void Dispose()
        {
            _layerTable.Dispose();
            _layers?.Release();
            _layerPrefabs?.Release();
            _prefabHalfWidthBuffer?.Release();
            _layers = null;
            _layerPrefabs = null;
            _prefabHalfWidthBuffer = null;
        }

        // Half the larger of the width and depth of the meshes of a prefab, as its root is spawned at scale 1
        private static float HalfWidth(GameObject prefab)
        {
            Transform root = prefab.transform;
            Matrix4x4 toRoot = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, root.localScale) * root.worldToLocalMatrix;
            var bounds = new Bounds();
            bool found = false;
            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.sharedMesh)
                {
                    continue;
                }
                Bounds mesh = filter.sharedMesh.bounds;
                Matrix4x4 toScaledRoot = toRoot * filter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                    Vector3 point = toScaledRoot.MultiplyPoint3x4(mesh.center + Vector3.Scale(mesh.extents, sign));
                    if (found)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                }
            }
            return found ? 0.5f * Mathf.Max(bounds.size.x, bounds.size.z) : 0.5f;
        }

        private static bool HasPrefab(IPlacedPrefabLayer layer)
        {
            foreach (GameObject prefab in layer.Prefabs)
            {
                if (prefab)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
