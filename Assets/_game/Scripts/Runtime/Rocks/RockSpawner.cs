using Core;
using Core.PlacedPrefabs;
using Core.Rocks;
using Runtime.PlacedPrefabs;
using UnityEngine;
using Zenject;

namespace Runtime.Rocks
{
    /// <summary>Large rocks with collision around the camera, placed and spawned by <see cref="PlacedPrefabSpawner"/>.</summary>
    public class RockSpawner : PlacedPrefabSpawner, IMyInstaller, IRocks
    {
        [SerializeField] private RockSettings settings;

        public RockSettings Settings => settings;

        protected override PlacedPrefabSettings PlacedSettings => settings;

        protected override string RootName => "Rocks";

        public void InstallBindings(DiContainer container)
        {
            container.Bind<IRocks>().FromInstance(this);
        }
    }
}
