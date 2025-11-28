using System.Collections.Generic;
using Core.Structure;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Core.Data
{
    [CreateAssetMenu(menuName = "SF/Data/GameData")]
    public class GameData : CompoundScriptableObject
    {
        [InlineProperty(LabelWidth = 160), SerializeField] private SharedGameData serializedSharedData;
        [InlineProperty(LabelWidth = 160), SerializeField] private PrivateGameData serializedPrivateData;
        public static SharedGameData Data;
        internal static PrivateGameData PrivateData;

        [Inject]
        private void InjectChildren(DiContainer diContainer)
        {
            foreach (var child in children)
            {
                diContainer.Inject(child);
            }
        }
        
        public void Initialize()
        {
            Data = serializedSharedData;
            PrivateData = serializedPrivateData;
            Data.lodDistances.Init();
        }

        public void InstallChildren(DiContainer container)
        {
            foreach (var child in children)
            {
                if(child is IMyInstaller installer)
                {
                    installer.InstallBindings(container);
                }
                else
                {
                    container.Bind(child.GetType()).FromInstance(child);
                }
            }
        }

        private void OnValidate()
        {
            Initialize();
        }
    }
    
    [System.Serializable]
    public class SharedGameData
    {
        [Header("Management")] 
        public string serializationVersion = "0.0.1";
        [Header("Character")]
        public LayerMask cargoCheckLayer;
        public LayerMask interactiveLayer;
        public LayerMask rayScanLayer;
        public int interactiveLayerIndex;
        public float interactionDistance = 1f;
        public int maxCollidersToScan = 20;
        public string controlFailText = "Control is already used";
        [Header("Physics")]
        public LayerMask walkableLayer;
        public LayerMask terrainLayer;
        [Header("Logistics")] 
        public float fuelTransitionAmount = 5;
        [Header("Lod")]
        public LodSettings lodDistances;
        [Header("Miscellaneous")]
        public int initialStructuresCacheCapacity;
    }

    [System.Serializable]
    public class PrivateGameData
    {
        [Header("Bundles")] public List<string> remotePrefabsTags;
    }


}
