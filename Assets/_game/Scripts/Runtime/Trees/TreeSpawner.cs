using Core;
using Core.PlacedPrefabs;
using Core.Trees;
using Runtime.PlacedPrefabs;
using UnityEngine;
using Zenject;

namespace Runtime.Trees
{
    /// <summary>
    /// Trees with collision around the camera, placed and spawned by <see cref="PlacedPrefabSpawner"/>. The levels of
    /// detail of a tree prefab (its LODGroup) switch at the distances of the <see cref="TreeSettings"/>.
    /// </summary>
    public class TreeSpawner : PlacedPrefabSpawner, IMyInstaller, ITrees
    {
        [SerializeField] private TreeSettings settings;

        public TreeSettings Settings => settings;

        protected override PlacedPrefabSettings PlacedSettings => settings;

        protected override string RootName => "Trees";

        public void InstallBindings(DiContainer container)
        {
            container.Bind<ITrees>().FromInstance(this);
        }

        protected override void OnInstantiated(GameObject instance)
        {
            var group = instance.GetComponent<LODGroup>();
            Camera camera = Camera.main;
            if (!group || !camera || !settings)
            {
                return;
            }

            LOD[] lods = group.GetLODs();
            float[] distances = { settings.Lod1Distance, settings.Lod2Distance, settings.CullDistance };
            for (int i = 0; i < lods.Length && i < distances.Length; i++)
            {
                lods[i].screenRelativeTransitionHeight = HeightAt(group, camera, distances[i]);
            }
            group.SetLODs(lods);
        }

        // The relative screen height of the object at a distance. The LOD bias multiplies the height before it is
        // compared, so dividing by it keeps the distance the one in the settings
        private static float HeightAt(LODGroup group, Camera camera, float distance)
        {
            float size = group.size * MaxScale(group.transform);
            float height = size / (2 * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
            return Mathf.Clamp(height / Mathf.Max(QualitySettings.lodBias, 1e-3f), 0.0001f, 1f);
        }

        private static float MaxScale(Transform transform)
        {
            Vector3 scale = transform.lossyScale;
            return Mathf.Max(scale.x, scale.y, scale.z);
        }
    }
}
