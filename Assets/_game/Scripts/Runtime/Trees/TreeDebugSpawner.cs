using System.Collections.Generic;
using Core.PlacedPrefabs;
using Core.TerrainGenerator.Surfaces;
using Core.Trees;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Runtime.Trees
{
    /// <summary>
    /// Editor-only preview of <see cref="TreeSettings"/>: scatters the trees of the settings over a disc around this
    /// object on the CPU, without playing. The GPU placement of <see cref="TreeSpawner"/> can't run in edit mode and
    /// the CPU doesn't know the surface weights, so the surface only filters layers and the density noise is
    /// approximated. The age of the grove (the weight of the surface in the real placement) is made up: it rises along
    /// this object's forward direction across the disc, or is fixed. The trees are not saved with the scene.
    /// </summary>
    [AddComponentMenu("Terrain/Tree Debug Spawner")]
    public class TreeDebugSpawner : MonoBehaviour
    {
        private const string RootName = "TreeDebugInstances";

        [SerializeField] private TreeSettings settings;
        [PropertyTooltip("Only the layers of this surface type. Empty: every layer")]
        [SerializeField] private SurfaceType onlySurface;
        [PropertyTooltip("Radius of the disc around this object, meters")]
        [SerializeField, MinValue(1)] private float radius = 100;
        [SerializeField] private int seed = 1;
        [PropertyTooltip("The age of the grove (surface weight) rises from 0 at the back of the disc to 1 at the front, " +
                         "along the forward direction of this object")]
        [SerializeField] private bool ageAcrossDisc = true;
        [PropertyTooltip("The age of the grove (surface weight) everywhere")]
        [SerializeField, HideIf(nameof(ageAcrossDisc)), PropertyRange(0, 1)] private float fixedAge = 1;
        [PropertyTooltip("Most trees spawned at once, a guard against a huge radius with a high density")]
        [SerializeField, MinValue(1)] private int maxTrees = 3000;
        [PropertyTooltip("Layers the ground is looked for on: colliders of the terrain. Without a hit the tree stands " +
                         "on the plane of this object")]
        [SerializeField] private LayerMask groundMask = ~0;

        [Button(ButtonSizes.Large)]
        public void Spawn()
        {
            Clear();
            if (settings == null) return;

            var random = new System.Random(seed);
            var placements = new List<(GameObject prefab, Vector3 position, Quaternion rotation, float scale)>();
            var center = transform.position;
            var area = Mathf.PI * radius * radius;
            // The age of the grove (surface weight): along this object's forward it rises from 0 at the back of the disc to 1 at the front
            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            var axis = forward.sqrMagnitude > 1e-6f ? new Vector2(forward.x, forward.z).normalized : Vector2.up;
            int candidates = 0, droppedByAge = 0, droppedByNoise = 0, droppedByGround = 0, droppedBySlope = 0, emptySlots = 0, skippedLayers = 0;
            float steepest = 0;
            var skipReason = "";

            foreach (var layer in settings.Layers)
            {
                if (layer.Prefabs.Count == 0)
                {
                    skippedLayers++;
                    skipReason = "a layer has no prefabs";
                    continue;
                }

                if (onlySurface != null && layer.Surface != onlySurface)
                {
                    skippedLayers++;
                    skipReason = $"onlySurface is '{onlySurface.name}', the layer's surface is '{(layer.Surface != null ? layer.Surface.name : "none")}'";
                    continue;
                }

                var expected = layer.Density * area;
                var count = (int) expected + (random.NextDouble() < expected - (int) expected ? 1 : 0);
                for (var i = 0; i < count && placements.Count < maxTrees; i++)
                {
                    var angle = (float) random.NextDouble() * Mathf.PI * 2;
                    var distance = Mathf.Sqrt((float) random.NextDouble()) * radius;
                    var x = center.x + Mathf.Cos(angle) * distance;
                    var z = center.z + Mathf.Sin(angle) * distance;

                    candidates++;
                    var age = ageAcrossDisc ? Mathf.InverseLerp(-radius, radius, (x - center.x) * axis.x + (z - center.z) * axis.y) : fixedAge;
                    // The density follows the weight of the surface, and a layer lies only in its range of ages
                    if (random.NextDouble() >= age * PlacedAge.Weight(layer, age))
                    {
                        droppedByAge++;
                        continue;
                    }

                    if (!PassesNoise(layer, x, z, random))
                    {
                        droppedByNoise++;
                        continue;
                    }

                    if (!FindGround(x, z, center.y, out var point, out var normal))
                    {
                        droppedByGround++;
                        continue;
                    }

                    var tilt = Vector3.Angle(normal, Vector3.up);
                    steepest = Mathf.Max(steepest, tilt);
                    if (tilt < layer.Slope.x || tilt > layer.Slope.y)
                    {
                        droppedBySlope++;
                        continue;
                    }

                    var scale = Mathf.Lerp(layer.Scale.x, layer.Scale.y, (float) random.NextDouble());
                    var alignment = Mathf.Lerp(layer.SlopeAlignment.x, layer.SlopeAlignment.y, (float) random.NextDouble());
                    var up = Vector3.Slerp(Vector3.up, normal, alignment).normalized;
                    var max = layer.MaxRotation;
                    var rotation = Quaternion.FromToRotation(Vector3.up, up)
                                   * Quaternion.Euler(Signed(random) * max.x, Signed(random) * max.y, Signed(random) * max.z);
                    var sink = Mathf.Lerp(layer.Sink.x, layer.Sink.y, (float) random.NextDouble()) * scale;
                    var prefab = layer.Prefabs[random.Next(layer.Prefabs.Count)];
                    if (prefab == null)
                    {
                        emptySlots++;
                        continue;
                    }

                    placements.Add((prefab, point - up * sink, rotation, scale));
                }
            }

            Debug.Log($"Tree debug spawn: {placements.Count} trees of {candidates} candidates. Dropped: {droppedByAge} by age, {droppedByNoise} by noise, " +
                      $"{droppedByGround} without ground, {droppedBySlope} by slope (steepest ground hit {steepest:0} deg), " +
                      $"{emptySlots} empty prefab slots. Layers skipped: {skippedLayers} of {settings.Layers.Count} {skipReason}. Settings: '{settings.name}'", this);

            // Instantiated after the search so that the trees placed so far don't catch the rays
            var root = new GameObject(RootName) { hideFlags = HideFlags.DontSaveInEditor };
            root.transform.SetParent(transform, false);
            foreach (var placement in placements)
            {
                var instance = Create(placement.prefab, root.transform);
                instance.transform.SetPositionAndRotation(placement.position, placement.rotation);
                instance.transform.localScale = Vector3.one * placement.scale;
            }
        }

        [Button(ButtonSizes.Large)]
        public void Clear()
        {
            var root = transform.Find(RootName);
            if (root == null) return;
            if (Application.isPlaying) Destroy(root.gameObject);
            else DestroyImmediate(root.gameObject);
        }

        private void OnDisable() => Clear();

        private static float Signed(System.Random random) => (float) random.NextDouble() * 2 - 1;

        private static GameObject Create(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return (GameObject) PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
            return Instantiate(prefab, parent);
        }

        private bool FindGround(float x, float z, float planeY, out Vector3 point, out Vector3 normal)
        {
            const float rayHeight = 1000;
            var ray = new Ray(new Vector3(x, planeY + rayHeight, z), Vector3.down);
            if (Physics.Raycast(ray, out var hit, rayHeight * 2, groundMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                normal = hit.normal;
                return true;
            }

            point = new Vector3(x, planeY, z);
            normal = Vector3.up;
            return true;
        }

        /// <summary>Patches by Perlin noise, like the density noise of the layer but not equal to it.</summary>
        private static bool PassesNoise(IPlacedPrefabLayer layer, float x, float z, System.Random random)
        {
            if (layer.NoiseScale <= 0) return true;

            var offset = layer.NoiseSeed * 17.31f;
            var value = Mathf.PerlinNoise(x / layer.NoiseScale + offset, z / layer.NoiseScale + offset);
            var edge = 1 - layer.NoiseCoverage;
            var width = Mathf.Max(layer.NoiseSoftness * 0.5f, 0.0001f);
            var weight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(edge - width, edge + width, value));
            if (layer.NoiseInvert) weight = 1 - weight;
            return random.NextDouble() < weight;
        }
    }
}
