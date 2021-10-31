using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable]
    public abstract class LayerSettings : ScriptableObject
    {
        [SerializeField] private TerrainGenerationSettings container;
        public TerrainGenerationSettings Container => container;
        
#if UNITY_EDITOR
        public void Initialize(TerrainGenerationSettings container)
        {
            this.container = container;
        }
#endif

        public abstract TerrainLayer MakeTerrainLayer(Vector2Int position, string directory);
    }
}
