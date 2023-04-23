using System.Collections.Generic;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime
{
    [CreateAssetMenu(menuName = "Management/GameData")]
    public class GameData : SingletonAsset<GameData>
    {
        [InlineProperty(LabelWidth = 160), SerializeField] private SharedGameData serializedSharedData;
        [InlineProperty(LabelWidth = 160), SerializeField] private PrivateGameData serializedPrivateData;
        public static SharedGameData Data;
        internal static PrivateGameData PrivateData; 
        public void OnEnable()
        {
            Data = serializedSharedData;
            PrivateData = serializedPrivateData;
            SetSqrLodDistances();
        }

        private void SetSqrLodDistances()
        {
            Data.sqrLodDistances = new float[Data.lodDistances.Length];
            for(int i = 0; i < Data.sqrLodDistances.Length; i++)
            {
                Data.sqrLodDistances[i] = Data.lodDistances[i] * Data.lodDistances[i];
            }
        }
        private void OnValidate()
        {
            Data = serializedSharedData;
            SetSqrLodDistances();
        }
    }
    
    [System.Serializable]
    public class SharedGameData
    {
        [Header("Management")] 
        public string serializationVersion = "0.0.1";
        [Header("Character")]
        public LayerMask interactiveLayer;
        public float interactionDistance = 1f;
        public string controlFailText = "Control is alredy used";
        [Header("Physics")]
        public LayerMask walkableLayer;
        public LayerMask terrainLayer;
        [Header("Logistics")] 
        public float fuelTransitionAmount = 5;
        [Header("Lod")]
        public float[] lodDistances;
        [HideInInspector]
        public float[] sqrLodDistances;
    }

    [System.Serializable]
    public class PrivateGameData
    {
        [Header("Bundles")] public List<string> remotePrefabsTags;
    }


}
