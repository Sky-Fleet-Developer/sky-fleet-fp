using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core;
using Core.TerrainGenerator.Biomes;
using Core.TerrainGenerator.Biomes.Sources;
using Core.World;
using UnityEngine;
using Zenject;

namespace Runtime.Environment.Terrain.Biomes
{
    /// <summary>
    /// Collects the biomes among its descendants and provides them as <see cref="IBiomeSet"/>.
    /// Blending order is the hierarchy order: siblings top to bottom, a parent before its children.
    /// Place it at the scene root (or under a [Translator] object), so the Bootstrapper installs it.
    /// </summary>
    public class BiomeSet : MonoBehaviour, IBiomeSet, IMyInstaller
    {
        [SerializeField] private bool drawGizmos = true;

        private List<IBiome> _biomes = new ();
        private List<IBiome> _collected = new ();
        private readonly List<IBiome> _componentsCache = new ();
        private readonly List<BiomeSitePose> _sitesCache = new ();
        private List<Vector4> _snapshot = new ();
        private List<Vector4> _collectedSnapshot = new ();
        private bool _initialized;

        public IReadOnlyList<IBiome> Biomes
        {
            get
            {
                if (!_initialized)
                {
                    Refresh();
                }
                return _biomes;
            }
        }

        public event Action Changed;

        public void InstallBindings(DiContainer container)
        {
            container.Bind<IBiomeSet>().FromInstance(this);
        }

        private void Awake()
        {
            // Biomes must move together with the terrain when the world origin shifts
            this.AddWorldOffsetAnchor();
        }

        private void LateUpdate()
        {
            if (Refresh())
            {
                Changed?.Invoke();
            }
        }

        /// <returns>True when the biomes differ from the previous refresh.</returns>
        private bool Refresh()
        {
            _collected.Clear();
            Collect(transform, _collected);
            TakeSnapshot(_collected, _collectedSnapshot);

            bool changed = !_initialized || !SameBiomes(_biomes, _collected) || !SameSnapshot(_snapshot, _collectedSnapshot);
            _initialized = true;
            if (changed)
            {
                (_biomes, _collected) = (_collected, _biomes);
                (_snapshot, _collectedSnapshot) = (_collectedSnapshot, _snapshot);
            }
            return changed;
        }

        private void Collect(Transform node, List<IBiome> result)
        {
            if (!node.gameObject.activeInHierarchy)
            {
                return;
            }

            node.GetComponents(_componentsCache);
            foreach (IBiome biome in _componentsCache)
            {
                if (biome is not Behaviour { enabled: false })
                {
                    result.Add(biome);
                }
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Collect(node.GetChild(i), result);
            }
        }

        // Everything the terrain reads from the biomes, flattened
        private void TakeSnapshot(List<IBiome> biomes, List<Vector4> snapshot)
        {
            snapshot.Clear();
            foreach (IBiome biome in biomes)
            {
                snapshot.Add(biome.DebugColor);
                _sitesCache.Clear();
                biome.GetSites(_sitesCache);
                snapshot.Add(new Vector4(biome.Radius, biome.BlendDistance, _sitesCache.Count, 0));
                foreach (BiomeSitePose site in _sitesCache)
                {
                    snapshot.Add(new Vector4(site.Position.x, site.Position.y, site.Position.z, site.Yaw));
                }

                IBiomeSource source = biome.Source;
                if (source != null)
                {
                    snapshot.Add(new Vector4(source.TileSizeMeters, source.Origin.x, source.Origin.y, source.SharedEdgePixels ? 1 : 0));
                    int contentHash = source.ContentKey?.GetHashCode() ?? 0;
                    snapshot.Add(new Vector4(Low(contentHash), High(contentHash),
                        Low(RuntimeHelpers.GetHashCode(source)), High(RuntimeHelpers.GetHashCode(source))));
                }
                else
                {
                    snapshot.Add(Vector4.zero);
                }
            }
        }

        // Hash halves are stored exactly in floats; a whole int bit pattern could be NaN, which never compares equal
        private static float Low(int hash) => hash & 0xFFFF;
        private static float High(int hash) => (hash >> 16) & 0xFFFF;

        private static bool SameBiomes(List<IBiome> a, List<IBiome> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (!ReferenceEquals(a[i], b[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool SameSnapshot(List<Vector4> a, List<Vector4> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private readonly List<BiomeSite> _gizmoSites = new ();
        private readonly List<Vector2> _gizmoPolygon = new ();

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            _collected.Clear();
            Collect(transform, _collected);
            BiomeDiagram.CollectSites(_collected, Matrix4x4.identity, _gizmoSites);
            if (_gizmoSites.Count == 0)
            {
                return;
            }

            Rect bounds = GetGizmoBounds();
            float y = transform.position.y;
            for (int i = 0; i < _gizmoSites.Count; i++)
            {
                BiomeSite site = _gizmoSites[i];
                Color color = _collected[site.Biome].DebugColor;
                Gizmos.color = color;
                Vector3 center = new Vector3(site.Position.x, y, site.Position.y);
                float markerSize = Mathf.Max(10f, bounds.width * 0.002f);
                Gizmos.DrawSphere(center, markerSize);
                // Rotation of the source data: the local +z axis of the site
                Gizmos.DrawLine(center, center + new Vector3(site.Rotation.y, 0, site.Rotation.x) * markerSize * 20f);

                // Cell border
                BiomeDiagram.GetCellPolygon(_gizmoSites, i, 0, bounds, _gizmoPolygon);
                DrawPolygon(y);

                // Where the biome reaches 100%
                Gizmos.color = new Color(color.r, color.g, color.b, color.a * 0.4f);
                BiomeDiagram.GetCellPolygon(_gizmoSites, i, site.BlendDistance, bounds, _gizmoPolygon);
                DrawPolygon(y);
            }
        }

        private Rect GetGizmoBounds()
        {
            Vector2 min = _gizmoSites[0].Position;
            Vector2 max = min;
            foreach (BiomeSite site in _gizmoSites)
            {
                min = Vector2.Min(min, site.Position);
                max = Vector2.Max(max, site.Position);
            }

            float margin = Mathf.Max(5000f, Mathf.Max(max.x - min.x, max.y - min.y) * 0.5f);
            return Rect.MinMaxRect(min.x - margin, min.y - margin, max.x + margin, max.y + margin);
        }

        private void DrawPolygon(float y)
        {
            for (int i = 0; i < _gizmoPolygon.Count; i++)
            {
                Vector2 a = _gizmoPolygon[i];
                Vector2 b = _gizmoPolygon[(i + 1) % _gizmoPolygon.Count];
                Gizmos.DrawLine(new Vector3(a.x, y, a.y), new Vector3(b.x, y, b.y));
            }
        }
    }
}
