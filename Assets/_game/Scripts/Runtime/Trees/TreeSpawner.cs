using Core;
using Core.PlacedPrefabs;
using Core.Trees;
using Runtime.PlacedPrefabs;
using UnityEngine;
using Zenject;

namespace Runtime.Trees
{
    /// <summary>Trees with collision around the camera, placed and spawned by <see cref="PlacedPrefabSpawner"/>.</summary>
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
    }
}
