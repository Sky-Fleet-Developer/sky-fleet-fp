using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    /// <summary>
    /// Saves settings about channel creating and setup
    /// </summary>
    [System.Serializable]
    public abstract class ChannelSettings : ScriptableObject
    {
        [SerializeField] private TerrainGenerationSettings container;
        public TerrainGenerationSettings Container => container;
        
#if UNITY_EDITOR
        public void Initialize(TerrainGenerationSettings container)
        {
            this.container = container;
        }
#endif

        public abstract DeformationChannel MakeDeformationChannel(Vector2Int position, string directory);
    }
}
