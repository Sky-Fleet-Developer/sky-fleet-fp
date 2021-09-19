using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime
{
    [CreateAssetMenu(menuName = "Management/GameData")]
    public class GameData : SingletonAsset<GameData>
    {
        [InlineProperty(LabelWidth = 160), SerializeField] private StaticGameData serializedData;
        public static StaticGameData Data;
        public void OnEnable()
        {
            Data = serializedData;
        }
    }
    
    [System.Serializable]
    public class StaticGameData
    {
        [Header("Character")]
        public LayerMask interactiveLayer;
        public float interactionDistance = 1f;
        public string controlFailText = "Control is alredy used";
        [Header("Physics")]
        public LayerMask groundLayer;
    }
}
