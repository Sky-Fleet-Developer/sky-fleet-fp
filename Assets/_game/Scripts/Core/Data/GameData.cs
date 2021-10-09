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
        }
    }
    
    [System.Serializable]
    public class SharedGameData
    {
        [Header("Character")]
        public LayerMask interactiveLayer;
        public float interactionDistance = 1f;
        public string controlFailText = "Control is alredy used";
        [Header("Physics")]
        public LayerMask groundLayer;
    }

    [System.Serializable]
    public class PrivateGameData
    {
        [Header("Bundles")] public List<string> remotePrefabsTags;
    }
}
